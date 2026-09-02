using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using WinDirStat.Core.Interfaces;

namespace WinDirStat.Services;

public class FileExplorerService : IFileExplorerService
{
    public void OpenInExplorer(string fullPath, bool isDirectory)
    {
        if (isDirectory)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = fullPath,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        else
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{fullPath}\"",
                UseShellExecute = true
            });
        }
    }

    public void ShowProperties(string fullPath)
    {
        var succeeded = ShellInterop.SHObjectProperties(
            IntPtr.Zero, ShellInterop.SHOP_FILEPATH, fullPath, null);

        if (!succeeded)
        {
            var error = Marshal.GetLastWin32Error();
            throw new Win32Exception(error,
                $"SHObjectProperties returned false for '{fullPath}' (Win32 error {error}).");
        }
    }

    private static class ShellInterop
    {
        public const uint SHOP_FILEPATH = 0x2;

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SHObjectProperties(
            IntPtr hwnd, uint shopObjectType, string pszObjectName, string? pszPropertyPage);
    }
}
