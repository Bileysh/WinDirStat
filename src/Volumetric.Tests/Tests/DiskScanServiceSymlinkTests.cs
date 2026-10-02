using Volumetric.Core.Entities;
using Volumetric.Services;

namespace Volumetric.Tests.Tests;

public class DiskScanServiceSymlinkTests : IDisposable
{
    private const int TargetFileSize = 5000;

    private readonly DirectoryInfo _outside = Directory.CreateTempSubdirectory("volumetric-symlink-target-");
    private readonly DirectoryInfo _scanRoot = Directory.CreateTempSubdirectory("volumetric-symlink-root-");
    private readonly DiskScanService _service = new(new FileIdentityService());

    public DiskScanServiceSymlinkTests()
    {
        File.WriteAllBytes(Path.Combine(_outside.FullName, "big.bin"), new byte[TargetFileSize]);
        File.WriteAllBytes(Path.Combine(_scanRoot.FullName, "own.bin"), new byte[100]);
    }

    [SymlinkFact]
    public async Task ScanAsync_DirectorySymlink_MarkedAsReparsePointAndNotTraversed()
    {
        Directory.CreateSymbolicLink(Path.Combine(_scanRoot.FullName, "dir-link"), _outside.FullName);

        var result = await _service.ScanAsync(_scanRoot.FullName);

        var link = result.RootNode.Children.Single(c => c.Name == "dir-link");
        Assert.Equal(ScanStatus.ReparsePoint, link.Status);
        Assert.Empty(link.Children);
        Assert.Equal(100, result.TotalSize);
    }

    [SymlinkFact]
    public async Task ScanAsync_FileSymlinkToFileOutsideTree_DoesNotCountTargetSize()
    {
        File.CreateSymbolicLink(Path.Combine(_scanRoot.FullName, "file-link.bin"),
            Path.Combine(_outside.FullName, "big.bin"));

        var result = await _service.ScanAsync(_scanRoot.FullName);

        var link = result.RootNode.Children.Single(c => c.Name == "file-link.bin");
        Assert.False(link.IsDirectory);
        Assert.Equal(0, link.SizeLogical);
        Assert.Equal(0, link.SizePhysical);
        Assert.Equal(100, result.TotalSize);
    }

    [SymlinkFact]
    public async Task ScanAsync_FileSymlinkToFileInsideTree_CountsTheDataOnce()
    {
        File.CreateSymbolicLink(Path.Combine(_scanRoot.FullName, "own-link.bin"),
            Path.Combine(_scanRoot.FullName, "own.bin"));

        var result = await _service.ScanAsync(_scanRoot.FullName);

        Assert.Equal(2, result.RootNode.Children.Count);
        Assert.Equal(100, result.TotalSize);
    }

    [SymlinkFact]
    public async Task ScanAsync_SymlinkLoopBackToRoot_FinishesWithoutRecursingForever()
    {
        var nested = _scanRoot.CreateSubdirectory("nested");
        Directory.CreateSymbolicLink(Path.Combine(nested.FullName, "back-to-root"), _scanRoot.FullName);

        var scan = _service.ScanAsync(_scanRoot.FullName);
        var finished = await Task.WhenAny(scan, Task.Delay(TimeSpan.FromSeconds(10)));

        Assert.Same(scan, finished);
        var loop = (await scan).RootNode.Children.Single(c => c.Name == "nested").Children.Single();
        Assert.Equal(ScanStatus.ReparsePoint, loop.Status);
    }

    [SymlinkFact]
    public async Task ScanAsync_BrokenSymlinks_AreListedWithoutFailingTheScan()
    {
        var missing = Path.Combine(_outside.FullName, "does-not-exist");
        File.CreateSymbolicLink(Path.Combine(_scanRoot.FullName, "broken-file-link"), missing + ".bin");
        Directory.CreateSymbolicLink(Path.Combine(_scanRoot.FullName, "broken-dir-link"), missing);

        var result = await _service.ScanAsync(_scanRoot.FullName);

        Assert.Equal(ScanStatus.Ok, result.RootNode.Status);
        Assert.Equal(3, result.RootNode.Children.Count);
        Assert.Equal(100, result.TotalSize);
    }

    public void Dispose()
    {
        _scanRoot.Delete(recursive: true);
        _outside.Delete(recursive: true);
    }
}
