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

public interface IWindowsHelloAvailabilityService
{
    Task<WindowsHelloAvailability> CheckAvailabilityAsync();
}
