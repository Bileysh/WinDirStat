using Volumetric.ViewModels;

namespace Volumetric.Tests.Tests;

public class TrayStatusUpdateThrottleTests
{
    private const long IntervalMs = 500;

    [Fact]
    public void ShouldUpdate_IsScanningChange_AlwaysTrueEvenRightAfterAnUpdate()
    {
        var throttle = new TrayStatusUpdateThrottle(IntervalMs);
        throttle.MarkUpdated(1000);

        Assert.True(throttle.ShouldUpdate(nameof(MainPageViewModel.IsScanning), 1001));
    }

    [Theory]
    [InlineData(nameof(MainPageViewModel.ScanFilesCount))]
    [InlineData(nameof(MainPageViewModel.ScanFoldersCount))]
    public void ShouldUpdate_CountChangeBeforeAnyUpdate_IsTrue(string propertyName)
    {
        var throttle = new TrayStatusUpdateThrottle(IntervalMs);

        Assert.True(throttle.ShouldUpdate(propertyName, 0));
    }

    [Theory]
    [InlineData(nameof(MainPageViewModel.ScanFilesCount))]
    [InlineData(nameof(MainPageViewModel.ScanFoldersCount))]
    public void ShouldUpdate_CountChangeWithinInterval_IsFalse(string propertyName)
    {
        var throttle = new TrayStatusUpdateThrottle(IntervalMs);
        throttle.MarkUpdated(1000);

        Assert.False(throttle.ShouldUpdate(propertyName, 1000 + IntervalMs - 1));
    }

    [Fact]
    public void ShouldUpdate_CountChangeAfterInterval_IsTrue()
    {
        var throttle = new TrayStatusUpdateThrottle(IntervalMs);
        throttle.MarkUpdated(1000);

        Assert.True(throttle.ShouldUpdate(nameof(MainPageViewModel.ScanFilesCount), 1000 + IntervalMs));
    }

    [Theory]
    [InlineData(nameof(MainPageViewModel.ScanCurrentPath))]
    [InlineData(nameof(MainPageViewModel.SearchText))]
    [InlineData(null)]
    public void ShouldUpdate_UnrelatedProperty_IsFalse(string? propertyName)
    {
        var throttle = new TrayStatusUpdateThrottle(IntervalMs);

        Assert.False(throttle.ShouldUpdate(propertyName, 0));
    }

    [Fact]
    public void ShouldUpdate_ThousandsOfProgressTicksOverTwoSeconds_UpdatesAtMostOncePerInterval()
    {
        var throttle = new TrayStatusUpdateThrottle(IntervalMs);
        var updates = 0;

        for (long now = 0; now < 2000; now++)
        {
            foreach (var property in new[]
                     {
                         nameof(MainPageViewModel.ScanFilesCount), nameof(MainPageViewModel.ScanFoldersCount)
                     })
            {
                if (throttle.ShouldUpdate(property, now))
                {
                    throttle.MarkUpdated(now);
                    updates++;
                }
            }
        }

        Assert.Equal(4, updates);
    }
}
