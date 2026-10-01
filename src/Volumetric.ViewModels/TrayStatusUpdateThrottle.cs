namespace Volumetric.ViewModels;

public sealed class TrayStatusUpdateThrottle(long intervalMs)
{
    private long? _lastUpdateMs;

    public bool ShouldUpdate(string? propertyName, long nowMs)
    {
        if (propertyName == nameof(MainPageViewModel.IsScanning))
        {
            return true;
        }

        if (propertyName is not (nameof(MainPageViewModel.ScanFilesCount)
            or nameof(MainPageViewModel.ScanFoldersCount)))
        {
            return false;
        }

        return _lastUpdateMs is not { } last || nowMs - last >= intervalMs;
    }

    public void MarkUpdated(long nowMs) => _lastUpdateMs = nowMs;
}
