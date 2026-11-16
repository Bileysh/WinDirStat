using Volumetric.Core.Interfaces;

namespace Volumetric.Tests.FakeService;

public class FakeSecuritySettingsService : ISecuritySettingsService
{
    public bool EncryptScanResults { get; set; }
    public bool RequireWindowsHelloOnLaunch { get; set; }
}
