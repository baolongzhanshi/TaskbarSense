namespace SmartTaskbar.Win11.Helpers
{
    public interface IRegistryReader
    {
        int? GetDwordValue(string keyPath, string valueName);
    }
}
