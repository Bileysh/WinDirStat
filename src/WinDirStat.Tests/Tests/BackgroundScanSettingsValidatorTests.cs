using WinDirStat.Core.BackgroundScan;

namespace WinDirStat.Tests.Tests;

public class BackgroundScanSettingsValidatorTests
{
    [Theory]
    [InlineData(0u, 15u)]
    [InlineData(1u, 15u)]
    [InlineData(14u, 15u)]
    [InlineData(15u, 15u)]
    [InlineData(60u, 60u)]
    public void ClampInterval_NeverGoesBelowMinimum(uint input, uint expected)
    {
        Assert.Equal(expected, BackgroundScanSettingsValidator.ClampInterval(input));
    }

    [Theory]
    [InlineData(0.0, 1.0)]
    [InlineData(0.5, 1.0)]
    [InlineData(1.0, 1.0)]
    [InlineData(25.0, 25.0)]
    [InlineData(50.0, 50.0)]
    [InlineData(75.0, 50.0)]
    public void ClampThreshold_StaysWithinBounds(double input, double expected)
    {
        Assert.Equal(expected, BackgroundScanSettingsValidator.ClampThreshold(input));
    }

    [Fact]
    public void ValidateImport_AcceptsValidValues()
    {
        var exception = Record.Exception(() => BackgroundScanSettingsValidator.ValidateImport(30, 10.0));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(14u)]
    public void ValidateImport_RejectsIntervalBelowMinimum(uint interval)
    {
        Assert.Throws<FormatException>(() => BackgroundScanSettingsValidator.ValidateImport(interval, 10.0));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.9)]
    [InlineData(50.1)]
    [InlineData(100.0)]
    public void ValidateImport_RejectsThresholdOutOfRange(double threshold)
    {
        Assert.Throws<FormatException>(() => BackgroundScanSettingsValidator.ValidateImport(30, threshold));
    }

    [Fact]
    public void ValidateImport_BoundaryValuesAreAccepted()
    {
        var atMinInterval = Record.Exception(() =>
            BackgroundScanSettingsValidator.ValidateImport(BackgroundScanSettingsValidator.MinIntervalMinutes, BackgroundScanSettingsValidator.MinThresholdPercent));
        var atMaxThreshold = Record.Exception(() =>
            BackgroundScanSettingsValidator.ValidateImport(BackgroundScanSettingsValidator.MinIntervalMinutes, BackgroundScanSettingsValidator.MaxThresholdPercent));

        Assert.Null(atMinInterval);
        Assert.Null(atMaxThreshold);
    }
}
