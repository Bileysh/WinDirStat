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

    private readonly bool _isInitialized;

    [ObservableProperty]
    public partial uint ScanIntervalMinutes { get; set; }

    [ObservableProperty]
    public partial double LowFreeSpaceThresholdPercent { get; set; }

    [ObservableProperty]
    public partial bool AccountForHardLinks { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    public IntPtr WindowHandle { get; set; }

    public SettingsViewModel(
        IBackgroundScanSettingsService settings,
        IBackgroundScanTaskRegistrar registrar,
        ISettingsFileService fileService,
        IBackgroundScanTestRunner testRunner,
        ILocalizationService localizationService)
    {
        _settings = settings;
        _registrar = registrar;
        _fileService = fileService;
        _testRunner = testRunner;
        _localizationService = localizationService;

        ScanIntervalMinutes = settings.ScanIntervalMinutes;
        LowFreeSpaceThresholdPercent = settings.LowFreeSpaceThresholdPercent;
        AccountForHardLinks = settings.AccountForHardLinks;

        _isInitialized = true;
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
