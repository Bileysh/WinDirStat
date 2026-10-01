using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Volumetric.Core.BackgroundScan;
using Volumetric.Core.Interfaces;
using System.Threading.Tasks;

namespace Volumetric.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IBackgroundScanSettingsService _settings;
    private readonly IBackgroundScanTaskRegistrar _registrar;
    private readonly ISettingsFileService _fileService;
    private readonly IBackgroundScanTestRunner _testRunner;
    private readonly ILocalizationService _localizationService;
    private readonly ISecuritySettingsService _securitySettings;
    private readonly IWindowsHelloService _windowsHello;

    private readonly bool _isInitialized;
    private bool _isRevertingWindowsHelloToggle;

    [ObservableProperty]
    public partial uint ScanIntervalMinutes { get; set; }

    [ObservableProperty]
    public partial double LowFreeSpaceThresholdPercent { get; set; }

    [ObservableProperty]
    public partial bool AccountForHardLinks { get; set; }

    [ObservableProperty]
    public partial bool EncryptScanResults { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWindowsHelloToggleEnabled))]
    public partial bool RequireWindowsHelloOnLaunch { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWindowsHelloToggleEnabled))]
    public partial bool IsWindowsHelloAvailable { get; private set; }

    public bool IsWindowsHelloToggleEnabled => IsWindowsHelloAvailable || RequireWindowsHelloOnLaunch;

    [ObservableProperty]
    public partial string? WindowsHelloUnavailableReason { get; private set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    public IntPtr WindowHandle { get; set; }

    public SettingsViewModel(
        IBackgroundScanSettingsService settings,
        IBackgroundScanTaskRegistrar registrar,
        ISettingsFileService fileService,
        IBackgroundScanTestRunner testRunner,
        ILocalizationService localizationService,
        ISecuritySettingsService securitySettings,
        IWindowsHelloService windowsHello)
    {
        _settings = settings;
        _registrar = registrar;
        _fileService = fileService;
        _testRunner = testRunner;
        _localizationService = localizationService;
        _securitySettings = securitySettings;
        _windowsHello = windowsHello;

        ScanIntervalMinutes = settings.ScanIntervalMinutes;
        LowFreeSpaceThresholdPercent = settings.LowFreeSpaceThresholdPercent;
        AccountForHardLinks = settings.AccountForHardLinks;
        EncryptScanResults = securitySettings.EncryptScanResults;
        RequireWindowsHelloOnLaunch = securitySettings.RequireWindowsHelloOnLaunch;

        _isInitialized = true;
    }

    public async Task LoadWindowsHelloAvailabilityAsync()
    {
        var availability = await _windowsHello.CheckAvailabilityAsync();

        IsWindowsHelloAvailable = availability == WindowsHelloAvailability.Available;
        WindowsHelloUnavailableReason = IsWindowsHelloAvailable
            ? null
            : _localizationService.GetString($"{ResourceKeys.WindowsHelloUnavailablePrefix}{availability}");
    }

    partial void OnRequireWindowsHelloOnLaunchChanged(bool value)
    {
        if (!_isInitialized || _isRevertingWindowsHelloToggle) return;

        if (value)
        {
            _ = ConfirmEnableWindowsHelloAsync();
            return;
        }

        _securitySettings.RequireWindowsHelloOnLaunch = false;
        StatusMessage = _localizationService.GetString(ResourceKeys.RequireWindowsHelloDisabledStatus);
    }

    private async Task ConfirmEnableWindowsHelloAsync()
    {
        var result = await _windowsHello.RequestVerificationAsync(WindowHandle,
            _localizationService.GetString(ResourceKeys.WindowsHelloEnablePrompt));

        if (result == WindowsHelloVerificationResult.Verified)
        {
            _securitySettings.RequireWindowsHelloOnLaunch = true;
            StatusMessage = _localizationService.GetString(ResourceKeys.RequireWindowsHelloEnabledStatus);
            return;
        }

        _isRevertingWindowsHelloToggle = true;
        RequireWindowsHelloOnLaunch = false;
        _isRevertingWindowsHelloToggle = false;
        StatusMessage = _localizationService.GetString(ResourceKeys.WindowsHelloVerificationFailed);
    }

    partial void OnEncryptScanResultsChanged(bool value)
    {
        if (!_isInitialized) return;

        _securitySettings.EncryptScanResults = value;
        StatusMessage = _localizationService.GetString(
            value ? ResourceKeys.EncryptScanResultsEnabledStatus : ResourceKeys.EncryptScanResultsDisabledStatus);
    }

    partial void OnScanIntervalMinutesChanged(uint value)
    {
        if (!_isInitialized) return;

        _settings.ScanIntervalMinutes = value;
        _registrar.ReRegister();
        StatusMessage = string.Format(_localizationService.GetString(ResourceKeys.ScanIntervalStatus), _settings.ScanIntervalMinutes);
    }

    partial void OnLowFreeSpaceThresholdPercentChanged(double value)
    {
        if (!_isInitialized) return;

        _settings.LowFreeSpaceThresholdPercent = value;
        StatusMessage = string.Format(_localizationService.GetString(ResourceKeys.LowSpaceThresholdStatus), _settings.LowFreeSpaceThresholdPercent.ToString("F0"));
    }

    partial void OnAccountForHardLinksChanged(bool value)
    {
        if (!_isInitialized) return;

        _settings.AccountForHardLinks = value;
        StatusMessage = _localizationService.GetString(
            value ? "AccountForHardLinksEnabledStatus" : "AccountForHardLinksDisabledStatus");
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        var fileName = await _fileService.ExportAsync(_settings.ExportToJson(), "volumetric-settings", WindowHandle);
        if (fileName is null)
        {
            return;
        }

        StatusMessage = string.Format(_localizationService.GetString(ResourceKeys.SettingsExportedStatus), fileName);
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        var result = await _fileService.ImportAsync(WindowHandle);
        if (result is null)
        {
            return;
        }

        var validationResult = _settings.ImportFromJson(result.Value.Json);

        if (validationResult == SettingsValidationError.None)
        {
            ScanIntervalMinutes = _settings.ScanIntervalMinutes;
            LowFreeSpaceThresholdPercent = _settings.LowFreeSpaceThresholdPercent;
            AccountForHardLinks = _settings.AccountForHardLinks;
            StatusMessage = string.Format(_localizationService.GetString(ResourceKeys.SettingsImportedStatus), result.Value.FileName);
        }
        else
        {
            StatusMessage = _localizationService.GetString($"{ResourceKeys.SettingsErrorPrefix}{validationResult}");
        }
    }

    [RelayCommand]
    private void TestScanNow()
    {
        _testRunner.RunNow();
        StatusMessage = _localizationService.GetString(ResourceKeys.TestScanCompletedStatus);
    }
}
