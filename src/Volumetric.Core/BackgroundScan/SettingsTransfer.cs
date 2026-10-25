using System.Text.Json;
using System.Text.Json.Serialization;
using Volumetric.Core.Interfaces;

namespace Volumetric.Core.BackgroundScan;

public static partial class SettingsTransfer
{
    public static string Export(IBackgroundScanSettingsService scanSettings, ISecuritySettingsService securitySettings)
    {
        var dto = new SettingsDto(scanSettings.ScanIntervalMinutes, scanSettings.LowFreeSpaceThresholdPercent,
            scanSettings.AccountForHardLinks, securitySettings.EncryptScanResults);
        return JsonSerializer.Serialize(dto, SettingsJsonContext.Default.SettingsDto);
    }

    public static SettingsValidationError Import(string json, IBackgroundScanSettingsService scanSettings,
        ISecuritySettingsService securitySettings)
    {
        SettingsDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.SettingsDto);
        }
        catch (JsonException)
        {
            return SettingsValidationError.InvalidFormat;
        }

        if (dto is null)
        {
            return SettingsValidationError.InvalidFormat;
        }

        var validation =
            BackgroundScanSettingsValidator.ValidateImport(dto.ScanIntervalMinutes, dto.LowFreeSpaceThresholdPercent);
        if (validation != SettingsValidationError.None)
        {
            return validation;
        }

        scanSettings.ScanIntervalMinutes = dto.ScanIntervalMinutes;
        scanSettings.LowFreeSpaceThresholdPercent = dto.LowFreeSpaceThresholdPercent;
        scanSettings.AccountForHardLinks = dto.AccountForHardLinks;

        if (dto.EncryptScanResults is { } encrypt)
        {
            securitySettings.EncryptScanResults = encrypt;
        }

        return SettingsValidationError.None;
    }

    private sealed record SettingsDto(
        uint ScanIntervalMinutes,
        double LowFreeSpaceThresholdPercent,
        bool AccountForHardLinks = false,
        bool? EncryptScanResults = null);

    [JsonSerializable(typeof(SettingsDto))]
    private partial class SettingsJsonContext : JsonSerializerContext;
}
