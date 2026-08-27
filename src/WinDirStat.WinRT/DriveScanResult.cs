namespace WinDirStat.WinRT;

public sealed class DriveScanResult
{
    public DriveScanResult(string driveName, long totalBytes, long freeBytes)
    {
        DriveName = driveName;
        TotalBytes = totalBytes;
        FreeBytes = freeBytes;
    }

    public string DriveName { get; }

    public long TotalBytes { get; }

    public long FreeBytes { get; }

    public double UsedPercent => TotalBytes == 0
        ? 0.0
        : (TotalBytes - FreeBytes) / (double)TotalBytes * 100.0;
}