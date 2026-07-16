using SmartTaskbar.Win11.Abstractions;
using SmartTaskbar.Win11.Models;

namespace SmartTaskbar.Win11.Worker
{
    /// <summary>
    /// MaximizeHide decision: hide when a maximized/fullscreen window exists on the taskbar monitor.
    /// </summary>
    public static class MaximizeHidePolicy
    {
        public static void Apply(
            in TaskbarInfo taskbar,
            IMaximizePresence detector,
            ITaskbarControlService control)
        {
            if (taskbar.Handle == IntPtr.Zero)
                return;

            if (detector.HasMaximizedWindowOnMonitor(taskbar.Monitor))
                control.HideTaskbar(in taskbar);
            else
                control.ShowTaskbar(in taskbar);
        }
    }
}