using Volumetric.Core.BackgroundScan;

namespace Volumetric.Core.Interfaces;

public interface IBackgroundScanSettingsService
{
    uint ScanIntervalMinutes { get; set; }
    double LowFreeSpaceThresholdPercent { get; set; }
    bool AccountForHardLinks { get; set; }
    string ExportToJson();
    SettingsValidationError ImportFromJson(string json);
}