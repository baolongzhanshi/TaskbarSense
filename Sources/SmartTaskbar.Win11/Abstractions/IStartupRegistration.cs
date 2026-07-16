namespace SmartTaskbar.Win11.Abstractions
{
    /// <summary>
    /// Abstraction over HKCU Run registration for TaskbarSense startup.
    /// </summary>
    public interface IStartupRegistration
    {
        bool IsCurrentEnabled();

        bool IsAnyLegacyEnabled();

        void PurgeLegacyEntries();

        void SetEnabled(string executablePath);

        void RemoveCurrent();

        string? GetExecutablePath();
    }
}