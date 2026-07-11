using SmartTaskbar.Win11.Models;

namespace SmartTaskbar.Win11.Abstractions
{
    public interface ITaskbarControlService
    {
        void HideTaskbar(in TaskbarInfo taskbar);

        void ShowTaskbar(in TaskbarInfo taskbar);

        void SetAutoHide();

        bool IsNotAutoHide();

        void CancelAutoHide();
    }
}
