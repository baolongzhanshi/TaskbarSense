using SmartTaskbar.Win11.Abstractions;

namespace SmartTaskbar.Win11.Worker
{
    public class MaximizeDetector
    {
        private readonly IWindowEnumerationService _windowEnumeration;
        private readonly IWindowStateService _windowState;
        private readonly IMonitorService _monitorService;
        private readonly Func<IntPtr> _getForegroundWindow;

        private IntPtr _cachedMaximizedHandle;
        private IntPtr _cachedMonitor;

        /// <summary>
        /// Cap expensive fullscreen geometry checks during full enumeration.
        /// </summary>
        public const int MaxFullscreenChecksPerScan = 8;

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
            IMonitorService monitorService,
            Func<IntPtr>? getForegroundWindow = null)
        {
            _windowEnumeration = windowEnumeration;
            _windowState = windowState;
            _monitorService = monitorService;
            _getForegroundWindow = getForegroundWindow ?? Fun.GetForegroundWindow;
        }

        public bool HasMaximizedWindowOnMonitor(IntPtr targetMonitor)
        {
            // 1) Re-validate previous hit (maximize or fullscreen).
            if (_cachedMaximizedHandle != IntPtr.Zero
                && _monitorService.IsSameMonitor(_cachedMonitor, targetMonitor)
                && IsCandidate(_cachedMaximizedHandle, targetMonitor, allowFullscreen: true))
            {
                return true;
            }

            _cachedMaximizedHandle = IntPtr.Zero;
            _cachedMonitor = IntPtr.Zero;

            // 2) Foreground first — games/videos are usually foreground when fullscreen.
            try
            {
                var foreground = _getForegroundWindow();
                if (foreground != IntPtr.Zero
                    && IsCandidate(foreground, targetMonitor, allowFullscreen: true))
                {
                    CacheHit(foreground, targetMonitor);
                    return true;
                }
            }
            catch
            {
                // ignore
            }

            // 3) Full scan with cheap filters before expensive fullscreen geometry.
            //    Order: visible → class exclude → same monitor → maximized → limited fullscreen.
            var handles = _windowEnumeration.EnumerateTopLevelWindows();
            var fullscreenChecks = 0;

            foreach (var handle in handles)
            {
                if (!_windowState.IsVisible(handle))
                    continue;

                var className = _windowState.GetClassName(handle);
                if (ExcludedClassNames.Contains(className))
                    continue;

                var windowMonitor = _monitorService.GetMonitorFromWindow(handle);
                if (!_monitorService.IsSameMonitor(windowMonitor, targetMonitor))
                    continue;

                if (_windowState.IsMaximized(handle))
                {
                    CacheHit(handle, targetMonitor);
                    return true;
                }

                if (fullscreenChecks >= MaxFullscreenChecksPerScan)
                    continue;

                fullscreenChecks++;
                if (_windowState.IsFullscreen(handle))
                {
                    CacheHit(handle, targetMonitor);
                    return true;
                }
            }

            return false;
        }

        private void CacheHit(IntPtr handle, IntPtr monitor)
        {
            _cachedMaximizedHandle = handle;
            _cachedMonitor = monitor;
        }

        /// <summary>
        /// Shared path for cache / foreground validation.
        /// Same filter order as the full scan, but fullscreen is optional.
        /// </summary>
        private bool IsCandidate(IntPtr handle, IntPtr targetMonitor, bool allowFullscreen)
        {
            if (!_windowState.IsVisible(handle))
                return false;

            var className = _windowState.GetClassName(handle);
            if (ExcludedClassNames.Contains(className))
                return false;

            var windowMonitor = _monitorService.GetMonitorFromWindow(handle);
            if (!_monitorService.IsSameMonitor(windowMonitor, targetMonitor))
                return false;

            if (_windowState.IsMaximized(handle))
                return true;

            return allowFullscreen && _windowState.IsFullscreen(handle);
        }
    }
}