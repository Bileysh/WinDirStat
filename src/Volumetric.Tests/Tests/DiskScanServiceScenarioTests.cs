using System.Diagnostics;
using Volumetric.Core.Entities;
using Volumetric.Services;

namespace Volumetric.Tests.Tests;

public class DiskScanServiceScenarioTests
{
    [Fact]
    public async Task ScanAsync_UnreachableNetworkPath_ReturnsGracefulErrorInsteadOfThrowing()
    {
        var service = new DiskScanService(new FileIdentityService());

        var result = await service.ScanAsync(@"\\volumetric-unreachable-host\share");

        Assert.NotEqual(ScanStatus.Ok, result.RootNode.Status);
        Assert.Equal(0, result.TotalSize);
    }

    [Fact]
    public async Task ScanAsync_DeeplyNestedDirectories_AggregatesSizeCorrectly()
    {
        var tempRoot = Directory.CreateTempSubdirectory();
        var deepest = tempRoot.FullName;
        const int depth = 300;

        for (var i = 0; i < depth; i++)
        {
            deepest = Path.Combine(deepest, $"d{i}");
        }

        Directory.CreateDirectory(deepest);
        await File.WriteAllBytesAsync(Path.Combine(deepest, "leaf.txt"), new byte[500]);

        var service = new DiskScanService(new FileIdentityService());

        var result = await service.ScanAsync(tempRoot.FullName);

        Assert.Equal(500, result.TotalSize);
        Assert.Equal(ScanStatus.Ok, result.RootNode.Status);

        var node = result.RootNode;
        for (var i = 0; i < depth; i++)
        {
            node = Assert.Single(node.Children);
            Assert.Equal($"d{i}", node.Name);
        }

        var leaf = Assert.Single(node.Children);
        Assert.Equal("leaf.txt", leaf.Name);
        Assert.Equal(500, leaf.SizeLogical);
    }

    [Fact]
    public async Task ScanAsync_PathLongerThanMaxPath_StillScansSuccessfully()
    {
        var tempRoot = Directory.CreateTempSubdirectory();
        var deepPath = tempRoot.FullName;
        var segment = new string('a', 50);

        while (deepPath.Length < 400)
        {
            deepPath = Path.Combine(deepPath, segment);
        }

        Directory.CreateDirectory(deepPath);
        await File.WriteAllBytesAsync(Path.Combine(deepPath, "long-path-file.txt"), new byte[42]);

        Assert.True(tempRoot.FullName.Length < 260);
        Assert.True(deepPath.Length > 260);

        var service = new DiskScanService(new FileIdentityService());

        var result = await service.ScanAsync(tempRoot.FullName);

        Assert.Equal(ScanStatus.Ok, result.RootNode.Status);
        Assert.Equal(42, result.TotalSize);
    }

    [Fact]
    public async Task ScanAsync_DirectoryJunction_MarkedAsReparsePointAndNotTraversed()
    {
        var tempRoot = Directory.CreateTempSubdirectory();
        var realTarget = tempRoot.CreateSubdirectory("real-target");
        await File.WriteAllBytesAsync(Path.Combine(realTarget.FullName, "inside-target.txt"), new byte[1000]);

        var junctionPath = Path.Combine(tempRoot.FullName, "junction-to-target");
        CreateJunction(junctionPath, realTarget.FullName);

        var service = new DiskScanService(new FileIdentityService());

        var result = await service.ScanAsync(tempRoot.FullName);

        var junctionNode = result.RootNode.Children.Single(c => c.Name == "junction-to-target");
        Assert.Equal(ScanStatus.ReparsePoint, junctionNode.Status);
        Assert.Empty(junctionNode.Children);
        Assert.Equal(0, junctionNode.SizeLogical);
        Assert.Equal(1000, result.TotalSize);
    }

    [Fact]
    public async Task ScanAsync_CancelledPartwayThroughALargeTree_ThrowsOperationCanceledException()
    {
        var tempRoot = Directory.CreateTempSubdirectory();
        for (var i = 0; i < 20; i++)
        {
            var dir = tempRoot.CreateSubdirectory($"d{i}");
            for (var j = 0; j < 50; j++)
            {
                await File.WriteAllBytesAsync(Path.Combine(dir.FullName, $"f{j}.txt"), new byte[10]);
            }
        }

        var service = new DiskScanService(new FileIdentityService());
        using var cts = new CancellationTokenSource();
        var sawProgress = new TaskCompletionSource();
        var progress = new CancelOnFirstReport(() =>
        {
            sawProgress.TrySetResult();
            cts.Cancel();
        });

        var scanTask = service.ScanAsync(tempRoot.FullName, cts.Token, progress: progress);

        await sawProgress.Task;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await scanTask);
    }

    private sealed class CancelOnFirstReport(Action onFirstReport) : IProgress<ScanProgress>
    {
        private int _reported;

        public void Report(ScanProgress value)
        {
            if (Interlocked.Exchange(ref _reported, 1) == 0)
            {
                onFirstReport();
            }
        }
    }

    private static void CreateJunction(string linkPath, string targetPath)
    {
        var psi = new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{linkPath}\" \"{targetPath}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            throw new InvalidOperationException($"mklink /J failed with exit code {process.ExitCode}: {error}");
        }
    }
}
