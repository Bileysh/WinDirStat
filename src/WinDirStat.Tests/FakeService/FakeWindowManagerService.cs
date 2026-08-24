using WinDirStat.Core.Interfaces;

namespace WinDirStat.Tests.FakeService;

public class FakeWindowManagerService : IWindowManagerService
{
    public void OpenMainWindow() { }
    public void OpenStatisticsWindow() { }
    
    public void OpenTreeViewWindow() { }
    public void OpenTreeMapWindow() { }
    public void ReloadMainWindowContent() { }
}