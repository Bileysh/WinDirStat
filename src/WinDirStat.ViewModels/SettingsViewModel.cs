using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinDirStat.Core.Interfaces;

namespace WinDirStat.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IBackgroundScanSettingsService _settings;
    private readonly IBackgroundScanTaskRegistrar _registrar;
    private readonly ISettingsFileService _fileService;
    private readonly IBackgroundScanTestRunner _testRunner;

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
        IBackgroundScanTestRunner testRunner)
    {
        _settings = settings;
        _registrar = registrar;
        _fileService = fileService;
        _testRunner = testRunner;
        _scanIntervalMinutes = settings.ScanIntervalMinutes;
        _lowFreeSpaceThresholdPercent = settings.LowFreeSpaceThresholdPercent;
    }

    partial void OnScanIntervalMinutesChanged(uint value)
    {
        _settings.ScanIntervalMinutes = value;
        _registrar.ReRegister();
        StatusMessage = $"Інтервал сканування: {_settings.ScanIntervalMinutes} хв.";
    }

    partial void OnLowFreeSpaceThresholdPercentChanged(double value)
    {
        _settings.LowFreeSpaceThresholdPercent = value;
        StatusMessage = $"Поріг попередження: {_settings.LowFreeSpaceThresholdPercent:F0}%.";
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        var fileName = await _fileService.ExportAsync(_settings.ExportToJson(), "windirstat-settings");
        if (fileName is null) return;

        StatusMessage = $"Налаштування збережено у {fileName}.";
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        var result = await _fileService.ImportAsync();
        if (result is null) return;

        try
        {
            _settings.ImportFromJson(result.Value.Json);

            ScanIntervalMinutes = _settings.ScanIntervalMinutes;
            LowFreeSpaceThresholdPercent = _settings.LowFreeSpaceThresholdPercent;

            StatusMessage = $"Налаштування імпортовано з {result.Value.FileName}.";
        }
        catch (FormatException ex)
        {
            StatusMessage = $"Не вдалось імпортувати: {ex.Message}";
        }
    }

    [RelayCommand]
    private void TestScanNow()
    {
        _testRunner.RunNow();
        StatusMessage = "Тестовий скан виконано — перевір тост.";
    }
}