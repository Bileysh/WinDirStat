using Volumetric.Core.Interfaces;

namespace Volumetric.Tests.FakeService;

public class FakeFileExplorerService : IFileExplorerService
{
    public void OpenInExplorer(string fullPath, bool isDirectory)
    {
    }

    public void ShowProperties(string fullPath)
    {
    }
}
