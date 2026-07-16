using System.ComponentModel;
using System.Threading;
using SmartTaskbar.Win11.Abstractions;
using SmartTaskbar.Win11.Worker;
using SmartTaskbar.Win11.Worker.Services;
using SmartTaskbar.Win11.Models;
using Microsoft.Win32;
using Timer = System.Windows.Forms.Timer;

namespace SmartTaskbar.Win11
{
    internal sealed class Engine : IDisposable
    {
        private Timer? _timer;

        private static int _timerCount;
        private static TaskbarInfo _taskbar;

        private static readonly HashSet<IntPtr> NonMouseOverShowHandleSet = new();
        private static readonly HashSet<IntPtr> NonDesktopShowHandleSet = new();
        private static readonly HashSet<IntPtr> NonForegroundShowHandleSet = new();
        private static readonly HashSet<IntPtr> DesktopHandleSet = new();
        private static readonly Stack<IntPtr> LastHideForegroundHandle = new();
        private static ForegroundWindowInfo _currentForegroundWindow;

        private static MaximizeDetector _maximizeDetector = null!;
        private static ITaskbarControlService _taskbarControl = null!;
        private static SynchronizationContext? _uiContext;
        private static bool _displayHooksRegistered;
        private bool _disposed;

        public Engine(Container container)
        {
            _uiContext = SynchronizationContext.Current;

            _timer = new Timer(container)
            {
                Interval = 125
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            _maximizeDetector = new MaximizeDetector(
                new WindowEnumerationService(),
                new WindowStateService(),
                new MonitorService());

            _taskbarControl = new TaskbarControlService();
            RegisterDisplayHooks();
        }

        /// <param name="ensureAutoHide">
        ///     When true (e.g. user just enabled a smart mode), force system auto-hide immediately
        ///     instead of waiting for the next periodic tick.
        /// </param>
        public static void RequestRefresh(bool ensureAutoHide = false)
        {
            void DoRefresh()
            {
                ClearCaches();
                if (ensureAutoHide
                    || UserSettings.Instance.AutoModeType != AutoModeType.None)
                {
                    _taskbarControl?.SetAutoHide();
                }

                _taskbar = TaskbarHelper.InitTaskbar();
            }

            var ctx = _uiContext;
            if (ctx is null || ReferenceEquals(SynchronizationContext.Current, ctx))
            {
                DoRefresh();
                return;
            }

            ctx.Post(_ => DoRefresh(), null);
        }

        private static void RegisterDisplayHooks()
        {
            if (_displayHooksRegistered)
                return;

            try
            {
                SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
                SystemEvents.SessionSwitch += OnSessionSwitch;
                _displayHooksRegistered = true;
            }
            catch
            {
                // SystemEvents may fail in some sessions
            }
        }

        private static void UnregisterDisplayHooks()
        {
            if (!_displayHooksRegistered)
                return;

            try
            {
                SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
                SystemEvents.SessionSwitch -= OnSessionSwitch;
            }
            catch
            {
                // ignore
            }

            _displayHooksRegistered = false;
        }

        private static void OnDisplaySettingsChanged(object? sender, EventArgs e)
            => RequestRefresh();

        private static void OnSessionSwitch(object? sender, SessionSwitchEventArgs e)
        {
            if (e.Reason is SessionSwitchReason.SessionUnlock or SessionSwitchReason.ConsoleConnect)
                RequestRefresh();
        }

        private static void ClearCaches()
        {
            DesktopHandleSet.Clear();
            NonMouseOverShowHandleSet.Clear();
            NonDesktopShowHandleSet.Clear();
            NonForegroundShowHandleSet.Clear();
            LastHideForegroundHandle.Clear();
            _currentForegroundWindow = default;
        }

        private static void Timer_Tick(object? sender, EventArgs e)
        {
            var mode = UserSettings.Instance.AutoModeType;

            if (mode == AutoModeType.None)
                return;

            if (_timerCount % 5 == 0)
            {
                _taskbarControl.SetAutoHide();
                _taskbar = TaskbarHelper.InitTaskbar();
            }
            else if (_taskbar.Handle == IntPtr.Zero)
            {
                _taskbar = TaskbarHelper.InitTaskbar();
            }

            if (_taskbar.Handle != IntPtr.Zero)
            {
                switch (mode)
                {
                    case AutoModeType.Auto:
                        HandleAutoMode();
                        break;
                    case AutoModeType.MaximizeHide:
                        HandleMaximizeHideMode();
                        break;
                }
            }

            ++_timerCount;

            if (_timerCount <= 7200) return;

            _timerCount = 0;
            ClearCaches();
        }

        #region MaximizeHide Mode

        private const int MaximizeScanIntervalTicks = 3;

        private static void HandleMaximizeHideMode()
        {
            switch (_taskbar.CheckIfMouseOver(NonMouseOverShowHandleSet))
            {
                case TaskbarBehavior.DoNothing:
                    return;
                case TaskbarBehavior.Show:
                    _taskbarControl.ShowTaskbar(in _taskbar);
                    return;
                case TaskbarBehavior.Pending:
                    break;
            }

            if (_timerCount % MaximizeScanIntervalTicks != 0)
                return;

            MaximizeHidePolicy.Apply(in _taskbar, _maximizeDetector, _taskbarControl);
        }

        #endregion

        #region Auto Mode

        private static void HandleAutoMode()
        {
            switch (_taskbar.CheckIfMouseOver(NonMouseOverShowHandleSet))
            {
                case TaskbarBehavior.DoNothing:
                    break;
                case TaskbarBehavior.Pending:
                    CheckCurrentWindow();
                    break;
                case TaskbarBehavior.Show:
                    _taskbarControl.ShowTaskbar(in _taskbar);
                    break;
            }
        }

        private static void CheckCurrentWindow()
        {
            var behavior =
                _taskbar.CheckIfForegroundWindowIntersectTaskbar(DesktopHandleSet,
                                                                 NonForegroundShowHandleSet,
                                                                 out var info);

            switch (behavior)
            {
                case TaskbarBehavior.DoNothing:
                    break;
                case TaskbarBehavior.Pending:
                    if (_taskbar.CheckIfDesktopShow(DesktopHandleSet, NonDesktopShowHandleSet))
                        BeforeShowBar();
                    break;
                case TaskbarBehavior.Show:
                    BeforeShowBar();
                    break;
                case TaskbarBehavior.Hide:
                    if (info == _currentForegroundWindow) return;

                    if (!LastHideForegroundHandle.Contains(info.Handle)
                        && info.Rect.AreaCompare(_taskbar.Handle))
                        LastHideForegroundHandle.Push(info.Handle);

                    _taskbarControl.HideTaskbar(in _taskbar);
                    break;
            }

            _currentForegroundWindow = info;
        }

        private static void BeforeShowBar()
        {
            while (LastHideForegroundHandle.Count != 0)
            {
                if (_taskbar.CheckIfWindowShouldHideTaskbar(LastHideForegroundHandle.Peek()))
                    return;

                LastHideForegroundHandle.Pop();
            }

            _taskbarControl.ShowTaskbar(in _taskbar);
        }

        #endregion

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            UnregisterDisplayHooks();

            if (_timer is not null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                _timer = null;
            }

            ClearCaches();
        }
    }
}