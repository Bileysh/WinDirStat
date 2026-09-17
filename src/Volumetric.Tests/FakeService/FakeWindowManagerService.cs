using Volumetric.Core.Entities;
using Volumetric.Core.Interfaces;

namespace Volumetric.Tests.FakeService;

public class FakeWindowManagerService : IWindowManagerService
{
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