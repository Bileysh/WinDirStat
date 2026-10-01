using Volumetric.Core.Entities;
using Volumetric.Core.Interfaces;

namespace Volumetric.Tests.FakeService;

public class FakeScanResultFileService : IScanResultFileService
{
    public Task<string?> ExportAsync(FileSystemNode rootNode, string suggestedFileName, IntPtr ownerHwnd)
    {
        throw new NotImplementedException();
    }

    public Exception? ImportException { get; set; }

    public Task<(FileSystemNode RootNode, string FileName)?> ImportAsync(IntPtr ownerHwnd)
    {
        if (ImportException is not null)
        {
            throw ImportException;
        }

        return Task.FromResult<(FileSystemNode RootNode, string FileName)?>(null);
    }

    public async Task<FileSystemNode?> ImportFromPathAsync(string filePath)
    {
        throw new NotImplementedException();
    }
}
