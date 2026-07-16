using Microsoft.Win32;
using SmartTaskbar.Win11.Abstractions;

namespace SmartTaskbar.Win11.Worker.Services
{
    public sealed class RegistryStartupRegistration : IStartupRegistration
    {
        public const string CurrentValueName = "TaskbarSense";

        public static readonly string[] LegacyValueNames =
        {
            "SmartTaskbar.Win11",
            "SmartTaskbar",
            "TaskbarSense.Win11"
        };

        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public bool IsCurrentEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
                return key?.GetValue(CurrentValueName) is string;
            }
            catch
            {
                return false;
            }
        }

        public bool IsAnyLegacyEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
                if (key is null)
                    return false;

                foreach (var name in LegacyValueNames)
                {
                    if (key.GetValue(name) is string)
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public void PurgeLegacyEntries()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                if (key is null)
                    return;

                foreach (var name in LegacyValueNames)
                {
                    try { key.DeleteValue(name, false); }
                    catch { /* ignore */ }
                }
            }
            catch
            {
                // ignore
            }
        }

        public void SetEnabled(string executablePath)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                key?.SetValue(CurrentValueName, $"\"{executablePath}\"");
            }
            catch
            {
                // ignore
            }
        }

        public void RemoveCurrent()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                key?.DeleteValue(CurrentValueName, false);
            }
            catch
            {
                // ignore
            }
        }

        public string? GetExecutablePath()
        {
            try
            {
                return Environment.ProcessPath
                       ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            }
            catch
            {
                return null;
            }
        }
    }
}