using SmartTaskbar.Win11.Models;

namespace SmartTaskbar.Win11
{
    /// <summary>
    ///     User settings configuration
    /// </summary>
    internal struct UserConfiguration
    {
        public AutoModeType AutoModeType { get; set; }

        public bool ShowTaskbarWhenExit { get; set; }

        public bool RunAtStartup { get; set; }

        /// <summary>
        ///     Whether the first-run tray tip was already shown.
        /// </summary>
        public bool FirstRunTipShown { get; set; }
    }
}