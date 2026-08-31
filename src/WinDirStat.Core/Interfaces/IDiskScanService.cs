using WinDirStat.Core.Entities;

namespace WinDirStat.Core.Interfaces;

public interface IDiskScanService
{
    Task<ScanResult> ScanAsync(string rootPath, CancellationToken cancellationToken = default,
        bool useElevatedFallbackForAccessDenied = false);
}