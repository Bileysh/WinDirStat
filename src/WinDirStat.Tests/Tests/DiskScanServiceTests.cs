using WinDirStat.Services;

namespace WinDirStat.Tests.Tests;

public class DiskScanServiceTests
{
    [Fact]
    public async Task ScanAsync_ReturnsCorrectTotalSize_ForNestedDirectories()
    {
        var tempRoot = Directory.CreateTempSubdirectory();
        await File.WriteAllBytesAsync(Path.Combine(tempRoot.FullName, "a.txt"), new byte[100]);
        var subDir = tempRoot.CreateSubdirectory("sub");
        await File.WriteAllBytesAsync(Path.Combine(subDir.FullName, "b.txt"), new byte[200]);

        var service = new DiskScanService();

        var result = await service.ScanAsync(tempRoot.FullName);

        Assert.Equal(300, result.TotalSize);
        Assert.Equal(2, result.RootNode.Children.Count);
    }

    [Fact]
    public async Task ScanAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        var tempRoot = Directory.CreateTempSubdirectory();
        await File.WriteAllBytesAsync(Path.Combine(tempRoot.FullName, "a.txt"), new byte[100]);

        var service = new DiskScanService();
        
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await service.ScanAsync(tempRoot.FullName, cts.Token);
        });
    }
}