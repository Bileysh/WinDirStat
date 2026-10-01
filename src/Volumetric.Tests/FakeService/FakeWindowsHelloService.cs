using Volumetric.Core.Interfaces;

namespace Volumetric.Tests.FakeService;

public class FakeWindowsHelloService : IWindowsHelloService
{
    private readonly Queue<WindowsHelloVerificationResult> _verificationResults = new();

    public WindowsHelloAvailability Availability { get; set; } = WindowsHelloAvailability.Available;
    public int VerificationRequests { get; private set; }
    public IntPtr? LastOwnerHwnd { get; private set; }

    public void EnqueueVerificationResults(params WindowsHelloVerificationResult[] results)
    {
        foreach (var result in results)
        {
            _verificationResults.Enqueue(result);
        }
    }

    public Task<WindowsHelloAvailability> CheckAvailabilityAsync() => Task.FromResult(Availability);

    public Task<WindowsHelloVerificationResult> RequestVerificationAsync(IntPtr ownerHwnd, string message)
    {
        VerificationRequests++;
        LastOwnerHwnd = ownerHwnd;
        return Task.FromResult(_verificationResults.TryDequeue(out var result)
            ? result
            : WindowsHelloVerificationResult.Failed);
    }
}
