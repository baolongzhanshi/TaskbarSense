using System.Diagnostics;
using System.IO;

namespace SmartTaskbar.Win11
{
    public static class Program
    {
        private const string DotNetDesktopDownload =
            "https://dotnet.microsoft.com/download/dotnet/thank-you/runtime-desktop-8.0-windows-x64-installer";

        [STAThread]
        private static void Main()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) => WriteCrashLog("ThreadException", e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    WriteCrashLog("UnhandledException", ex);
                else
                    WriteCrashLog("UnhandledException", e.ExceptionObject?.ToString() ?? "Unknown");
            };

            // Framework-dependent builds need Desktop Runtime; self-contained always has it.
            if (!IsRunningSelfContained() && !IsDotNet8DesktopRuntimePresent())
            {
                ShowMissingRuntimeDialog();
                return;
            }

            // TaskbarSense single-instance mutex (legacy GUID kept for upgrade continuity)
            using (new Mutex(true, "{a1b2c3d4-e5f6-7890-abcd-ef1234567890}", out var createNew))
            {
                if (!createNew)
                {
                    // Already running — quiet exit (tray icon exists).
                    return;
                }

                ApplicationConfiguration.Initialize();
                Application.Run(new SystemTray());
            }
        }

        private static bool IsRunningSelfContained()
        {
            // Self-contained publish includes hostfxr / coreclr next to the exe.
            try
            {
                var baseDir = AppContext.BaseDirectory;
                return File.Exists(Path.Combine(baseDir, "coreclr.dll"))
                       || File.Exists(Path.Combine(baseDir, "hostfxr.dll"));
            }
            catch
            {
                return false;
            }
        }

        private static bool IsDotNet8DesktopRuntimePresent()
        {
            // Shared framework folder is the most reliable offline check.
            try
            {
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var desktopRoot = Path.Combine(programFiles, "dotnet", "shared", "Microsoft.WindowsDesktop.App");
                if (!Directory.Exists(desktopRoot))
                    return false;

                foreach (var dir in Directory.EnumerateDirectories(desktopRoot))
                {
                    var name = Path.GetFileName(dir);
                    if (name.StartsWith("8.", StringComparison.Ordinal))
                        return true;
                }
            }
            catch
            {
                // fall through
            }

            return false;
        }

        private static void ShowMissingRuntimeDialog()
        {
            var result = MessageBox.Show(
                "TaskbarSense 需要 .NET 8 桌面运行时（Desktop Runtime x64）才能运行。\n\n" +
                "是 = 打开官方下载页面\n" +
                "否 = 退出\n\n" +
                "也可以改下「SelfContained / 自包含」安装包，无需单独安装 .NET。",
                "TaskbarSense — 缺少运行时",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = DotNetDesktopDownload,
                    UseShellExecute = true
                });
            }
            catch
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://dotnet.microsoft.com/download/dotnet/8.0",
                        UseShellExecute = true
                    });
                }
                catch { /* ignore */ }
            }
        }

        private static void WriteCrashLog(string source, Exception ex)
            => WriteCrashLog(source, $"{ex.GetType().FullName}: {ex.Message}{Environment.NewLine}{ex.StackTrace}");

        private static void WriteCrashLog(string source, string message)
        {
            try
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "TaskbarSense",
                    "logs");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, "crash.log");
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {source}{Environment.NewLine}{message}{Environment.NewLine}{Environment.NewLine}";
                File.AppendAllText(path, line);

                // Simple rotate: keep last ~512 KB
                var info = new FileInfo(path);
                if (info.Length > 512 * 1024)
                {
                    var text = File.ReadAllText(path);
                    if (text.Length > 256 * 1024)
                        File.WriteAllText(path, text[^ (256 * 1024)..]);
                }
            }
            catch
            {
                // never throw from crash logger
            }
        }
    }
}