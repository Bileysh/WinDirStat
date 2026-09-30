using Volumetric.Core.Interfaces;
using Windows.Security.Credentials.UI;

namespace Volumetric_App.Services;

public sealed class WindowsHelloAvailabilityService : IWindowsHelloAvailabilityService
{
    public async Task<WindowsHelloAvailability> CheckAvailabilityAsync()
    {
        var result = await UserConsentVerifier.CheckAvailabilityAsync();

        return result switch
        {
            UserConsentVerifierAvailability.Available => WindowsHelloAvailability.Available,
            UserConsentVerifierAvailability.DeviceNotPresent => WindowsHelloAvailability.DeviceNotPresent,
            UserConsentVerifierAvailability.NotConfiguredForUser => WindowsHelloAvailability.NotConfiguredForUser,
            UserConsentVerifierAvailability.DisabledByPolicy => WindowsHelloAvailability.DisabledByPolicy,
            UserConsentVerifierAvailability.DeviceBusy => WindowsHelloAvailability.DeviceBusy,
            _ => WindowsHelloAvailability.Unknown
        };
    }
}
