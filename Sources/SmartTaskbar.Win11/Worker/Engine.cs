using System.ComponentModel;
using System.Diagnostics;
using SmartTaskbar.Win11.Worker;
using SmartTaskbar.Win11.Worker.Services;
using SmartTaskbar.Win11.Models;
using Timer = System.Windows.Forms.Timer;

namespace SmartTaskbar.Win11
{
    internal sealed class Engine
    {
        private static Timer _timer;

        private static int _timerCount;
        private static TaskbarInfo _taskbar;

        private static readonly HashSet<IntPtr> NonMouseOverShowHandleSet = new();
        private static readonly HashSet<IntPtr> NonDesktopShowHandleSet = new();
        private static readonly HashSet<IntPtr> NonForegroundShowHandleSet = new();
        private static readonly HashSet<IntPtr> DesktopHandleSet = new();
        private static readonly Stack<IntPtr> LastHideForegroundHandle = new();
        private static ForegroundWindowInfo _currentForegroundWindow;

        private static MaximizeDetector _maximizeDetector;

        public Engine(Container container)
        {
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
        }

        private static void Timer_Tick(object? sender, EventArgs e)
        {
            var mode = UserSettings.Instance.AutoModeType;

            if (mode == AutoModeType.None)
                return;

            if (_timerCount % 5 == 0)
            {
                Fun.SetAutoHide();

                _taskbar = TaskbarHelper.InitTaskbar();

                if (_taskbar.Handle == IntPtr.Zero)
                    return;
            }

            switch (mode)
            {
                case AutoModeType.Auto:
                    HandleAutoMode();
                    break;
                case AutoModeType.MaximizeHide:
                    HandleMaximizeHideMode();
                    break;
            }

            ++_timerCount;

            if (_timerCount <= 7200) return;

            _timerCount = 0;

            DesktopHandleSet.Clear();
            NonMouseOverShowHandleSet.Clear();
            NonDesktopShowHandleSet.Clear();
            NonForegroundShowHandleSet.Clear();
        }

        #region MaximizeHide Mode

        private static void HandleMaximizeHideMode()
        {
            switch (_taskbar.CheckIfMouseOver(NonMouseOverShowHandleSet))
            {
                case TaskbarBehavior.DoNothing:
                    return;
                case TaskbarBehavior.Show:
                    _taskbar.ShowTaskar();
                    return;
                case TaskbarBehavior.Pending:
                    break;
            }

            if (_maximizeDetector.HasMaximizedWindowOnMonitor(_taskbar.Monitor))
                _taskbar.HideTaskbar();
            else
                _taskbar.ShowTaskar();
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
                    _taskbar.ShowTaskar();
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
                    {
                        BeforeShowBar();
                    }

                    break;
                case TaskbarBehavior.Show:
                    BeforeShowBar();
                    break;
                case TaskbarBehavior.Hide:
                    if (info == _currentForegroundWindow) return;

                    if (!LastHideForegroundHandle.Contains(info.Handle)
                        && info.Rect.AreaCompare())
                        LastHideForegroundHandle.Push(info.Handle);

                    _taskbar.HideTaskbar();
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

            _taskbar.ShowTaskar();
        }

        #endregion
    }
}
