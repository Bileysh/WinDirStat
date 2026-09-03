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

    public string ExportToJson()
    {
        var dto = new SettingsDto(ScanIntervalMinutes, LowFreeSpaceThresholdPercent);
        return JsonSerializer.Serialize(dto, SettingsJsonContext.Default.SettingsDto);
    }

    public SettingsValidationError ImportFromJson(string json)
    {
        try
        {
            var dto = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.SettingsDto);
            if (dto is null) return SettingsValidationError.InvalidFormat;

            var validation =
                BackgroundScanSettingsValidator.ValidateImport(dto.ScanIntervalMinutes,
                    dto.LowFreeSpaceThresholdPercent);
            if (validation != SettingsValidationError.None) return validation;

            ScanIntervalMinutes = dto.ScanIntervalMinutes;
            LowFreeSpaceThresholdPercent = dto.LowFreeSpaceThresholdPercent;
            return SettingsValidationError.None;
        }
        catch (JsonException)
        {
            return SettingsValidationError.InvalidFormat;
        }
    }

    private record SettingsDto(uint ScanIntervalMinutes, double LowFreeSpaceThresholdPercent);

    [JsonSerializable(typeof(SettingsDto))]
    private partial class SettingsJsonContext : JsonSerializerContext;

    public bool AccountForHardLinks
    {
        get => _localSettings.Values.TryGetValue(AccountForHardLinksKey, out var v) && v is bool stored && stored;
        set => _localSettings.Values[AccountForHardLinksKey] = value;
    }
}