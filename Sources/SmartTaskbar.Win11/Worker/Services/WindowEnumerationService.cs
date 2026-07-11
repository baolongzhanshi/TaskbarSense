using SmartTaskbar.Win11.Abstractions;

namespace SmartTaskbar.Win11.Worker.Services
{
    using static SmartTaskbar.Win11.Fun;

    public class WindowEnumerationService : IWindowEnumerationService
    {
        public IReadOnlyList<IntPtr> EnumerateTopLevelWindows()
        {
            var handles = new List<IntPtr>(64);

            EnumWindowsProc callback = (hwnd, _) =>
            {
                handles.Add(hwnd);
                return true;
            };

            EnumWindows(callback, IntPtr.Zero);

            GC.KeepAlive(callback);

            return handles;
        }
    }
}
