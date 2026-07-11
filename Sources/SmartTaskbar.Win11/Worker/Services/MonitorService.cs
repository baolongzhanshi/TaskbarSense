using SmartTaskbar.Win11.Abstractions;

namespace SmartTaskbar.Win11.Worker.Services
{
    using static SmartTaskbar.Win11.Fun;

    public class MonitorService : IMonitorService
    {
        private const uint MonitorDefaultToNearest = 2;

        public IntPtr GetMonitorFromWindow(IntPtr windowHandle)
            => MonitorFromWindow(windowHandle, MonitorDefaultToNearest);

        public bool IsSameMonitor(IntPtr monitor1, IntPtr monitor2)
            => monitor1 == monitor2;
    }
}
