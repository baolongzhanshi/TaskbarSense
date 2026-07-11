using System.Runtime.InteropServices;

namespace SmartTaskbar.Win11.Models
{
    [StructLayout(LayoutKind.Sequential)]
    public struct WindowPlacement
    {
        public uint length;

        public uint flags;

        public uint showCmd;

        public TagPoint ptMinPosition;

        public TagPoint ptMaxPosition;

        public TagRect rcNormalPosition;
    }
}
