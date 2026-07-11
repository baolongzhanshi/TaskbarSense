using Microsoft.Win32;

namespace SmartTaskbar.Win11.Helpers
{
    public class WindowsRegistryReader : IRegistryReader
    {
        public int? GetDwordValue(string keyPath, string valueName)
        {
            using var key = Registry.CurrentUser.OpenSubKey(keyPath, false);
            return key?.GetValue(valueName) as int?;
        }
    }
}
