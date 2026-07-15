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

        // Cap expensive fullscreen geometry checks during full enumeration.
        private const int MaxFullscreenChecksPerScan = 8;

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
            // 1) Re-validate previous hit (allows maximize + fullscreen).
            if (_cachedMaximizedHandle != IntPtr.Zero
                && _monitorService.IsSameMonitor(_cachedMonitor, targetMonitor)
                && IsCandidate(_cachedMaximizedHandle, targetMonitor, allowFullscreen: true))
            {
                return true;
            }

            _cachedMaximizedHandle = IntPtr.Zero;
            _cachedMonitor = IntPtr.Zero;

            // 2) Foreground window first — most games/videos are foreground when fullscreen.
            try
            {
                var foreground = Fun.GetForegroundWindow();
                if (foreground != IntPtr.Zero
                    && IsCandidate(foreground, targetMonitor, allowFullscreen: true))
                {
                    _cachedMaximizedHandle = foreground;
                    _cachedMonitor = targetMonitor;
                    return true;
                }
            }
            catch
            {
                // ignore
            }

            // 3) Full scan: prefer cheap IsMaximized; limit fullscreen geometry probes.
            var handles = _windowEnumeration.EnumerateTopLevelWindows();
            var fullscreenChecks = 0;

            foreach (var handle in handles)
            {
                if (!_windowState.IsVisible(handle))
                    continue;

                var isMax = _windowState.IsMaximized(handle);
                var isFull = false;
                if (!isMax && fullscreenChecks < MaxFullscreenChecksPerScan)
                {
                    fullscreenChecks++;
                    isFull = _windowState.IsFullscreen(handle);
                }

                if (!isMax && !isFull)
                    continue;

                var className = _windowState.GetClassName(handle);
                if (ExcludedClassNames.Contains(className))
                    continue;

                var windowMonitor = _monitorService.GetMonitorFromWindow(handle);
                if (!_monitorService.IsSameMonitor(windowMonitor, targetMonitor))
                    continue;

                _cachedMaximizedHandle = handle;
                _cachedMonitor = targetMonitor;
                return true;
            }

            return false;
        }

        private bool IsCandidate(IntPtr handle, IntPtr targetMonitor, bool allowFullscreen)
        {
            if (!_windowState.IsVisible(handle))
                return false;

            var isMax = _windowState.IsMaximized(handle);
            if (!isMax && !(allowFullscreen && _windowState.IsFullscreen(handle)))
                return false;

            var className = _windowState.GetClassName(handle);
            if (ExcludedClassNames.Contains(className))
                return false;

            var windowMonitor = _monitorService.GetMonitorFromWindow(handle);
            return _monitorService.IsSameMonitor(windowMonitor, targetMonitor);
        }
    }
}