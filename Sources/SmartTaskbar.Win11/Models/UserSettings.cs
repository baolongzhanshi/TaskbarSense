using SmartTaskbar.Win11.Abstractions;

namespace SmartTaskbar.Win11.Models
{
    public class UserSettings
    {
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
                    _store.GetValue<bool?>(nameof(UserConfiguration.ShowTaskbarWhenExit)) ?? true
            };
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
    }
}
