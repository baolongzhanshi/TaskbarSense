namespace SmartTaskbar.Win11.Abstractions
{
    public interface ISettingsStore
    {
        T? GetValue<T>(string key);

        void SetValue<T>(string key, T value);
    }
}
