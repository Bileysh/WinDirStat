using WinDirStat.Core.Entities;
using WinDirStat.Core.Interfaces;

namespace WinDirStat.Tests.FakeService;

public class FakeScanResultFileService: IScanResultFileService
{
    public Task<string?> ExportAsync(FileSystemNode rootNode, string suggestedFileName, IntPtr ownerHwnd)
    {
        throw new NotImplementedException();
    }

    public Task<(FileSystemNode RootNode, string FileName)?> ImportAsync(IntPtr ownerHwnd)
    {
        throw new NotImplementedException();
    }

    public async Task<FileSystemNode?> ImportFromPathAsync(string filePath)
    {
        throw new NotImplementedException();
    }
}