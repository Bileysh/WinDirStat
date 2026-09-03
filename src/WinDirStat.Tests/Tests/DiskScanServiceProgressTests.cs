using WinDirStat.Core.Entities;
using WinDirStat.Services;
using WinDirStat.Tests.FakeService;

namespace WinDirStat.Tests.Tests;

public class DiskScanServiceProgressTests
{
    private sealed class RecordingProgress : IProgress<ScanProgress>
    {
        public List<ScanProgress> Reports { get; } = [];
        public void Report(ScanProgress value) => Reports.Add(value);
    }

    [Fact]
    public async Task ScanAsync_WithoutProgress_DoesNotThrow()
    {
        var tempRoot = Directory.CreateTempSubdirectory();
        await File.WriteAllBytesAsync(Path.Combine(tempRoot.FullName, "a.txt"), new byte[10]);

        var service = new DiskScanService(new FileIdentityService());

        var result = await service.ScanAsync(tempRoot.FullName);

        Assert.Equal(1, result.RootNode.Children.Count);
    }

    [Fact]
    public async Task ScanAsync_BelowThrottleThreshold_ReportsNothing()
    {
        var tempRoot = Directory.CreateTempSubdirectory();
        for (var i = 0; i < 10; i++)
        {
            await File.WriteAllBytesAsync(Path.Combine(tempRoot.FullName, $"f{i}.txt"), new byte[1]);
        }

        var service = new DiskScanService(new FileIdentityService());
        var progress = new RecordingProgress();

        await service.ScanAsync(tempRoot.FullName, progress: progress);

        Assert.Empty(progress.Reports);
    }

    [Fact]
    public async Task ScanAsync_AboveThrottleThreshold_ReportsAtExpectedCount()
    {
        var tempRoot = Directory.CreateTempSubdirectory();
        for (var i = 0; i < 70; i++)
        {
            await File.WriteAllBytesAsync(Path.Combine(tempRoot.FullName, $"f{i}.txt"), new byte[1]);
        }

        var service = new DiskScanService(new FileIdentityService());
        var progress = new RecordingProgress();

        var result = await service.ScanAsync(tempRoot.FullName, progress: progress);

        var report = Assert.Single(progress.Reports);
        Assert.Equal(64, report.FilesScanned);
        Assert.Equal(0, report.FoldersScanned);
        Assert.Equal(70, result.RootNode.Children.Count);
    }

    [Fact]
    public async Task ScanAsync_CountsFilesAndFoldersSeparately()
    {
        var tempRoot = Directory.CreateTempSubdirectory();
        for (var i = 0; i < 40; i++)
        {
            await File.WriteAllBytesAsync(Path.Combine(tempRoot.FullName, $"f{i}.txt"), new byte[1]);
        }

        for (var i = 0; i < 30; i++)
        {
            tempRoot.CreateSubdirectory($"d{i}");
        }

        var service = new DiskScanService(new FileIdentityService());
        var progress = new RecordingProgress();

        await service.ScanAsync(tempRoot.FullName, progress: progress);
        var report = Assert.Single(progress.Reports);
        Assert.Equal(64, report.FilesScanned + report.FoldersScanned);
    }
}