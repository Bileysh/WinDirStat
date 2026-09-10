using WinDirStat.Core.BackgroundScan;
using WinDirStat.Core.Interfaces;

namespace WinDirStat.Tests.FakeService;
 
public class FakeBackgroundScanSettingsService: IBackgroundScanSettingsService
{
    public uint ScanIntervalMinutes { get; set; }
    public double LowFreeSpaceThresholdPercent { get; set; }
    public bool AccountForHardLinks { get; set; }
    public string ExportToJson()
    {
        throw new NotImplementedException();
    }

    public SettingsValidationError ImportFromJson(string json)
    {
        throw new NotImplementedException();
    }
}