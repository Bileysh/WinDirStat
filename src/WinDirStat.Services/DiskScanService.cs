using System.Diagnostics;
using WinDirStat.Core.Classification;
using WinDirStat.Core.Entities;
using WinDirStat.Core.Interfaces;

namespace WinDirStat.Services;

public class DiskScanService : IDiskScanService
{
    public ScanResult Scan(string rootPath)
    {
        var stopwatch = Stopwatch.StartNew();
        
        PrivilegeHelper.EnableBackupPrivilege();
        var directoryInfo = new DirectoryInfo(rootPath);
        var clusterSize = DiskSizeHelper.GetClusterSize(Path.GetPathRoot(rootPath) ?? rootPath);
        
        var rootNode = ScanDirectory(directoryInfo, clusterSize); 
        
        var byCategory = FileStatisticsAggregator.ByCategory(rootNode);
        var byExtension = FileStatisticsAggregator.ByExtension(rootNode);
        
        stopwatch.Stop();
        
        return new ScanResult(
            rootPath,
            rootNode,
            byCategory,
            byExtension,
            stopwatch.Elapsed
        );
    }

    private FileSystemNode ScanDirectory(DirectoryInfo directoryInfo, uint clusterSize)
    {
        var node = new FileSystemNode
        {
            Name = directoryInfo.Name,
            FullPath = directoryInfo.FullName,
            IsDirectory = true,
            LastModified = directoryInfo.LastWriteTimeUtc
        };
        if (directoryInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
            return node;

        IEnumerable<FileSystemInfo> entries;
        try
        {
            entries = directoryInfo.EnumerateFileSystemInfos();
        }
        catch (UnauthorizedAccessException)
        {
            return node; //TODO: Обійти обмеження доступу за допомогою SeBackupPrivilege
        }
        catch (IOException)
        {
            return node;
        }

        foreach (var entry in entries)
        {
            if (entry is DirectoryInfo subDirectory)
            {
                var childNode = ScanDirectory(subDirectory, clusterSize); 
                node.Children.Add(childNode);
                node.SizeLogical += childNode.SizeLogical;
                node.SizePhysical += childNode.SizePhysical;
            }
            else if (entry is FileInfo fileInfo)
            {
                var physicalSize = DiskSizeHelper.GetPhysicalSize(fileInfo.FullName, clusterSize);
                var fileNode = new FileSystemNode
                {
                    Name = fileInfo.Name,
                    FullPath = fileInfo.FullName,
                    IsDirectory = false,
                    Extension = fileInfo.Extension,
                    SizeLogical = fileInfo.Length,
                    SizePhysical = physicalSize >= 0 ? physicalSize : fileInfo.Length,
                    LastModified = fileInfo.LastWriteTimeUtc
                };
                node.Children.Add(fileNode);
                node.SizeLogical += fileNode.SizeLogical;
                node.SizePhysical += fileNode.SizePhysical;
            }
        }

        return node;
    }
}