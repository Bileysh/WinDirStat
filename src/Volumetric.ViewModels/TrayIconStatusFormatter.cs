namespace Volumetric.ViewModels;

public static class TrayIconStatusFormatter
{
    public static string BuildTooltip(bool isScanning, long filesScanned, long foldersScanned,
        string idleTooltip, string scanningTooltipFormat) =>
        isScanning
            ? string.Format(scanningTooltipFormat, filesScanned, foldersScanned)
            : idleTooltip;
}
