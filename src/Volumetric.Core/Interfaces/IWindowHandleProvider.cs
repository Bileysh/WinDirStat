namespace Volumetric.Core.Interfaces;

public interface IWindowHandleProvider
{
    IntPtr Hwnd { get; set; }
}
