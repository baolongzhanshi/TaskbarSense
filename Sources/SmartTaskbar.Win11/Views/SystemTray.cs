using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Win32;
using SmartTaskbar.Win11.Helpers;
using SmartTaskbar.Win11.Languages;
using SmartTaskbar.Win11.Models;
using SmartTaskbar.Win11.Worker.Services;
using Timer = System.Windows.Forms.Timer;

namespace SmartTaskbar.Win11
{
    internal class SystemTray : ApplicationContext
    {
        private const int TrayTolerance = 4;
        private const string ProductDisplayName = "TaskbarSense";
        private const string RepoUrl = "https://github.com/baolongzhanshi/TaskbarSense";

        // Ignore a right-click that arrives immediately after a double-click.
        private const int DoubleClickGuardMs = 400;

        private readonly ToolStripMenuItem _animationInBar;
        private readonly ToolStripMenuItem _autoMode;
        private readonly ToolStripMenuItem _maximizeHideMode;
        private readonly ToolStripMenuItem _runAtStartup;
        private readonly ToolStripMenuItem _showBarOnExit;
        private readonly ToolStripMenuItem _exit;
        private readonly ToolStripMenuItem _about;

        private readonly Container _container = new();
        private readonly ContextMenuStrip _contextMenuStrip;
        private readonly Engine _engine;
        private readonly NotifyIcon _notifyIcon;
        private readonly ResourceCulture _resourceCulture = new();
        private readonly TaskbarAlignmentHelper _taskbarAlignment;
        private readonly Timer _firstRunTipTimer;

        private DateTime _ignoreRightClickUntil = DateTime.MinValue;
        private bool _shownModeHint;

        public SystemTray()
        {
            UserSettings.Instance = new UserSettings(new LocalSettingsStore());
            _taskbarAlignment = new TaskbarAlignmentHelper(new WindowsRegistryReader());

            #region Initialization

            _engine = new Engine(_container);

            var font = new Font("Segoe UI", 10.5F);

            _about = new ToolStripMenuItem(_resourceCulture.GetString(LangName.About)) { Font = font };
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
                _about,
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

            _firstRunTipTimer = new Timer(_container) { Interval = 1200 };
            _firstRunTipTimer.Tick += FirstRunTipTimerOnTick;

            #endregion

            #region Load Event

            _about.Click += AboutOnClick;
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

            if (!UserSettings.Instance.FirstRunTipShown)
                _firstRunTipTimer.Start();
        }

        private void FirstRunTipTimerOnTick(object? sender, EventArgs e)
        {
            _firstRunTipTimer.Stop();
            if (UserSettings.Instance.FirstRunTipShown)
                return;

            ShowBalloon(
                _resourceCulture.GetString(LangName.FirstRunTitle),
                _resourceCulture.GetString(LangName.FirstRunText),
                ToolTipIcon.Info,
                6000);

            UserSettings.Instance.FirstRunTipShown = true;
        }

        private void AboutOnClick(object? sender, EventArgs e)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "2.2.0";
            var title = _resourceCulture.GetString(LangName.AboutTitle);
            var bodyTemplate = _resourceCulture.GetString(LangName.AboutBody);
            if (string.IsNullOrWhiteSpace(title))
                title = ProductDisplayName;
            if (string.IsNullOrWhiteSpace(bodyTemplate))
                bodyTemplate = "Version {0}";

            var body = string.Format(bodyTemplate, version);
            var result = MessageBox.Show(
                body,
                title,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result != DialogResult.Yes)
                return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = RepoUrl,
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
            // Suppress a trailing right-click that WinForms may raise around double-click.
            _ignoreRightClickUntil = DateTime.UtcNow.AddMilliseconds(DoubleClickGuardMs);

            if (_contextMenuStrip.Visible)
                _contextMenuStrip.Close();

            var wasOn = UserSettings.Instance.AutoModeType != AutoModeType.None;
            UserSettings.Instance.AutoModeType = AutoModeType.None;
            Fun.CancelAutoHide();
            UpdateModeCheckState();
            UpdateTrayTooltip();

            if (wasOn)
            {
                ShowBalloon(
                    _resourceCulture.GetString(LangName.ModeOffTipTitle),
                    _resourceCulture.GetString(LangName.ModeOffTipText),
                    ToolTipIcon.Info,
                    3000);
            }
        }

        private void NotifyIconOnMouseClick(object? s, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            if (DateTime.UtcNow < _ignoreRightClickUntil)
                return;

            // Immediate menu — no artificial delay.
            OpenContextMenu();
        }

        private void OpenContextMenu()
        {
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

        private string BuildTooltip()
        {
            var mode = UserSettings.Instance.AutoModeType switch
            {
                AutoModeType.Auto => _resourceCulture.GetString(LangName.Auto),
                AutoModeType.MaximizeHide => _resourceCulture.GetString(LangName.MaximizeHide),
                _ => _resourceCulture.GetString(LangName.ModeOff)
            };

            if (string.IsNullOrWhiteSpace(mode))
                mode = UserSettings.GetModeDisplayName(UserSettings.Instance.AutoModeType);

            var hint = _resourceCulture.GetString(LangName.TooltipHint);
            if (string.IsNullOrWhiteSpace(hint))
                hint = "Right-click";

            // NotifyIcon.Text max length is 63.
            var text = $"{ProductDisplayName} · {mode}";
            if (text.Length + hint.Length + 3 <= 63)
                text = $"{text} · {hint}";

            return text.Length <= 63 ? text : text[..63];
        }

        private void ShowMenu()
        {
            var taskbar = TaskbarHelper.InitTaskbar();
            if (taskbar.Handle == IntPtr.Zero)
            {
                // Fallback near cursor if taskbar handle is missing.
                _contextMenuStrip.Show(Cursor.Position);
                return;
            }

            var taskbarScreen = Screen.FromHandle(taskbar.Handle);
            var screenBounds = taskbarScreen.Bounds;
            var centered = _taskbarAlignment.IsCentered;

            _contextMenuStrip.PerformLayout();
            var menuSize = _contextMenuStrip.GetPreferredSize(Size.Empty);
            if (menuSize.Width <= 0 || menuSize.Height <= 0)
                menuSize = _contextMenuStrip.Size;

            switch (taskbar.Position)
            {
                case TaskbarPosition.Bottom:
                {
                    var menuY = taskbar.Rect.top - menuSize.Height - TrayTolerance;
                    var menuX = centered
                        ? Cursor.Position.X - menuSize.Width / 2
                        : Cursor.Position.X - TrayTolerance;

                    menuX = Math.Max(screenBounds.Left + TrayTolerance,
                        Math.Min(menuX, screenBounds.Right - menuSize.Width - TrayTolerance));

                    _contextMenuStrip.Show(menuX, menuY);
                    break;
                }
                case TaskbarPosition.Left:
                {
                    var menuX = taskbar.Rect.right + TrayTolerance;
                    var menuY = Cursor.Position.Y - TrayTolerance;
                    if (menuY + menuSize.Height > screenBounds.Bottom)
                        menuY = screenBounds.Bottom - menuSize.Height - TrayTolerance;
                    _contextMenuStrip.Show(menuX, menuY);
                    break;
                }
                case TaskbarPosition.Right:
                {
                    var menuX = taskbar.Rect.left - TrayTolerance - menuSize.Width;
                    var menuY = Cursor.Position.Y - TrayTolerance;
                    if (menuY + menuSize.Height > screenBounds.Bottom)
                        menuY = screenBounds.Bottom - menuSize.Height - TrayTolerance;
                    _contextMenuStrip.Show(menuX, menuY);
                    break;
                }
                case TaskbarPosition.Top:
                {
                    var menuY = taskbar.Rect.bottom + TrayTolerance;
                    var menuX = centered
                        ? Cursor.Position.X - menuSize.Width / 2
                        : Cursor.Position.X - TrayTolerance;
                    menuX = Math.Max(screenBounds.Left + TrayTolerance,
                        Math.Min(menuX, screenBounds.Right - menuSize.Width - TrayTolerance));
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
            _firstRunTipTimer.Stop();
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
            _engine.Dispose();

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
            var enable = !_runAtStartup.Checked;
            var ok = UserSettings.Instance.TrySetRunAtStartup(enable);
            _runAtStartup.Checked = UserSettings.Instance.RunAtStartup;

            if (!ok)
            {
                ShowBalloon(
                    ProductDisplayName,
                    _resourceCulture.GetString(LangName.StartupFailed),
                    ToolTipIcon.Warning,
                    5000);
                return;
            }

            ShowBalloon(
                ProductDisplayName,
                enable
                    ? _resourceCulture.GetString(LangName.StartupOnOk)
                    : _resourceCulture.GetString(LangName.StartupOffOk),
                ToolTipIcon.Info,
                2500);
        }

        private void AutoModeOnClick(object? s, EventArgs e)
        {
            if (_autoMode.Checked)
            {
                UserSettings.Instance.AutoModeType = AutoModeType.None;
                Fun.CancelAutoHide();
                ShowModeOffTip();
            }
            else
            {
                UserSettings.Instance.AutoModeType = AutoModeType.Auto;
                Engine.RequestRefresh(ensureAutoHide: true);
                ShowModeEnabledHint();
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
                ShowModeOffTip();
            }
            else
            {
                UserSettings.Instance.AutoModeType = AutoModeType.MaximizeHide;
                Engine.RequestRefresh(ensureAutoHide: true);
                ShowModeEnabledHint();
            }

            UpdateModeCheckState();
            UpdateTrayTooltip();
        }

        private void ShowModeOffTip()
        {
            ShowBalloon(
                _resourceCulture.GetString(LangName.ModeOffTipTitle),
                _resourceCulture.GetString(LangName.ModeOffTipText),
                ToolTipIcon.Info,
                3000);
        }

        private void ShowModeEnabledHint()
        {
            if (_shownModeHint)
                return;

            _shownModeHint = true;
            ShowBalloon(
                _resourceCulture.GetString(LangName.ModeHintTitle),
                _resourceCulture.GetString(LangName.ModeHintText),
                ToolTipIcon.Info,
                5000);
        }

        private void ShowBalloon(string? title, string? text, ToolTipIcon icon, int timeoutMs)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                    return;

                _notifyIcon.BalloonTipTitle = string.IsNullOrWhiteSpace(title) ? ProductDisplayName : title;
                _notifyIcon.BalloonTipText = text;
                _notifyIcon.BalloonTipIcon = icon;
                _notifyIcon.ShowBalloonTip(timeoutMs);
            }
            catch
            {
                // balloon may be disabled by system policy
            }
        }

        private void AnimationInBarOnClick(object? s, EventArgs e)
            => _animationInBar.Checked = Fun.ChangeTaskbarAnimation();
    }
}