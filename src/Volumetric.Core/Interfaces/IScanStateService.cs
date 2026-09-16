using Volumetric.Core.Entities;

namespace Volumetric.Core.Interfaces;

public interface IScanStateService
{
    ScanResult? CurrentResult { get; }
    event EventHandler<ScanResult?>? StateChanged;
    void SetResult(ScanResult? result);
}