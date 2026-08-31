using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.Storage;
using WinDirStat.Core.BackgroundScan;
using WinDirStat.Core.Interfaces;

namespace WinDirStat_App.Services;

public partial class BackgroundScanSettingsService : IBackgroundScanSettingsService
{
    private const string IntervalKey = "BackgroundScan.IntervalMinutes";
    private const string ThresholdKey = "BackgroundScan.LowFreeSpaceThresholdPercent";
 
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
 
    public string ExportToJson()
    {
        var dto = new SettingsDto(ScanIntervalMinutes, LowFreeSpaceThresholdPercent);
        return JsonSerializer.Serialize(dto, SettingsJsonContext.Default.SettingsDto);
    }
 
    public void ImportFromJson(string json)
    {
        var dto = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.SettingsDto)
                  ?? throw new FormatException("Порожній або невалідний JSON налаштувань.");
 
        BackgroundScanSettingsValidator.ValidateImport(dto.ScanIntervalMinutes, dto.LowFreeSpaceThresholdPercent);
 
        ScanIntervalMinutes = dto.ScanIntervalMinutes;
        LowFreeSpaceThresholdPercent = dto.LowFreeSpaceThresholdPercent;
    }
 
    private record SettingsDto(uint ScanIntervalMinutes, double LowFreeSpaceThresholdPercent);
 
    [JsonSerializable(typeof(SettingsDto))]
    private partial class SettingsJsonContext : JsonSerializerContext;
}