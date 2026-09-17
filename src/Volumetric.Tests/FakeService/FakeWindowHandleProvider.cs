using Volumetric.Core.Interfaces;

namespace Volumetric.Tests.FakeService;

public class FakeWindowHandleProvider : IWindowHandleProvider
{
    public IntPtr Hwnd { get; set; }
}
