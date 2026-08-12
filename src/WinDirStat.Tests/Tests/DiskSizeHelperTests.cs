using WinDirStat.Services;

namespace WinDirStat.Tests.Tests;

public class DiskSizeHelperTests
{
    [Theory]
    [InlineData(0, 4096, 0)]
    [InlineData(1, 4096, 4096)]
    [InlineData(4096, 4096, 4096)]
    [InlineData(4097, 4096, 8192)]
    [InlineData(8192, 4096, 8192)]
    public void RoundUpToCluster_RoundsCorrectly(long rawSize, uint clusterSize, long expected)
    {
        Assert.Equal(expected, DiskSizeHelper.RoundUpToCluster(rawSize, clusterSize));
    }

    [Fact]
    public void RoundUpToCluster_ZeroClusterSize_ReturnsRawSizeUnchanged()
    {
        Assert.Equal(12345, DiskSizeHelper.RoundUpToCluster(12345, 0));
    }
}