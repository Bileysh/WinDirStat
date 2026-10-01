using Windows.Storage;
using Volumetric.Core.Interfaces;

namespace Volumetric_App.Services;

public sealed class SecuritySettingsService : ISecuritySettingsService
{
    private const string EncryptScanResultsKey = "Security.EncryptScanResults";

    private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

    public bool EncryptScanResults
    {
        get => _localSettings.Values.TryGetValue(EncryptScanResultsKey, out var v) && v is true;
        set => _localSettings.Values[EncryptScanResultsKey] = value;
    }
}
