using WinDirStat.Core.Interfaces;

namespace WinDirStat_App.Services;

public class WindowHandleProvider : IWindowHandleProvider
{
    public IntPtr Hwnd { get; set; }
}