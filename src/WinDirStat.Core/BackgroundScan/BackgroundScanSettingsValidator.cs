namespace WinDirStat.Core.BackgroundScan;

public static class BackgroundScanSettingsValidator
{
    public const uint MinIntervalMinutes = 15;

    public const double MinThresholdPercent = 1.0;
    public const double MaxThresholdPercent = 50.0;

    public static uint ClampInterval(uint value) => Math.Max(value, MinIntervalMinutes);

    public static double ClampThreshold(double value) =>
        Math.Clamp(value, MinThresholdPercent, MaxThresholdPercent);
    
    public static void ValidateImport(uint scanIntervalMinutes, double lowFreeSpaceThresholdPercent)
    {
        if (scanIntervalMinutes < MinIntervalMinutes)
        {
            throw new FormatException(
                $"ScanIntervalMinutes не може бути менше {MinIntervalMinutes} (обмеження Windows TimeTrigger).");
        }

        if (lowFreeSpaceThresholdPercent is < MinThresholdPercent or > MaxThresholdPercent)
        {
            throw new FormatException(
                $"LowFreeSpaceThresholdPercent має бути в межах {MinThresholdPercent:F0}–{MaxThresholdPercent:F0}.");
        }
    }
}
