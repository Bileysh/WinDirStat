using System;
using System.Collections.Generic;
using Volumetric.Core.Classification;

namespace Volumetric.Core.Entities;

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
    public long FileCount { get; }
    public long FolderCount { get; }

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
        (FileCount, FolderCount) = CountNodes(rootNode);
    }

    private static (long Files, long Folders) CountNodes(FileSystemNode root)
    {
        long files = 0, folders = 0;
        var pending = new Stack<FileSystemNode>();
        pending.Push(root);

        while (pending.TryPop(out var node))
        {
            if (!node.IsDirectory)
            {
                files++;
                continue;
            }

            folders++;
            foreach (var child in node.Children)
            {
                pending.Push(child);
            }
        }

        return (files, folders);
    }
}
