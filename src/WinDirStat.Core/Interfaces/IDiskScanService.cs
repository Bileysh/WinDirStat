using WinDirStat.Core.Entities;

namespace WinDirStat.Core.Interfaces;

public interface IDiskScanService
{
    ScanResult Scan(string rootPath);
}