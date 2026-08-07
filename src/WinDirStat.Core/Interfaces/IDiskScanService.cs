using WinDirStat.Core.Entities;

namespace WinDirStat.Core.Interfaces;

public interface IDiskScanService
{
    FileSystemNode Scan(string rootPath);
}