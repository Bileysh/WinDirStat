namespace WinDirStat.Core.Interfaces;

public interface IWindowHandleProvider
{
    IntPtr Hwnd { get; set; }
}