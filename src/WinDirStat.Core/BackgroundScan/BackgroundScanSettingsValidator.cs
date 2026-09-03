namespace WinDirStat.Core.BackgroundScan;

public enum SettingsValidationError
{
    None,
    IntervalTooSmall,
    ThresholdOutOfRange,
    InvalidFormat
}

public static class BackgroundScanSettingsValidator
{
    public const uint MinIntervalMinutes = 15;
    public const double MinThresholdPercent = 1.0;
    public const double MaxThresholdPercent = 50.0;

    public static uint ClampInterval(uint value) => Math.Max(value, MinIntervalMinutes);

    public static double ClampThreshold(double value) =>
        Math.Clamp(value, MinThresholdPercent, MaxThresholdPercent);
    
    public static SettingsValidationError ValidateImport(uint scanIntervalMinutes, double lowFreeSpaceThresholdPercent)
    {
        if (scanIntervalMinutes < MinIntervalMinutes) return SettingsValidationError.IntervalTooSmall;
        if (lowFreeSpaceThresholdPercent is < MinThresholdPercent or > MaxThresholdPercent) return SettingsValidationError.ThresholdOutOfRange;
        
        return SettingsValidationError.None;
    }
}