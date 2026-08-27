namespace WinDirStat.Core.Interfaces;

public interface IWindowManagerService
{
    void OpenMainWindow();
    void OpenStatisticsWindow();
    void OpenTreeViewWindow();
    void OpenTreeMapWindow();
    void ReloadMainWindowContent();
    void OpenSettingsWindow();
    void ExitApplication();
}