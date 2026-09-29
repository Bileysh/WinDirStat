using Volumetric.Core.Entities;

namespace Volumetric.Core.Interfaces;

public interface IWindowManagerService
{
    void OpenMainWindow(string? initialScanPath = null);
    void OpenMainWindowWithImportedResult(FileSystemNode rootNode);
    void OpenStatisticsWindow(IMainPageViewModel viewModel);
    void OpenTreeViewWindow(IMainPageViewModel viewModel);
    void OpenTreeMapWindow(IMainPageViewModel viewModel);
    void OpenScanReportWindow(FileSystemNode rootNode);
    void ReloadMainWindowContent();
    void OpenSettingsWindow();
    void ExitApplication();
}
