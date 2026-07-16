using SmartTaskbar.Win11.Abstractions;
using SmartTaskbar.Win11.Worker.Services;

namespace SmartTaskbar.Win11.Models
{
    public class UserSettings
    {
        private readonly ISettingsStore _store;
        private readonly IStartupRegistration _startup;
        private UserConfiguration _configuration;

        public static UserSettings Instance { get; set; } = null!;

        public UserSettings(ISettingsStore store)
            : this(store, new RegistryStartupRegistration())
        {
        }

        public UserSettings(ISettingsStore store, IStartupRegistration startup)
        {
            _store = store;
            _startup = startup;

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
                    ?? StartupRegistrationCoordinator.IsEffectivelyEnabled(_startup),
                FirstRunTipShown =
                    _store.GetValue<bool?>(nameof(UserConfiguration.FirstRunTipShown)) ?? false
            };

            StartupRegistrationCoordinator.Apply(_startup, _configuration.RunAtStartup);
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
                StartupRegistrationCoordinator.Apply(_startup, value);
            }
        }

        public bool FirstRunTipShown
        {
            get => _configuration.FirstRunTipShown;
            set
            {
                if (value == _configuration.FirstRunTipShown)
                    return;

                _configuration.FirstRunTipShown = value;
                _store.SetValue(nameof(UserConfiguration.FirstRunTipShown), value);
            }
        }

        /// <summary>
        ///     Applies startup and returns whether the effective registry state matches the request.
        /// </summary>
        public bool TrySetRunAtStartup(bool enable)
        {
            RunAtStartup = enable;
            var ok = enable
                ? _startup.IsCurrentEnabled()
                : !_startup.IsCurrentEnabled();
            return ok;
        }

        public static string GetModeDisplayName(AutoModeType mode)
            => mode switch
            {
                AutoModeType.Auto => "Auto",
                AutoModeType.MaximizeHide => "MaximizeHide",
                _ => "Off"
            };
    }
}