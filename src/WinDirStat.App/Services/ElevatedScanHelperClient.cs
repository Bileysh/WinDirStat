using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Windows.ApplicationModel;
using WinDirStat.Core.Entities;
using WinDirStat.Core.Interfaces;

namespace WinDirStat_App.Services;

public sealed class ElevatedScanHelperClient : IElevatedScanHelper
{
    public const string ElevatedScanArg = "--elevated-scan";

    private const string ApplicationActivationManagerClsid = "45BA127D-10A8-46EA-8AB7-56EA9078943C";
    private static readonly Guid IidApplicationActivationManager = new("2E941141-7F97-4756-BA1D-9DECDE894A3D");

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

    public bool TryScanElevated(IReadOnlyList<string> paths, out IReadOnlyDictionary<string, FileSystemNode> results)
    {
        results = new Dictionary<string, FileSystemNode>();

        if (paths.Count == 0) return true;

        var inputFile = Path.Combine(Path.GetTempPath(), $"windirstat-elevated-scan-in-{Guid.NewGuid():N}.txt");
        var outputFile = Path.Combine(Path.GetTempPath(), $"windirstat-elevated-scan-{Guid.NewGuid():N}.json");
        File.WriteAllLines(inputFile, paths);
        var arguments = $"{ElevatedScanArg} \"{inputFile}\" \"{outputFile}\"";

        try
        {
            var launched = TryLaunchElevated(arguments, out var process);
            if (!launched)
            {
                return false;
            }

            if (process is not null)
            {
                process.WaitForExit();
            }
            else if (!WaitForOutputFile(outputFile, TimeSpan.FromMinutes(2)))
            {
                return false;
            }

            if (!File.Exists(outputFile))
            {
                return false;
            }

            var json = File.ReadAllText(outputFile);
            var deserialized =
                JsonSerializer.Deserialize(json, FileSystemNodeJsonContext.Default.DictionaryStringFileSystemNode);
            results = deserialized ?? new Dictionary<string, FileSystemNode>();
            return true;
        }
        catch (Win32Exception)
        {
            return false;
        }
        catch (Exception ex)
        { 
            Debug.WriteLine($"[ElevatedScanHelper] Batch scan failed: {ex}");
            return false;
        }
        finally
        {
            try
            {
                File.Delete(inputFile);
            }
            catch
            {
                // ignored
            }

            try
            {
                File.Delete(outputFile);
            }
            catch
            {
                // ignored
            }
        }
    }

    private static bool TryLaunchElevated(string arguments, out Process? fallbackProcess)
    {
        fallbackProcess = null;

        var aumid = GetCurrentAppUserModelId();
        if (aumid is not null && TryLaunchViaActivationManager(aumid, arguments))
        {
            return true;
        }

        return TryLaunchViaProcessStart(arguments, out fallbackProcess);
    }

    private static string? GetCurrentAppUserModelId()
    {
        try
        {
            return $"{Package.Current.Id.FamilyName}!App";
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static bool TryLaunchViaActivationManager(string appUserModelId, string arguments)
    {
        try
        {
            var bindOptions = new BIND_OPTS3
            {
                cbStruct = Marshal.SizeOf<BIND_OPTS3>(),
                dwClassContext = CLSCTX_LOCAL_SERVER
            };

            var moniker = $"Elevation:Administrator!new:{{{ApplicationActivationManagerClsid}}}";
            var iid = IidApplicationActivationManager;

            var hr = CoGetObject(moniker, ref bindOptions, ref iid, out var comObject);
            if (hr != 0 || comObject is not IApplicationActivationManager manager)
                return false;

            hr = manager.ActivateApplication(appUserModelId, arguments, 0, out _);
            return hr == 0;
        }
        catch (COMException)
        {
            return false;
        }
    }

    private static bool TryLaunchViaProcessStart(string arguments, out Process? process)
    {
        process = null;

        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
        {
            return false;
        }

        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = arguments,
            UseShellExecute = true,
            Verb = "runas",
            WorkingDirectory = Path.GetDirectoryName(exePath) ?? Environment.CurrentDirectory
        };

        process = Process.Start(psi);
        return process is not null;
    }

    private static bool WaitForOutputFile(string outputFile, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (File.Exists(outputFile))
            {
                return true;
            }

            Thread.Sleep(250);
        }

        return false;
    }
}