using Volumetric.Core.Entities;

namespace Volumetric.Core.Interfaces;

public interface IDiskScanService
{
    Task<ScanResult> ScanAsync(string rootPath, CancellationToken cancellationToken = default,
        bool useElevatedFallbackForAccessDenied = false, IProgress<ScanProgress>? progress = null, bool accountForHardLinks = false);
}
