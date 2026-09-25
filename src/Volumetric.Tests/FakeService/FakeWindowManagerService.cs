using Volumetric.Core.Entities;
using Volumetric.Core.Interfaces;

namespace Volumetric.Tests.FakeService;

public class FakeWindowManagerService : IWindowManagerService
{
    public FileSystemNode? LastReportedRootNode { get; private set; }

    public void OpenMainWindow(string? initialScanPath = null)
    {
    }

    public void OpenMainWindowWithImportedResult(FileSystemNode rootNode)
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

    public void OpenScanReportWindow(FileSystemNode rootNode)
    {
        LastReportedRootNode = rootNode;
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
