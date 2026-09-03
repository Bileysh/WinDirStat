using System.Diagnostics;
using System.IO.Enumeration;
using WinDirStat.Core.Classification;
using WinDirStat.Core.Entities;
using WinDirStat.Core.Interfaces;

namespace WinDirStat.Services;

public class DiskScanService : IDiskScanService
{
    private readonly IFileIdentityService _fileIdentityService;
    private readonly IElevatedScanHelper? _elevatedScanHelper;

    public DiskScanService(IFileIdentityService fileIdentityService, IElevatedScanHelper? elevatedScanHelper = null)
    {
        _fileIdentityService = fileIdentityService;
        _elevatedScanHelper = elevatedScanHelper;
    }

    private readonly record struct ScanEntry(
        string Name,
        string FullPath,
        bool IsDirectory,
        FileAttributes Attributes,
        long Length,
        DateTime LastWriteTimeUtc);

    private static readonly EnumerationOptions ScanOptions = new()
    {
        RecurseSubdirectories = false,
        AttributesToSkip = 0,
        IgnoreInaccessible = false
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

    private sealed class ScanProgressTracker
    {
        private const int ReportEveryNItems = 64;

        public long FilesScanned;
        public long FoldersScanned;
        private long _itemsSinceLastReport;

        public void OnItemScanned(string currentPath, IProgress<ScanProgress>? progress)
        {
            if (progress is null) return;
            if (++_itemsSinceLastReport < ReportEveryNItems) return;

            _itemsSinceLastReport = 0;
            progress.Report(new ScanProgress(currentPath, FilesScanned, FoldersScanned));
        }
    }

    public Task<ScanResult> ScanAsync(string rootPath, CancellationToken cancellationToken = default,
        bool useElevatedFallbackForAccessDenied = false, IProgress<ScanProgress>? progress = null,
        bool accountForHardLinks = false)
    {
        return Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var rootInfo = new DirectoryInfo(rootPath);
                var clusterSize = DiskSizeHelper.GetClusterSize(Path.GetPathRoot(rootPath) ?? rootPath);
                var seenHardLinks = new HashSet<FileIdentity>();

                var deniedNodes = new List<FileSystemNode>();
                var tracker = new ScanProgressTracker();
                var rootNode = ScanDirectory(rootInfo.Name, rootInfo.FullName, rootInfo.Attributes,
                    rootInfo.LastWriteTimeUtc, clusterSize, seenHardLinks, deniedNodes, tracker, progress,
                    cancellationToken, accountForHardLinks);

                if (useElevatedFallbackForAccessDenied && deniedNodes.Count > 0 && _elevatedScanHelper is not null)
                {
                    var paths = deniedNodes.Select(n => n.FullPath).ToList();
                    if (_elevatedScanHelper.TryScanElevated(paths, out var elevatedResults))
                    {
                        foreach (var deniedNode in deniedNodes)
                        {
                            if (!elevatedResults.TryGetValue(deniedNode.FullPath, out var elevatedNode)) continue;

                            deniedNode.Children.Clear();
                            deniedNode.Children.AddRange(elevatedNode.Children);
                            deniedNode.Status = elevatedNode.Status;
                            deniedNode.ErrorMessage = elevatedNode.ErrorMessage;
                        }

                        RecomputeSizes(rootNode);
                    }
                }

                stopwatch.Stop();

                var (statsByExtension, statsByCategory) = FileStatisticsAggregator.ComputeAll(rootNode);

                return new ScanResult(
                    rootPath,
                    rootNode,
                    statsByCategory,
                    statsByExtension,
                    stopwatch.Elapsed
                );
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                stopwatch.Stop();

                var errorNode = new FileSystemNode
                {
                    Name = Path.GetFileName(rootPath.TrimEnd(Path.DirectorySeparatorChar)) is { Length: > 0 } name
                        ? name
                        : rootPath,
                    FullPath = rootPath,
                    IsDirectory = true,
                    Status = ScanStatus.Error,
                    ErrorMessage = ex.Message
                };

                return new ScanResult(
                    rootPath,
                    errorNode,
                    Array.Empty<FileTypeStatisticsEntry>(),
                    Array.Empty<FileTypeStatisticsEntry>(),
                    stopwatch.Elapsed
                );
            }
        }, cancellationToken);
    }

    private FileSystemNode ScanDirectory(string name, string fullPath, FileAttributes attributes,
        DateTime lastWriteTimeUtc, uint clusterSize, HashSet<FileIdentity> seenHardLinks,
        List<FileSystemNode> deniedNodes, ScanProgressTracker tracker, IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken, bool accountForHardLinks)
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
        {
            node.Status = ScanStatus.ReparsePoint;
            return node;
        }

        try
        {
            foreach (var entry in EnumerateEntries(fullPath))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (entry.IsDirectory)
                {
                    var childNode = ScanDirectory(entry.Name, entry.FullPath, entry.Attributes,
                        entry.LastWriteTimeUtc, clusterSize, seenHardLinks, deniedNodes, tracker, progress,
                        cancellationToken, accountForHardLinks);


                    node.Children.Add(childNode);
                    node.SizeLogical += childNode.SizeLogical;
                    node.SizePhysical += childNode.SizePhysical;

                    tracker.FoldersScanned++;
                    tracker.OnItemScanned(entry.FullPath, progress);
                }
                else
                {
                    var physicalSize =
                        DiskSizeHelper.GetPhysicalSize(entry.FullPath, entry.Attributes, entry.Length, clusterSize);
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


                    if (accountForHardLinks)
                    {
                        var identity = _fileIdentityService.GetIdentity(entry.FullPath);
                        if (identity is { LinkCount: > 1 } id && !seenHardLinks.Add(id))
                        {
                            fileNode.IsDuplicateHardLink = true;
                            fileNode.SizePhysical = 0;
                        }
                    }

                    node.Children.Add(fileNode);
                    node.SizeLogical += fileNode.SizeLogical;
                    node.SizePhysical += fileNode.SizePhysical;

                    tracker.FilesScanned++;
                    tracker.OnItemScanned(entry.FullPath, progress);
                }
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            node.Status = ScanStatus.AccessDenied;
            node.ErrorMessage = ex.Message;
            deniedNodes.Add(node);
        }
        catch (IOException)
        {
            node.Status = ScanStatus.Error;
            node.ErrorMessage = "IO error while enumerating";
        }

        return node;
    }

    private static void RecomputeSizes(FileSystemNode node)
    {
        if (node.Children.Count == 0) return;

        long logical = 0, physical = 0;
        foreach (var child in node.Children)
        {
            RecomputeSizes(child);
            logical += child.SizeLogical;
            physical += child.SizePhysical;
        }

        node.SizeLogical = logical;
        node.SizePhysical = physical;
    }
}