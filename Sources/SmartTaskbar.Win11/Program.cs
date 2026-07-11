namespace SmartTaskbar.Win11
{
    public static class Program
    {
        [STAThread]
        private static void Main()
        {
            using (new Mutex(true, "{a1b2c3d4-e5f6-7890-abcd-ef1234567890}", out var createNew))
            {
                if (!createNew) return;

                ApplicationConfiguration.Initialize();
                Application.Run(new SystemTray());
            }
        }
    }
}