namespace SmartTaskbar.Win11.Helpers
{
    public class TaskbarAlignmentHelper
    {
        private const string AdvancedKeyPath =
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";

        private const string TaskbarAlValueName = "TaskbarAl";

        private readonly IRegistryReader _registryReader;

        public TaskbarAlignmentHelper(IRegistryReader registryReader)
        {
            _registryReader = registryReader;
        }

        public bool IsCentered
        {
            get
            {
                var value = _registryReader.GetDwordValue(AdvancedKeyPath, TaskbarAlValueName);
                return value == null || value == 0;
            }
        }

        public bool IsLeftAligned => !IsCentered;
    }
}
