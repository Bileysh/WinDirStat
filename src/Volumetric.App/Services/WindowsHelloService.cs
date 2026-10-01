using Volumetric.Core.Interfaces;
using Windows.Security.Credentials.UI;

namespace Volumetric_App.Services;

public sealed class WindowsHelloService(IAppLogger logger) : IWindowsHelloService
{
    public async Task<WindowsHelloAvailability> CheckAvailabilityAsync()
    {
        UserConsentVerifierAvailability result;
        try
        {
            result = await UserConsentVerifier.CheckAvailabilityAsync();
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Windows Hello availability check failed");
            return WindowsHelloAvailability.Unknown;
        }

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

    public async Task<WindowsHelloVerificationResult> RequestVerificationAsync(IntPtr ownerHwnd, string message)
    {
        UserConsentVerificationResult result;
        try
        {
            result = await UserConsentVerifierInterop.RequestVerificationForWindowAsync(ownerHwnd, message);
        }
        catch (Exception ex)
        {
            logger.Warning(ex, "Windows Hello verification failed");
            return WindowsHelloVerificationResult.Failed;
        }

        return result switch
        {
            UserConsentVerificationResult.Verified => WindowsHelloVerificationResult.Verified,
            UserConsentVerificationResult.Canceled => WindowsHelloVerificationResult.Canceled,
            UserConsentVerificationResult.DeviceNotPresent
                or UserConsentVerificationResult.NotConfiguredForUser
                or UserConsentVerificationResult.DisabledByPolicy => WindowsHelloVerificationResult.Unavailable,
            _ => WindowsHelloVerificationResult.Failed
        };
    }
}
