using System.Diagnostics;
using System.IO.Enumeration;
using WinDirStat.Core.Classification;
using WinDirStat.Core.Entities;
using WinDirStat.Core.Interfaces;

namespace WinDirStat.Services;

public class DiskScanService : IDiskScanService
{
    private readonly record struct ScanEntry(
        string Name, string FullPath, bool IsDirectory,
        FileAttributes Attributes, long Length, DateTime LastWriteTimeUtc);

    private static readonly EnumerationOptions ScanOptions = new()
    {
        RecurseSubdirectories = false,
        AttributesToSkip = 0,
        IgnoreInaccessible = true
    };

    private static FileSystemEnumerable<ScanEntry> EnumerateEntries(string path) =>
        new(path, (ref FileSystemEntry entry) => new ScanEntry(
            entry.FileName.ToString(),
            entry.ToFullPath(),
            entry.IsDirectory,
            entry.Attributes,
            entry.Length,
            entry.LastWriteTimeUtc.UtcDateTime),
            ScanOptions);

    public Task<ScanResult> ScanAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();

            var rootInfo = new DirectoryInfo(rootPath);
            var clusterSize = DiskSizeHelper.GetClusterSize(Path.GetPathRoot(rootPath) ?? rootPath);

            var rootNode = ScanDirectory(rootInfo.Name, rootInfo.FullName, rootInfo.Attributes,
                rootInfo.LastWriteTimeUtc, clusterSize, cancellationToken);

            stopwatch.Stop();

            var (statsByExtension, statsByCategory) = FileStatisticsAggregator.ComputeAll(rootNode);

            return new ScanResult(
                rootPath,
                rootNode,
                statsByCategory,
                statsByExtension,
                stopwatch.Elapsed
            );
        }, cancellationToken);
    }

    private FileSystemNode ScanDirectory(string name, string fullPath, FileAttributes attributes,
        DateTime lastWriteTimeUtc, uint clusterSize, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var node = new FileSystemNode
        {
            Name = name,
            FullPath = fullPath,
            IsDirectory = true,
            LastModified = lastWriteTimeUtc
        };

        if (attributes.HasFlag(FileAttributes.ReparsePoint))
            return node;

        IEnumerable<ScanEntry> entries;
        try { entries = EnumerateEntries(fullPath); }
        catch (IOException) { return node; }

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry.IsDirectory)
            {
                var childNode = ScanDirectory(entry.Name, entry.FullPath, entry.Attributes,
                    entry.LastWriteTimeUtc, clusterSize, cancellationToken);
                node.Children.Add(childNode);
                node.SizeLogical += childNode.SizeLogical;
                node.SizePhysical += childNode.SizePhysical;
            }
            else
            {
                var physicalSize = DiskSizeHelper.GetPhysicalSize(entry.FullPath, entry.Attributes, entry.Length, clusterSize);

                var fileNode = new FileSystemNode
                {
                    Name = entry.Name,
                    FullPath = entry.FullPath,
                    IsDirectory = false,
                    Extension = Path.GetExtension(entry.Name),
                    SizeLogical = entry.Length,
                    SizePhysical = physicalSize >= 0 ? physicalSize : entry.Length,
                    LastModified = entry.LastWriteTimeUtc
                };
                node.Children.Add(fileNode);
                node.SizeLogical += fileNode.SizeLogical;
                node.SizePhysical += fileNode.SizePhysical;
            }
        }

        return node;
    }
}