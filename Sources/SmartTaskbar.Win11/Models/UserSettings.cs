using SmartTaskbar.Win11.Abstractions;
using Microsoft.Win32;

namespace SmartTaskbar.Win11.Models
{
    public class UserSettings
    {
        private const string StartupValueName = "TaskbarSense";
        private readonly ISettingsStore _store;
        private UserConfiguration _configuration;

        public static UserSettings Instance { get; set; } = null!;

        public UserSettings(ISettingsStore store)
        {
            _store = store;

            var autoModeString = _store.GetValue<string>(nameof(UserConfiguration.AutoModeType));

            _configuration = new UserConfiguration
            {
                AutoModeType = autoModeString switch
                {
                    nameof(AutoModeType.Auto) => AutoModeType.Auto,
                    nameof(AutoModeType.MaximizeHide) => AutoModeType.MaximizeHide,
                    _ => AutoModeType.None
                },
                ShowTaskbarWhenExit =
                    _store.GetValue<bool?>(nameof(UserConfiguration.ShowTaskbarWhenExit)) ?? true,
                RunAtStartup =
                    _store.GetValue<bool?>(nameof(UserConfiguration.RunAtStartup))
                    ?? IsStartupEnabled()
            };

            // Keep registry in sync with persisted preference.
            ApplyStartup(_configuration.RunAtStartup);
        }

        public AutoModeType AutoModeType
        {
            get => _configuration.AutoModeType;
            set
            {
                if (value == _configuration.AutoModeType)
                    return;

                _configuration.AutoModeType = value;
                _store.SetValue(nameof(UserConfiguration.AutoModeType), value.ToString());
            }
        }

        public bool ShowTaskbarWhenExit
        {
            get => _configuration.ShowTaskbarWhenExit;
            set
            {
                if (value == _configuration.ShowTaskbarWhenExit)
                    return;

                _configuration.ShowTaskbarWhenExit = value;
                _store.SetValue(nameof(UserConfiguration.ShowTaskbarWhenExit), value);
            }
        }

        public bool RunAtStartup
        {
            get => _configuration.RunAtStartup;
            set
            {
                if (value == _configuration.RunAtStartup)
                    return;

                _configuration.RunAtStartup = value;
                _store.SetValue(nameof(UserConfiguration.RunAtStartup), value);
                ApplyStartup(value);
            }
        }

        public static string GetModeDisplayName(AutoModeType mode)
            => mode switch
            {
                AutoModeType.Auto => "Auto",
                AutoModeType.MaximizeHide => "MaximizeHide",
                _ => "Off"
            };

        private static bool IsStartupEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", false);
                return key?.GetValue(StartupValueName) is string;
            }
            catch
            {
                return false;
            }
        }

        private static void ApplyStartup(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (key is null)
                    return;

                if (enable)
                {
                    var exe = Environment.ProcessPath
                               ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                    if (!string.IsNullOrEmpty(exe))
                        key.SetValue(StartupValueName, $"\"{exe}\"");
                }
                else
                {
                    key.DeleteValue(StartupValueName, false);
                }
            }
            catch
            {
                // ignore registry errors
            }
        }
    }
}