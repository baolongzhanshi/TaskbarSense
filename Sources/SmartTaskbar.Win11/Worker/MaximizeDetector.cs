using SmartTaskbar.Win11.Abstractions;

namespace SmartTaskbar.Win11.Worker
{
    public class MaximizeDetector
    {
        private readonly IWindowEnumerationService _windowEnumeration;
        private readonly IWindowStateService _windowState;
        private readonly IMonitorService _monitorService;

        private static readonly HashSet<string> ExcludedClassNames = new()
        {
            "Shell_TrayWnd",
            "Progman",
            "WorkerW",
            "Windows.UI.Core.CoreWindow"
        };

        public MaximizeDetector(
            IWindowEnumerationService windowEnumeration,
            IWindowStateService windowState,
            IMonitorService monitorService)
        {
            _windowEnumeration = windowEnumeration;
            _windowState = windowState;
            _monitorService = monitorService;
        }

        public bool HasMaximizedWindowOnMonitor(IntPtr targetMonitor)
        {
            var handles = _windowEnumeration.EnumerateTopLevelWindows();

            foreach (var handle in handles)
            {
                if (!_windowState.IsVisible(handle))
                    continue;

                var className = _windowState.GetClassName(handle);
                if (ExcludedClassNames.Contains(className))
                    continue;

                if (!_windowState.IsMaximized(handle))
                    continue;

                var windowMonitor = _monitorService.GetMonitorFromWindow(handle);
                if (_monitorService.IsSameMonitor(windowMonitor, targetMonitor))
                    return true;
            }

            return false;
        }
    }
}
