using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WinDirStat_App.Services;

internal static class ElevatedRelauncher
{
    private const string ApplicationActivationManagerClsid = "45BA127D-10A8-46EA-8AB7-56EA9078943C";

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("2E941141-7F97-4756-BA1D-9DECDE894A3D")]
    private interface IApplicationActivationManager
    {
        [PreserveSig]
        int ActivateApplication(
            [In, MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
            [In, MarshalAs(UnmanagedType.LPWStr)] string arguments,
            [In] uint options,
            [Out] out uint processId);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct BIND_OPTS3
    {
        public int cbStruct;
        public int grfFlags;
        public int grfMode;
        public int dwTickCountDeadline;
        public int dwTrackFlags;
        public int dwClassContext;
        public int locale;
        public IntPtr pServerInfo;
        public IntPtr hwnd;
    }

    [DllImport("ole32.dll")]
    private static extern int CoGetObject(
        [MarshalAs(UnmanagedType.LPWStr)] string pszName,
        ref BIND_OPTS3 pBindOptions,
        [In] ref Guid riid,
        [MarshalAs(UnmanagedType.IUnknown)] out object ppv);

    private const int CLSCTX_LOCAL_SERVER = 0x4;
    
    public static bool TryLaunchElevatedClassic()
    {
        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (exePath is null) return false;

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(exePath) ?? Environment.CurrentDirectory
            };
            Process.Start(psi);
            return true;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false; // користувач відхилив UAC або інша системна помилка
        }
    }
}