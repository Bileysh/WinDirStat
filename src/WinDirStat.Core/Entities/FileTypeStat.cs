namespace WinDirStat.Core.Entities;

public class FileTypeStat
{
    public required string Label { get; init; }
    public long TotalSize { get; init; }
    public int FileCount { get; init; }
    public double PercentOfTotal { get; init; }
}