using WinDirStat.Core.Entities;

namespace WinDirStat.Core.Interfaces;

public interface IScanStateService
{
    ScanResult? CurrentResult { get; }
    event EventHandler<ScanResult?>? StateChanged;
    void SetResult(ScanResult? result);
}