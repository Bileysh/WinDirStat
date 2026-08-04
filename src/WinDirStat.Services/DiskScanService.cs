using WinDirStat.Core.Entities;
using WinDirStat.Core.Interfaces;

namespace WinDirStat.Services;

public class DiskScanService: IDiskScanService
{
    public FileSystemNode Scan(string rootPath)
    {
        var directoryInfo = new DirectoryInfo(rootPath);
        return ScanDirectory(directoryInfo);
    }

    private FileSystemNode ScanDirectory(DirectoryInfo directoryInfo)
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
                var childNode = ScanDirectory(subDirectory);
                node.Children.Add(childNode);
                node.SizeLogical += childNode.SizeLogical;
                node.SizePhysical += childNode.SizePhysical;
            }
            else if (entry is FileInfo fileInfo)
            {
                var fileNode = new FileSystemNode
                {
                    Name = fileInfo.Name,
                    FullPath = fileInfo.FullName,
                    IsDirectory = false,
                    Extension = fileInfo.Extension,
                    SizeLogical = fileInfo.Length,
                    SizePhysical = fileInfo.Length,
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