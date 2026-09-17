using Volumetric.Core.Interfaces;

namespace Volumetric_App.Services;

public class WindowHandleProvider : IWindowHandleProvider
{
    public IntPtr Hwnd { get; set; }
}