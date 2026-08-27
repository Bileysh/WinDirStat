namespace WinDirStat.Core.Interfaces;

public interface IBackgroundScanSettingsService
{
    uint ScanIntervalMinutes { get; set; }
    double LowFreeSpaceThresholdPercent { get; set; }
    string ExportToJson();
    void ImportFromJson(string json);
}