using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinDirStat.Core.BackgroundScan;
using WinDirStat.Core.Interfaces;
using System.Threading.Tasks;

namespace WinDirStat.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IBackgroundScanSettingsService _settings;
    private readonly IBackgroundScanTaskRegistrar _registrar;
    private readonly ISettingsFileService _fileService;
    private readonly IBackgroundScanTestRunner _testRunner;
    private readonly ILocalizationService _localizationService;

    [ObservableProperty]
    private uint _scanIntervalMinutes;

    [ObservableProperty]
    private double _lowFreeSpaceThresholdPercent;

    [ObservableProperty]
    private string? _statusMessage;

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
        _scanIntervalMinutes = settings.ScanIntervalMinutes;
        _lowFreeSpaceThresholdPercent = settings.LowFreeSpaceThresholdPercent;
    }

    partial void OnScanIntervalMinutesChanged(uint value)
    {
        _settings.ScanIntervalMinutes = value;
        _registrar.ReRegister();
        StatusMessage = string.Format(_localizationService.GetString("ScanIntervalStatus"), _settings.ScanIntervalMinutes);
    }

    partial void OnLowFreeSpaceThresholdPercentChanged(double value)
    {
        _settings.LowFreeSpaceThresholdPercent = value;
        StatusMessage = string.Format(_localizationService.GetString("LowSpaceThresholdStatus"), _settings.LowFreeSpaceThresholdPercent.ToString("F0"));
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        var fileName = await _fileService.ExportAsync(_settings.ExportToJson(), "windirstat-settings");
        if (fileName is null) return;

        StatusMessage = string.Format(_localizationService.GetString("SettingsExportedStatus"), fileName);
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        var result = await _fileService.ImportAsync();
        if (result is null) return;

        var validationResult = _settings.ImportFromJson(result.Value.Json);

        if (validationResult == SettingsValidationError.None)
        {
            ScanIntervalMinutes = _settings.ScanIntervalMinutes;
            LowFreeSpaceThresholdPercent = _settings.LowFreeSpaceThresholdPercent;
            StatusMessage = string.Format(_localizationService.GetString("SettingsImportedStatus"), result.Value.FileName);
        }
        else
        {
            StatusMessage = _localizationService.GetString($"SettingsError_{validationResult}");
        }
    }

    [RelayCommand]
    private void TestScanNow()
    {
        _testRunner.RunNow();
        StatusMessage = _localizationService.GetString("TestScanCompletedStatus");
    }
}