using SmartTaskbar.Win11.Models;

namespace SmartTaskbar.Win11
{
    /// <summary>
    ///     User settings configuration
    /// </summary>
    internal struct UserConfiguration
    {
        /// <summary>
        ///     Auto mode type
        /// </summary>
        public AutoModeType AutoModeType { get; set; }

        /// <summary>
        ///     Show / restore taskbar when exiting
        /// </summary>
        public bool ShowTaskbarWhenExit { get; set; }

        /// <summary>
        ///     Launch at Windows logon
        /// </summary>
        public bool RunAtStartup { get; set; }
    }
}