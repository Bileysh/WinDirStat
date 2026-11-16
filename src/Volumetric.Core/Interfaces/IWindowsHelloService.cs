namespace Volumetric.Core.Interfaces;

public enum WindowsHelloAvailability
{
    Available,
    DeviceNotPresent,
    NotConfiguredForUser,
    DisabledByPolicy,
    DeviceBusy,
    Unknown
}

public enum WindowsHelloVerificationResult
{
    Verified,
    Canceled,
    Failed,
    Unavailable
}

public interface IWindowsHelloService
{
    Task<WindowsHelloAvailability> CheckAvailabilityAsync();
    Task<WindowsHelloVerificationResult> RequestVerificationAsync(IntPtr ownerHwnd, string message);
}
