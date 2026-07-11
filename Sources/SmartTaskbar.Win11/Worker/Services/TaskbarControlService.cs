using SmartTaskbar.Win11.Abstractions;
using SmartTaskbar.Win11.Models;

namespace SmartTaskbar.Win11.Worker.Services
{
    using static SmartTaskbar.Win11.Fun;

    public class TaskbarControlService : ITaskbarControlService
    {
        public void HideTaskbar(in TaskbarInfo taskbar) => taskbar.HideTaskbar();

        public void ShowTaskbar(in TaskbarInfo taskbar) => taskbar.ShowTaskar();

        public void SetAutoHide() => Fun.SetAutoHide();

        public bool IsNotAutoHide() => Fun.IsNotAutoHide();

        public void CancelAutoHide() => Fun.CancelAutoHide();
    }
}
