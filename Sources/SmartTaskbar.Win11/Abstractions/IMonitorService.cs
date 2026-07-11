namespace SmartTaskbar.Win11.Abstractions
{
    public interface IMonitorService
    {
        IntPtr GetMonitorFromWindow(IntPtr windowHandle);

        bool IsSameMonitor(IntPtr monitor1, IntPtr monitor2);
    }
}
