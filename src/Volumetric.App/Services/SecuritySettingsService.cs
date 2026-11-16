using Windows.Storage;
using Volumetric.Core.Interfaces;

namespace Volumetric_App.Services;

public sealed class SecuritySettingsService : ISecuritySettingsService
{
    private const string EncryptScanResultsKey = "Security.EncryptScanResults";
    private const string RequireWindowsHelloOnLaunchKey = "Security.RequireWindowsHelloOnLaunch";

    private readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

    public bool EncryptScanResults
    {
        get => GetFlag(EncryptScanResultsKey);
        set => _localSettings.Values[EncryptScanResultsKey] = value;
    }

    public bool RequireWindowsHelloOnLaunch
    {
        get => GetFlag(RequireWindowsHelloOnLaunchKey);
        set => _localSettings.Values[RequireWindowsHelloOnLaunchKey] = value;
    }

    private bool GetFlag(string key) => _localSettings.Values.TryGetValue(key, out var v) && v is true;
}
