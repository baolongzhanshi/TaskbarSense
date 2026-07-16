namespace SmartTaskbar.Win11.Abstractions
{
    /// <summary>
    /// Presence of a maximized / fullscreen window on a monitor (test seam for MaximizeHide).
    /// </summary>
    public interface IMaximizePresence
    {
        bool HasMaximizedWindowOnMonitor(IntPtr targetMonitor);
    }
}