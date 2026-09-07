using WinDirStat.Core.Interfaces;

namespace WinDirStat.Tests.FakeService;

public class FakeWindowHandleProvider: IWindowHandleProvider
{
    public IntPtr Hwnd { get; set; }
}