using System.IO;

namespace SmartTaskbar.Win11
{
    public static class Program
    {
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

            // TaskbarSense single-instance mutex (legacy GUID kept for upgrade continuity)
            using (new Mutex(true, "{a1b2c3d4-e5f6-7890-abcd-ef1234567890}", out var createNew))
            {
                if (!createNew) return;

                ApplicationConfiguration.Initialize();
                Application.Run(new SystemTray());
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
            }
            catch
            {
                // never throw from crash logger
            }
        }
    }
}