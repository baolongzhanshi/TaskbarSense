using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Win32;
using SmartTaskbar.Win11.Helpers;
using SmartTaskbar.Win11.Languages;
using SmartTaskbar.Win11.Models;
using SmartTaskbar.Win11.Worker.Services;

namespace SmartTaskbar.Win11
{
    internal class SystemTray : ApplicationContext
    {
        private const int TrayTolerance = 4;
        private const string ProductDisplayName = "TaskbarSense";

        private readonly ToolStripMenuItem _animationInBar;
        private readonly ToolStripMenuItem _autoMode;
        private readonly ToolStripMenuItem _maximizeHideMode;
        private readonly ToolStripMenuItem _runAtStartup;
        private readonly ToolStripMenuItem _showBarOnExit;
        private readonly ToolStripMenuItem _exit;

        private readonly Container _container = new();
        private readonly ContextMenuStrip _contextMenuStrip;
        private readonly Engine _engine;
        private readonly NotifyIcon _notifyIcon;
        private readonly ResourceCulture _resourceCulture = new();
        private readonly TaskbarAlignmentHelper _taskbarAlignment;

        public SystemTray()
        {
            UserSettings.Instance = new UserSettings(new LocalSettingsStore());
            _taskbarAlignment = new TaskbarAlignmentHelper(new WindowsRegistryReader());

            #region Initialization

            _engine = new Engine(_container);

            var font = new Font("Segoe UI", 10.5F);

            var about = new ToolStripMenuItem(_resourceCulture.GetString(LangName.About)) { Font = font };
            _animationInBar = new ToolStripMenuItem(_resourceCulture.GetString(LangName.Animation)) { Font = font };
            _showBarOnExit = new ToolStripMenuItem(_resourceCulture.GetString(LangName.ShowBarOnExit)) { Font = font };
            _runAtStartup = new ToolStripMenuItem(_resourceCulture.GetString(LangName.RunAtStartup)) { Font = font };
            _autoMode = new ToolStripMenuItem(_resourceCulture.GetString(LangName.Auto)) { Font = font };
            _maximizeHideMode = new ToolStripMenuItem(_resourceCulture.GetString(LangName.MaximizeHide)) { Font = font };
            _exit = new ToolStripMenuItem(_resourceCulture.GetString(LangName.Exit)) { Font = font };

            _contextMenuStrip = new ContextMenuStrip(_container)
            {
                Renderer = new Win11Renderer()
            };

            _contextMenuStrip.Items.AddRange(new ToolStripItem[]
            {
                about,
                _animationInBar,
                new ToolStripSeparator(),
                _autoMode,
                _maximizeHideMode,
                new ToolStripSeparator(),
                _runAtStartup,
                _showBarOnExit,
                _exit
            });

            _notifyIcon = new NotifyIcon(_container)
            {
                Text = BuildTooltip(),
                Icon = Fun.IsLightThemeSafe() ? IconResource.Logo_Black : IconResource.Logo_White,
                Visible = true
            };

            #endregion

            #region Load Event

            about.Click += AboutOnClick;
            _animationInBar.Click += AnimationInBarOnClick;
            _showBarOnExit.Click += ShowBarOnExitOnClick;
            _runAtStartup.Click += RunAtStartupOnClick;
            _autoMode.Click += AutoModeOnClick;
            _maximizeHideMode.Click += MaximizeHideModeOnClick;
            _exit.Click += ExitOnClick;
            _notifyIcon.MouseClick += NotifyIconOnMouseClick;
            _notifyIcon.MouseDoubleClick += NotifyIconOnMouseDoubleClick;
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

            #endregion

            UpdateTrayTooltip();
        }

        private static void AboutOnClick(object? sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/baolongzhanshi/SmartTaskbar.Win11",
                    UseShellExecute = true
                });
            }
            catch { /* ignore */ }
        }

        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category != UserPreferenceCategory.General)
                return;

            void UpdateIcon()
            {
                _notifyIcon.Icon = Fun.IsLightThemeSafe()
                    ? IconResource.Logo_Black
                    : IconResource.Logo_White;
            }

            if (_contextMenuStrip.IsHandleCreated)
                _contextMenuStrip.BeginInvoke(UpdateIcon);
            else
                UpdateIcon();
        }

        private void NotifyIconOnMouseDoubleClick(object? s, MouseEventArgs e)
        {
            UserSettings.Instance.AutoModeType = AutoModeType.None;
            Fun.CancelAutoHide();
            UpdateModeCheckState();
            UpdateTrayTooltip();
        }

        private void NotifyIconOnMouseClick(object? s, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            _animationInBar.Checked = Fun.IsEnableTaskbarAnimation();
            _showBarOnExit.Checked = UserSettings.Instance.ShowTaskbarWhenExit;
            _runAtStartup.Checked = UserSettings.Instance.RunAtStartup;
            UpdateModeCheckState();
            ShowMenu();
            Fun.SetForegroundWindow(_contextMenuStrip.Handle);
        }

        private void UpdateModeCheckState()
        {
            var currentMode = UserSettings.Instance.AutoModeType;
            _autoMode.Checked = currentMode == AutoModeType.Auto;
            _maximizeHideMode.Checked = currentMode == AutoModeType.MaximizeHide;
        }

        private void UpdateTrayTooltip()
        {
            _notifyIcon.Text = BuildTooltip();
        }

        private static string BuildTooltip()
        {
            var mode = UserSettings.GetModeDisplayName(UserSettings.Instance.AutoModeType);
            var text = $"{ProductDisplayName} | {mode}";
            return text.Length <= 63 ? text : text[..63];
        }

        private void ShowMenu()
        {
            var taskbar = TaskbarHelper.InitTaskbar();
            if (taskbar.Handle == IntPtr.Zero)
                return;

            var taskbarScreen = Screen.FromHandle(taskbar.Handle);
            var screenBounds = taskbarScreen.Bounds;
            var centered = _taskbarAlignment.IsCentered;

            switch (taskbar.Position)
            {
                case TaskbarPosition.Bottom:
                {
                    var menuY = taskbar.Rect.top - _contextMenuStrip.Height - TrayTolerance;
                    var menuX = centered
                        ? Cursor.Position.X - _contextMenuStrip.Width / 2
                        : Cursor.Position.X - TrayTolerance;

                    menuX = Math.Max(screenBounds.Left + TrayTolerance,
                        Math.Min(menuX, screenBounds.Right - _contextMenuStrip.Width - TrayTolerance));

                    _contextMenuStrip.Show(menuX, menuY);
                    break;
                }
                case TaskbarPosition.Left:
                {
                    var menuX = taskbar.Rect.right + TrayTolerance;
                    var menuY = Cursor.Position.Y - TrayTolerance;
                    if (menuY + _contextMenuStrip.Height > screenBounds.Bottom)
                        menuY = screenBounds.Bottom - _contextMenuStrip.Height - TrayTolerance;
                    _contextMenuStrip.Show(menuX, menuY);
                    break;
                }
                case TaskbarPosition.Right:
                {
                    var menuX = taskbar.Rect.left - TrayTolerance - _contextMenuStrip.Width;
                    var menuY = Cursor.Position.Y - TrayTolerance;
                    if (menuY + _contextMenuStrip.Height > screenBounds.Bottom)
                        menuY = screenBounds.Bottom - _contextMenuStrip.Height - TrayTolerance;
                    _contextMenuStrip.Show(menuX, menuY);
                    break;
                }
                case TaskbarPosition.Top:
                {
                    var menuY = taskbar.Rect.bottom + TrayTolerance;
                    var menuX = centered
                        ? Cursor.Position.X - _contextMenuStrip.Width / 2
                        : Cursor.Position.X - TrayTolerance;
                    menuX = Math.Max(screenBounds.Left + TrayTolerance,
                        Math.Min(menuX, screenBounds.Right - _contextMenuStrip.Width - TrayTolerance));
                    _contextMenuStrip.Show(menuX, menuY);
                    break;
                }
            }
        }

        private static void HideBar()
        {
            if (Fun.IsNotAutoHide())
                return;

            var taskbar = TaskbarHelper.InitTaskbar();
            if (taskbar.Handle != IntPtr.Zero)
                taskbar.HideTaskbar();
        }

        private void ExitOnClick(object? s, EventArgs e)
        {
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;

            // Exit policy: default restores normal taskbar; uncheck keeps auto-hide as-is.
            if (UserSettings.Instance.ShowTaskbarWhenExit)
                Fun.CancelAutoHide();
            else
                HideBar();

            _notifyIcon.Visible = false;
            _container.Dispose();
            Application.Exit();
        }

        private void ShowBarOnExitOnClick(object? s, EventArgs e)
            => UserSettings.Instance.ShowTaskbarWhenExit = !_showBarOnExit.Checked;

        private void RunAtStartupOnClick(object? s, EventArgs e)
        {
            UserSettings.Instance.RunAtStartup = !_runAtStartup.Checked;
            _runAtStartup.Checked = UserSettings.Instance.RunAtStartup;
        }

        private void AutoModeOnClick(object? s, EventArgs e)
        {
            if (_autoMode.Checked)
            {
                UserSettings.Instance.AutoModeType = AutoModeType.None;
                Fun.CancelAutoHide();
            }
            else
            {
                UserSettings.Instance.AutoModeType = AutoModeType.Auto;
                Engine.RequestRefresh();
            }

            UpdateModeCheckState();
            UpdateTrayTooltip();
        }

        private void MaximizeHideModeOnClick(object? s, EventArgs e)
        {
            if (_maximizeHideMode.Checked)
            {
                UserSettings.Instance.AutoModeType = AutoModeType.None;
                Fun.CancelAutoHide();
            }
            else
            {
                UserSettings.Instance.AutoModeType = AutoModeType.MaximizeHide;
                Engine.RequestRefresh();
            }

            UpdateModeCheckState();
            UpdateTrayTooltip();
        }

        private void AnimationInBarOnClick(object? s, EventArgs e)
            => _animationInBar.Checked = Fun.ChangeTaskbarAnimation();
    }
}