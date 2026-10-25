using Windows.Storage;
using Volumetric.Core.BackgroundScan;
using Volumetric.Core.Interfaces;

namespace Volumetric_App.Services;

public class BackgroundScanSettingsService : IBackgroundScanSettingsService
{
    private const string IntervalKey = "BackgroundScan.IntervalMinutes";
    private const string ThresholdKey = "BackgroundScan.LowFreeSpaceThresholdPercent";
    private const string AccountForHardLinksKey = "AccountForHardLinks";

    private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

    public uint ScanIntervalMinutes
    {
        get => _localSettings.Values.TryGetValue(IntervalKey, out var v) && v is uint stored
            ? stored
            : BackgroundScanSettingsValidator.MinIntervalMinutes;
        set => _localSettings.Values[IntervalKey] = BackgroundScanSettingsValidator.ClampInterval(value);
    }

    public double LowFreeSpaceThresholdPercent
    {
        get => _localSettings.Values.TryGetValue(ThresholdKey, out var v) && v is double stored
            ? stored
            : 10.0;
        set => _localSettings.Values[ThresholdKey] = BackgroundScanSettingsValidator.ClampThreshold(value);
    }

    public bool AccountForHardLinks
    {
        get => _localSettings.Values.TryGetValue(AccountForHardLinksKey, out var v) && v is bool stored && stored;
        set => _localSettings.Values[AccountForHardLinksKey] = value;
    }
}
