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
        var result = BackgroundScanSettingsValidator.ValidateImport(30, 10.0);
        Assert.Equal(SettingsValidationError.None, result);
    }

    [Theory]
    [InlineData(0u)]
    [InlineData(14u)]
    public void ValidateImport_RejectsIntervalBelowMinimum(uint interval)
    {
        var result = BackgroundScanSettingsValidator.ValidateImport(interval, 10.0);
        Assert.Equal(SettingsValidationError.IntervalTooSmall, result);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.9)]
    [InlineData(50.1)]
    [InlineData(100.0)]
    public void ValidateImport_RejectsThresholdOutOfRange(double threshold)
    {
        var result = BackgroundScanSettingsValidator.ValidateImport(30, threshold);
        Assert.Equal(SettingsValidationError.ThresholdOutOfRange, result);
    }

    [Fact]
    public void ValidateImport_BoundaryValuesAreAccepted()
    {
        var atMinInterval = BackgroundScanSettingsValidator.ValidateImport(BackgroundScanSettingsValidator.MinIntervalMinutes, BackgroundScanSettingsValidator.MinThresholdPercent);
        var atMaxThreshold = BackgroundScanSettingsValidator.ValidateImport(BackgroundScanSettingsValidator.MinIntervalMinutes, BackgroundScanSettingsValidator.MaxThresholdPercent);

        Assert.Equal(SettingsValidationError.None, atMinInterval);
        Assert.Equal(SettingsValidationError.None, atMaxThreshold);
    }
}