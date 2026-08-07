using WinDirStat.Services;

namespace WinDirStat.Tests.Tests;

public class DiskScanServiceTests
{
    [Fact]
    public void Scan_ReturnsCorrectTotalSize_ForNestedDirectories()
    {
        var tempRoot = Directory.CreateTempSubdirectory();
        File.WriteAllBytes(Path.Combine(tempRoot.FullName, "a.txt"), new byte[100]);
        var subDir = tempRoot.CreateSubdirectory("sub");
        File.WriteAllBytes(Path.Combine(subDir.FullName, "b.txt"), new byte[200]);

        var result = new DiskScanService().Scan(tempRoot.FullName);

        Assert.Equal(300, result.SizeLogical);
        Assert.Equal(2, result.Children.Count);
    }
}