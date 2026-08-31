using WinDirStat.Core.Interfaces;

namespace WinDirStat.Tests.FakeService;

public class FakeWindowManagerService : IWindowManagerService
{
    public void OpenMainWindow()
    {
    }

    public void OpenStatisticsWindow(IMainPageViewModel viewModel)
    {
    }

    public void OpenTreeViewWindow(IMainPageViewModel viewModel)
    {
    }

    public void OpenTreeMapWindow(IMainPageViewModel viewModel)
    {
    }

    public void ReloadMainWindowContent()
    {
    }

    public void OpenSettingsWindow()
    {
    }

    public void ExitApplication()
    {
    }
}