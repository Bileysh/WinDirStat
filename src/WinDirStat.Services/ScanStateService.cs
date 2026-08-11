using WinDirStat.Core.Entities;
using WinDirStat.Core.Interfaces;

namespace WinDirStat.Services;

public class ScanStateService : IScanStateService
{
    public ScanResult? CurrentResult { get; private set; }

    public event EventHandler<ScanResult?>? StateChanged;

    public void SetResult(ScanResult? result)
    {
        CurrentResult = result;
        StateChanged?.Invoke(this, result);
    }
}