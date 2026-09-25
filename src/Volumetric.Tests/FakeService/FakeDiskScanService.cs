using Volumetric.Core.Entities;
using Volumetric.Core.Interfaces;

namespace Volumetric.Tests.FakeService;

public class FakeDiskScanService : IDiskScanService
{
    private int _scanCount;

    public int ScanCount => _scanCount;

    public Task<ScanResult> ScanAsync(string rootPath, CancellationToken cancellationToken = default,
        bool useElevatedFallbackForAccessDenied = false, IProgress<ScanProgress>? progress = null,
        bool accountForHardLinks = false)
    {
        Interlocked.Increment(ref _scanCount);
        return new TaskCompletionSource<ScanResult>().Task;
    }
}
