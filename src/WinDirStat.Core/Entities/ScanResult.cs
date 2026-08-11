using System;
using System.Collections.Generic;
using WinDirStat.Core.Classification;

namespace WinDirStat.Core.Entities;

public class ScanResult
{
    public Guid Id { get; } = Guid.NewGuid();

    public string RootPath { get; }
    public FileSystemNode RootNode { get; }

    public IReadOnlyList<FileTypeStatisticsEntry> StatisticsByCategory { get; }
    public IReadOnlyList<FileTypeStatisticsEntry> StatisticsByExtension { get; }

    public TimeSpan ScanDuration { get; }
    public DateTime ScannedAt { get; }
    public long TotalSize => RootNode.SizeLogical;

    public ScanResult(
        string rootPath,
        FileSystemNode rootNode,
        IReadOnlyList<FileTypeStatisticsEntry> statisticsByCategory,
        IReadOnlyList<FileTypeStatisticsEntry> statisticsByExtension,
        TimeSpan scanDuration)
    {
        RootPath = rootPath;
        RootNode = rootNode;
        StatisticsByCategory = statisticsByCategory;
        StatisticsByExtension = statisticsByExtension;
        ScanDuration = scanDuration;
        ScannedAt = DateTime.Now;
    }
}