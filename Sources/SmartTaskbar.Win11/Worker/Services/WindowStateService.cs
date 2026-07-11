using SmartTaskbar.Win11.Abstractions;

namespace SmartTaskbar.Win11.Worker.Services
{
    using static SmartTaskbar.Win11.Fun;

    public class WindowStateService : IWindowStateService
    {
        private const int DwmwaCloaked = 14;

        public bool IsMaximized(IntPtr handle) => IsZoomed(handle);

        public bool IsVisible(IntPtr handle)
        {
            if (IsWindowVisible(handle) == false) return false;

            DwmGetWindowAttribute(handle, DwmwaCloaked, out bool cloaked, sizeof(int));
            return !cloaked;
        }

        public string GetClassName(IntPtr handle) => handle.GetClassName();
    }
}
