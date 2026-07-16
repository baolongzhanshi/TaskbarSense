namespace SmartTaskbar.Win11.Worker
{
    /// <summary>
    /// Pure geometry helper for Auto-mode "large window" checks.
    /// Rule (classic SmartTaskbar): 3 * windowArea &gt; screenArea.
    /// </summary>
    public static class ScreenAreaComparer
    {
        public static bool IsLargeEnough(
            int left,
            int top,
            int right,
            int bottom,
            int screenWidth,
            int screenHeight)
        {
            if (screenWidth <= 0 || screenHeight <= 0)
                return false;

            long windowArea = (long)(right - left) * (bottom - top);
            long screenArea = (long)screenWidth * screenHeight;
            return 3 * windowArea > screenArea;
        }
    }
}