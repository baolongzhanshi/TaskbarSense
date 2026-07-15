using System.Runtime.InteropServices;

namespace SmartTaskbar.Win11
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TagRect
    {
        public int left;

        public int top;

        public int right;

        public int bottom;

        public static bool operator ==(TagRect left, TagRect right)
            => left.left == right.left
               && left.top == right.top
               && left.right == right.right
               && left.bottom == right.bottom;

        public static bool operator !=(TagRect left, TagRect right)
            => !(left == right);

        public bool Equals(TagRect other)
            => left == other.left
               && top == other.top
               && right == other.right
               && bottom == other.bottom;

        public override bool Equals(object? obj)
            => obj is TagRect other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = left;
                hashCode = hashCode * 397 ^ top;
                hashCode = hashCode * 397 ^ right;
                hashCode = hashCode * 397 ^ bottom;
                return hashCode;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MonitorInfo
    {
        public uint cbSize;
        public TagRect rcMonitor;
        public TagRect rcWork;
        public uint dwFlags;
    }
}