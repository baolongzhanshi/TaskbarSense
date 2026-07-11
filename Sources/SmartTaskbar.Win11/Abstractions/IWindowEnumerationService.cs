namespace SmartTaskbar.Win11.Abstractions
{
    public interface IWindowEnumerationService
    {
        IReadOnlyList<IntPtr> EnumerateTopLevelWindows();
    }
}
