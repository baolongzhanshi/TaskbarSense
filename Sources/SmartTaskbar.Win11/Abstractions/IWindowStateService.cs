namespace SmartTaskbar.Win11.Abstractions
{
    public interface IWindowStateService
    {
        bool IsMaximized(IntPtr handle);

        bool IsVisible(IntPtr handle);

        string GetClassName(IntPtr handle);
    }
}
