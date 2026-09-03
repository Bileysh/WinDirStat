using WinDirStat.Core.BackgroundScan;

namespace WinDirStat.Core.Interfaces;

public interface IBackgroundScanSettingsService
{
    uint ScanIntervalMinutes { get; set; }
    double LowFreeSpaceThresholdPercent { get; set; }
    bool AccountForHardLinks { get; set; }
    string ExportToJson();
    SettingsValidationError ImportFromJson(string json);
}