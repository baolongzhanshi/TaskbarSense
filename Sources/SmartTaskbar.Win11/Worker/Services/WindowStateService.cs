using SmartTaskbar.Win11.Abstractions;
using SmartTaskbar.Win11.Models;

namespace SmartTaskbar.Win11.Worker.Services
{
    using static SmartTaskbar.Win11.Fun;

    public class WindowStateService : IWindowStateService
    {
        private const int DwmwaCloaked = 14;
        private const int MonitorDefaultToNearest = 2;

        public bool IsMaximized(IntPtr handle) => IsZoomed(handle);

        public bool IsVisible(IntPtr handle)
        {
            if (IsWindowVisible(handle) == false) return false;

            DwmGetWindowAttribute(handle, DwmwaCloaked, out bool cloaked, sizeof(int));
            return !cloaked;
        }

        public string GetClassName(IntPtr handle) => handle.GetClassName();

        /// <summary>
        ///     Detect borderless / nearly-fullscreen windows that are not classic maximized.
        /// </summary>
        public bool IsFullscreen(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                return false;

            if (!GetWindowRect(handle, out var rect))
                return false;

            var monitor = MonitorFromWindow(handle, MonitorDefaultToNearest);
            if (monitor == IntPtr.Zero)
                return false;

            var info = new MonitorInfo
            {
                cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<MonitorInfo>()
            };

            if (!GetMonitorInfo(monitor, ref info))
                return false;

            var mon = info.rcMonitor;
            // Exact match to monitor work area or monitor bounds (tolerance 2px).
            return NearlyEqual(rect.left, mon.left)
                   && NearlyEqual(rect.top, mon.top)
                   && NearlyEqual(rect.right, mon.right)
                   && NearlyEqual(rect.bottom, mon.bottom);
        }

        private static bool NearlyEqual(int a, int b) => Math.Abs(a - b) <= 2;
    }
}