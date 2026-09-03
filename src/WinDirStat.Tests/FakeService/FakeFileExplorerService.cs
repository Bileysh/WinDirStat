using WinDirStat.Core.Interfaces;

namespace WinDirStat.Tests.FakeService;

public class FakeFileExplorerService : IFileExplorerService
{
    public void OpenInExplorer(string fullPath, bool isDirectory)
    {
    }

    public void ShowProperties(string fullPath)
    {
    }
}
