using WinDirStat.Core.Entities;

namespace WinDirStat.Core.Classification;

public static class FileStatisticsAggregator
{
    public static List<FileTypeStatisticsEntry> ByExtension(FileSystemNode root) =>
        Aggregate(root, f => string.IsNullOrEmpty(f.Extension) ? "(без розширення)" : f.Extension.ToLowerInvariant());

    public static List<FileTypeStatisticsEntry> ByCategory(FileSystemNode root) =>
        Aggregate(root, f => FileCategoryClassifier.Classify(f.Extension).ToString());

    private static List<FileTypeStatisticsEntry> Aggregate(FileSystemNode root, Func<FileSystemNode, string> keySelector)
    {
        var files = EnumerateFiles(root).ToList();
        var totalSize = files.Sum(f => (double)f.SizeLogical);

        return files
            .GroupBy(keySelector)
            .Select(g => new FileTypeStatisticsEntry
            {
                Label = g.Key,
                TotalSize = g.Sum(f => f.SizeLogical),
                FileCount = g.Count(),
                PercentOfTotal = totalSize > 0 ? g.Sum(f => f.SizeLogical) / totalSize * 100 : 0
            })
            .OrderByDescending(s => s.TotalSize)
            .ToList();
    }


    private static IEnumerable<FileSystemNode> EnumerateFiles(FileSystemNode node)
    {
        if (!node.IsDirectory)
        {
            yield return node; 
            yield break;
        }

        foreach (var child in node.Children)
        {
            foreach (var file in EnumerateFiles(child))
            {
                yield return file;
            }
        }
    
    }
}