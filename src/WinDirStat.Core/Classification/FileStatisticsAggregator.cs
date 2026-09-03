using WinDirStat.Core.Entities;

namespace WinDirStat.Core.Classification;

public static class FileStatisticsAggregator
{
    public static (List<FileTypeStatisticsEntry> ByExtension, List<FileTypeStatisticsEntry> ByCategory) ComputeAll(
        FileSystemNode root)
    {
        var files = EnumerateFiles(root).ToList();

        var byExtension = Aggregate(files, f => (
            Label: string.IsNullOrEmpty(f.Extension) ? string.Empty : f.Extension.ToLowerInvariant(),
            Category: FileCategoryClassifier.Classify(f.Extension)));

        var byCategory = Aggregate(files, f =>
        {
            var category = FileCategoryClassifier.Classify(f.Extension);
            return (Label: category.ToString(), Category: category);
        });

        return (byExtension, byCategory);
    }

    public static List<FileTypeStatisticsEntry> ByExtension(FileSystemNode root) => ComputeAll(root).ByExtension;
    public static List<FileTypeStatisticsEntry> ByCategory(FileSystemNode root) => ComputeAll(root).ByCategory;

    private static List<FileTypeStatisticsEntry> Aggregate(
        List<FileSystemNode> files, Func<FileSystemNode, (string Label, FileCategory Category)> selector)
    {
        var totalSize = files.Sum(f => (double)f.SizeLogical);
        return files
            .GroupBy(selector)
            .Select(g => new FileTypeStatisticsEntry
            {
                Label = g.Key.Label, Category = g.Key.Category,
                TotalSize = g.Sum(f => f.SizeLogical), FileCount = g.Count(),
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
        foreach (var file in EnumerateFiles(child))
            yield return file;
    }
}