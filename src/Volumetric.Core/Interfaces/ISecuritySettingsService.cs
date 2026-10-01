namespace Volumetric.Core.Interfaces;

public interface ISecuritySettingsService
{
    bool EncryptScanResults { get; set; }
    bool RequireWindowsHelloOnLaunch { get; set; }
}
