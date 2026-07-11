using System.Runtime.InteropServices;
using SmartTaskbar.Win11.Models;

namespace SmartTaskbar.Win11
{
    public static partial class Fun
    {
        #region PostMessage

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PostMessage(IntPtr hWnd, uint wMsg, IntPtr wParam, IntPtr lParam);

        #endregion

        #region GetCursorPos

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out TagPoint lpPoint);

        #endregion

        #region IsWindowVisible

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        #endregion

        #region DwmGetWindowAttribute

        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr                                   hwnd,
                                                       int                                      dwAttribute,
                                                       [MarshalAs(UnmanagedType.Bool)] out bool pvAttribute,
                                                       int                                      cbAttribute);

        #endregion

        #region GetForegroundWindow

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        #endregion

        #region SetForegroundWindow

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        #endregion

        #region MonitorFromPoint

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromPoint(TagPoint pt, uint dwFlags);

        #endregion

        #region MonitorFromWindow

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        #endregion

        #region FindWindow

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

        #endregion

        #region GetWindowRect

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out TagRect lpRect);

        #endregion

        #region GetProcessId

        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        #endregion

        #region Taskbar Display State

        [DllImport("shell32.dll",
                   EntryPoint = "SHAppBarMessage",
                   CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr SHAppBarMessage(uint dwMessage, ref AppbarData pData);

        #endregion

        #region WindowFromPoint

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(TagPoint point);

        #endregion

        #region GetAncestor

        [DllImport("user32.dll")]
        public static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

        #endregion

        #region GetClassName

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern unsafe int GetClassName(
            IntPtr hWnd,
            char*  lpClassName,
            int    nMaxCount);

        public static unsafe string GetClassName(this IntPtr hWnd)
        {
            const int maxLength = 256;

            var className = stackalloc char[maxLength];
            var count = GetClassName(hWnd, className, maxLength);
            return count == 0 ? "" : new string(className, 0, count);
        }

        #endregion

        #region SystemParametersInfo

        [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetSystemParameters(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetSystemParameters(uint uiAction, uint uiParam, out bool pvParam, uint fWinIni);

        #endregion

        #region EnumWindows

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        #endregion

        #region IsZoomed

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsZoomed(IntPtr hWnd);

        #endregion

        #region GetWindowPlacement

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowPlacement(IntPtr hWnd, ref WindowPlacement lpwndpl);

        #endregion
    }
}
