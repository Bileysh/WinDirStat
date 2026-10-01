using System.Runtime.Versioning;
using System.Security.Principal;

namespace Volumetric.Services;

public static class ElevationHelper
{
    [SupportedOSPlatform("windows")]
    public static bool IsElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
