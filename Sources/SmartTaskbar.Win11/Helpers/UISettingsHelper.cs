using Microsoft.Win32;

namespace SmartTaskbar.Win11
{
    public static partial class Fun
    {
        /// <summary>
        ///     Determine whether it is a light theme
        /// </summary>
        public static bool IsLightTheme()
        {
            using var personalizeKey =
                Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", false)
                ?? throw new InvalidOperationException("OpenSubKey Personalize Failed.");

            return (int) (personalizeKey.GetValue("SystemUsesLightTheme", 0) ?? 0) == 1;
        }

        /// <summary>
        ///     Get current light theme status, returns false on any error
        /// </summary>
        public static bool IsLightThemeSafe()
        {
            try { return IsLightTheme(); }
            catch { return false; }
        }
    }
}