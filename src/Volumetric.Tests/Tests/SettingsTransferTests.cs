using Volumetric.Core.BackgroundScan;
using Volumetric.Tests.FakeService;

namespace Volumetric.Tests.Tests;

public class SettingsTransferTests
{
    [Fact]
    public void ExportThenImport_RestoresAllTransferableSettings()
    {
        var sourceScan = new FakeBackgroundScanSettingsService
        {
            ScanIntervalMinutes = 60, LowFreeSpaceThresholdPercent = 20, AccountForHardLinks = true
        };
        var sourceSecurity = new FakeSecuritySettingsService { EncryptScanResults = true };
        var json = SettingsTransfer.Export(sourceScan, sourceSecurity);

        var targetScan = new FakeBackgroundScanSettingsService();
        var targetSecurity = new FakeSecuritySettingsService();
        var result = SettingsTransfer.Import(json, targetScan, targetSecurity);

        Assert.Equal(SettingsValidationError.None, result);
        Assert.Equal(60u, targetScan.ScanIntervalMinutes);
        Assert.Equal(20, targetScan.LowFreeSpaceThresholdPercent);
        Assert.True(targetScan.AccountForHardLinks);
        Assert.True(targetSecurity.EncryptScanResults);
    }

    [Fact]
    public void Export_DoesNotIncludeTheWindowsHelloRequirement()
    {
        var json = SettingsTransfer.Export(new FakeBackgroundScanSettingsService { ScanIntervalMinutes = 15, LowFreeSpaceThresholdPercent = 10 },
            new FakeSecuritySettingsService { RequireWindowsHelloOnLaunch = true });

        Assert.DoesNotContain("Hello", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Import_FileThatTriesToDisableWindowsHello_LeavesTheRequirementOn()
    {
        var security = new FakeSecuritySettingsService { RequireWindowsHelloOnLaunch = true };

        var result = SettingsTransfer.Import(
            """{"ScanIntervalMinutes":30,"LowFreeSpaceThresholdPercent":10,"RequireWindowsHelloOnLaunch":false}""",
            new FakeBackgroundScanSettingsService(), security);

        Assert.Equal(SettingsValidationError.None, result);
        Assert.True(security.RequireWindowsHelloOnLaunch);
    }

    [Fact]
    public void Import_OlderFileWithoutEncryptionSetting_KeepsCurrentEncryptionChoice()
    {
        var security = new FakeSecuritySettingsService { EncryptScanResults = true };

        var result = SettingsTransfer.Import(
            """{"ScanIntervalMinutes":30,"LowFreeSpaceThresholdPercent":10,"AccountForHardLinks":false}""",
            new FakeBackgroundScanSettingsService(), security);

        Assert.Equal(SettingsValidationError.None, result);
        Assert.True(security.EncryptScanResults);
    }

    [Theory]
    [InlineData("not json", SettingsValidationError.InvalidFormat)]
    [InlineData("null", SettingsValidationError.InvalidFormat)]
    [InlineData("""{"ScanIntervalMinutes":5,"LowFreeSpaceThresholdPercent":10}""", SettingsValidationError.IntervalTooSmall)]
    [InlineData("""{"ScanIntervalMinutes":30,"LowFreeSpaceThresholdPercent":90}""", SettingsValidationError.ThresholdOutOfRange)]
    public void Import_InvalidFile_ReturnsErrorAndChangesNothing(string json, SettingsValidationError expected)
    {
        var scan = new FakeBackgroundScanSettingsService { ScanIntervalMinutes = 60, LowFreeSpaceThresholdPercent = 20 };
        var security = new FakeSecuritySettingsService { EncryptScanResults = false };

        var result = SettingsTransfer.Import(json, scan, security);

        Assert.Equal(expected, result);
        Assert.Equal(60u, scan.ScanIntervalMinutes);
        Assert.Equal(20, scan.LowFreeSpaceThresholdPercent);
        Assert.False(security.EncryptScanResults);
    }
}
