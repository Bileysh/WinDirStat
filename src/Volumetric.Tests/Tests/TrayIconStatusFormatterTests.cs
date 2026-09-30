using Volumetric.ViewModels;

namespace Volumetric.Tests.Tests;

public class TrayIconStatusFormatterTests
{
    private const string IdleTooltip = "Volumetric";
    private const string ScanningFormat = "Volumetric — Scanning… Files: {0:N0}  Folders: {1:N0}";

    [Fact]
    public void BuildTooltip_NotScanning_ReturnsIdleTooltip()
    {
        var tooltip = TrayIconStatusFormatter.BuildTooltip(false, 123, 45, IdleTooltip, ScanningFormat);

        Assert.Equal(IdleTooltip, tooltip);
    }

    [Fact]
    public void BuildTooltip_NotScanning_IgnoresStaleCounts()
    {
        var tooltip = TrayIconStatusFormatter.BuildTooltip(false, 999_999, 999_999, IdleTooltip, ScanningFormat);

        Assert.Equal(IdleTooltip, tooltip);
    }

    [Fact]
    public void BuildTooltip_Scanning_FormatsFileAndFolderCounts()
    {
        var tooltip = TrayIconStatusFormatter.BuildTooltip(true, 1234, 56, IdleTooltip, ScanningFormat);

        Assert.Equal(string.Format(ScanningFormat, 1234, 56), tooltip);
    }

    [Fact]
    public void BuildTooltip_ScanningWithZeroCounts_StillReportsScanningState()
    {
        var tooltip = TrayIconStatusFormatter.BuildTooltip(true, 0, 0, IdleTooltip, ScanningFormat);

        Assert.Equal(string.Format(ScanningFormat, 0, 0), tooltip);
        Assert.NotEqual(IdleTooltip, tooltip);
    }
}
