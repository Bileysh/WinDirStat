using WinDirStat.Core.Classification;
using WinDirStat.Core.Entities;

namespace WinDirStat.ViewModels;

public class DriveItemViewModel
{
    public string RootPath { get; }
    public string DisplayName { get; }
    public string TotalFormatted { get; }
    public string FreeFormatted { get; }
    public string CapacitySummary { get; }

    public double UsedPercent { get; }

    public DriveItemViewModel(DriveItem drive)
    {
        RootPath = drive.RootPath;
        DisplayName = string.IsNullOrEmpty(drive.VolumeLabel)
            ? RootPath
            : $"{RootPath} ({drive.VolumeLabel})";

        TotalFormatted = SizeFormatter.Format(drive.TotalBytes);
        FreeFormatted = SizeFormatter.Format(drive.FreeBytes);
        CapacitySummary = $"{FreeFormatted} / {TotalFormatted}";

        var usedBytes = Math.Max(0, drive.TotalBytes - drive.FreeBytes);
        UsedPercent = drive.TotalBytes > 0
            ? Math.Clamp(usedBytes * 100.0 / drive.TotalBytes, 0, 100)
            : 0;
    }
}