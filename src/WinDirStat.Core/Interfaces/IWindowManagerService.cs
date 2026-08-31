namespace WinDirStat.Core.Interfaces;

public interface IWindowManagerService
{
    void OpenMainWindow();
    void OpenStatisticsWindow(IMainPageViewModel viewModel);
    void OpenTreeViewWindow(IMainPageViewModel viewModel);
    void OpenTreeMapWindow(IMainPageViewModel viewModel);
    void ReloadMainWindowContent();
    void OpenSettingsWindow();
    void ExitApplication();
}