using SmartTaskbar.Win11.Abstractions;

namespace SmartTaskbar.Win11.Models
{
    /// <summary>
    /// Coordinates startup Run-key policy without depending on the Windows Registry API.
    /// </summary>
    public static class StartupRegistrationCoordinator
    {
        public static void Apply(IStartupRegistration registration, bool enable)
        {
            registration.PurgeLegacyEntries();

            if (enable)
            {
                var exe = registration.GetExecutablePath();
                if (!string.IsNullOrWhiteSpace(exe))
                    registration.SetEnabled(exe);
                return;
            }

            registration.RemoveCurrent();
        }

        public static bool IsEffectivelyEnabled(IStartupRegistration registration)
            => registration.IsCurrentEnabled() || registration.IsAnyLegacyEnabled();
    }
}