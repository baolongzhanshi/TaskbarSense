using SmartTaskbar.Win11.Abstractions;

namespace SmartTaskbar.Win11.Worker
{
    public class MaximizeDetector
    {
        private readonly IWindowEnumerationService _windowEnumeration;
        private readonly IWindowStateService _windowState;
        private readonly IMonitorService _monitorService;

        private IntPtr _cachedMaximizedHandle;
        private IntPtr _cachedMonitor;

        private static readonly HashSet<string> ExcludedClassNames = new(StringComparer.Ordinal)
        {
            "Shell_TrayWnd",
            "Shell_SecondaryTrayWnd",
            "Progman",
            "WorkerW",
            "Windows.UI.Core.CoreWindow",
            "XamlExplorerHostIslandWindow",
            "ForegroundStaging"
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
            // Fast path: re-validate previous hit first.
            if (_cachedMaximizedHandle != IntPtr.Zero
                && _monitorService.IsSameMonitor(_cachedMonitor, targetMonitor)
                && IsCandidate(_cachedMaximizedHandle, targetMonitor))
            {
                return true;
            }

            _cachedMaximizedHandle = IntPtr.Zero;
            _cachedMonitor = IntPtr.Zero;

            var handles = _windowEnumeration.EnumerateTopLevelWindows();

            foreach (var handle in handles)
            {
                if (!IsCandidate(handle, targetMonitor))
                    continue;

                _cachedMaximizedHandle = handle;
                _cachedMonitor = targetMonitor;
                return true;
            }

            return false;
        }

        private bool IsCandidate(IntPtr handle, IntPtr targetMonitor)
        {
            if (!_windowState.IsVisible(handle))
                return false;

            // Cheaper check before class name string work.
            if (!_windowState.IsMaximized(handle) && !_windowState.IsFullscreen(handle))
                return false;

            var className = _windowState.GetClassName(handle);
            if (ExcludedClassNames.Contains(className))
                return false;

            // ApplicationFrameWindow can host UWP; keep only if truly maximized/fullscreen (already checked).
            var windowMonitor = _monitorService.GetMonitorFromWindow(handle);
            return _monitorService.IsSameMonitor(windowMonitor, targetMonitor);
        }
    }
}