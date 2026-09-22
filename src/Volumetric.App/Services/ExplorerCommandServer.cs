using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Serilog;

namespace Volumetric_App.Services;

public static partial class ExplorerCommandServer
{
    private const uint S_OK = 0x00000000;
    private const uint E_NOTIMPL = 0x80004001;
    private const uint CLASS_E_NOAGGREGATION = 0x80040110;
    private const uint E_NOINTERFACE = 0x80004002;
    private const string IID_IUnknown = "00000000-0000-0000-C000-000000000046";

    public const string ExplorerCommandClsid = "6C3F1A9D-2E48-4B7A-9F0C-1D8E5A3B7C21";
    private const string AppProtocolScheme = "volumetric";

    private static readonly StrategyBasedComWrappers ComWrappers = new();

    [GeneratedComInterface, Guid("a08ce4d0-fa25-44ab-b57c-c7b1c323e0b9"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IExplorerCommand
    {
        [PreserveSig]
        uint GetTitle(IntPtr items, out IntPtr name);

        [PreserveSig]
        uint GetIcon(IntPtr items, out IntPtr icon);

        [PreserveSig]
        uint GetToolTip(IntPtr items, out IntPtr infoTip);

        [PreserveSig]
        uint GetCanonicalName(out Guid guidCommandName);

        [PreserveSig]
        uint GetState(IntPtr items, [MarshalAs(UnmanagedType.Bool)] bool okToBeSlow, out int cmdState);

        [PreserveSig]
        uint Invoke(IntPtr items, IntPtr bindCtx);

        [PreserveSig]
        uint GetFlags(out int flags);

        [PreserveSig]
        uint EnumSubCommands(out IntPtr enumCommands);
    }

    [GeneratedComInterface, Guid("b63ea76d-1f85-456f-a19c-48159efa858b"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IShellItemArray
    {
        [PreserveSig]
        uint BindToHandler(IntPtr bc, in Guid bhid, in Guid riid, out IntPtr ppv);

        [PreserveSig]
        uint GetPropertyStore(int flags, in Guid riid, out IntPtr ppv);

        [PreserveSig]
        uint GetPropertyDescriptionList(IntPtr keyType, in Guid riid, out IntPtr ppv);

        [PreserveSig]
        uint GetAttributes(int attribFlags, int mask, out int attribs);

        [PreserveSig]
        uint GetCount(out uint count);

        [PreserveSig]
        uint GetItemAt(uint index, out IntPtr item);

        [PreserveSig]
        uint EnumItems(out IntPtr enumShellItems);
    }

    [GeneratedComInterface, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IShellItem
    {
        [PreserveSig]
        uint BindToHandler(IntPtr bc, in Guid bhid, in Guid riid, out IntPtr ppv);

        [PreserveSig]
        uint GetParent(out IntPtr parent);

        [PreserveSig]
        uint GetDisplayName(int sigdnName, out IntPtr name);

        [PreserveSig]
        uint GetAttributes(int sfgaoMask, out int attribs);

        [PreserveSig]
        uint Compare(IntPtr other, int hint, out int order);
    }

    private const int SIGDN_FILESYSPATH = unchecked((int)0x80058000);

    [GeneratedComClass]
    public sealed partial class ScanWithVolumetricCommand : IExplorerCommand
    {
        public uint GetTitle(IntPtr items, out IntPtr name)
        {
            Log.Information("GetTitle called");
            name = Marshal.StringToCoTaskMemUni("Scan with Volumetric");
            return S_OK;
        }

        public uint GetIcon(IntPtr items, out IntPtr icon)
        {
            try
            {
                var exeDir = Path.GetDirectoryName(Environment.ProcessPath);
                var iconPath = exeDir is null ? null : Path.Combine(exeDir, "Assets", "AppIcon.ico");

                if (iconPath is null || !File.Exists(iconPath))
                {
                    Log.Warning("GetIcon: icon file not found (exeDir={ExeDir}, iconPath={IconPath})",
                        exeDir, iconPath);
                    icon = IntPtr.Zero;
                    return E_NOTIMPL;
                }
                icon = Marshal.StringToCoTaskMemUni($"{iconPath},0");
                return S_OK;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "GetIcon failed");
                icon = IntPtr.Zero;
                return E_NOTIMPL;
            }
        }

        public uint GetToolTip(IntPtr items, out IntPtr infoTip)
        {
            infoTip = IntPtr.Zero;
            return E_NOTIMPL;
        }

        public uint GetCanonicalName(out Guid guidCommandName)
        {
            guidCommandName = Guid.Empty;
            return S_OK;
        }

        public uint GetState(IntPtr items, bool okToBeSlow, out int cmdState)
        {
            Log.Information("GetState called (okToBeSlow={OkToBeSlow})", okToBeSlow);
            cmdState = 0;
            return S_OK;
        }

        public uint GetFlags(out int flags)
        {
            flags = 0;
            return S_OK;
        }

        public uint EnumSubCommands(out IntPtr enumCommands)
        {
            enumCommands = IntPtr.Zero;
            return E_NOTIMPL;
        }

        public uint Invoke(IntPtr itemsPtr, IntPtr bindCtx)
        {
            Log.Information("Invoke called");
            try
            {
                var path = GetFirstSelectedPath(itemsPtr);
                Log.Information("Invoke: resolved path = '{Path}'", path);
                if (path is null) return S_OK;

                var protocolUri = $"{AppProtocolScheme}://scan?path={Uri.EscapeDataString(path)}";
                Log.Information("Invoke: launching URI '{Uri}'", protocolUri);

                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = protocolUri,
                    UseShellExecute = true
                };

                var process = System.Diagnostics.Process.Start(startInfo);
                Log.Information("Invoke: launched process, pid={Pid}", process?.Id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Invoke failed");
            }
            finally
            {
                Program.ExitEvent.Set();
            }

            return S_OK;
        }

        private static string? GetFirstSelectedPath(IntPtr itemsPtr)
        {
            if (itemsPtr == IntPtr.Zero) return null;

            if (ComWrappers.GetOrCreateObjectForComInstance(itemsPtr, CreateObjectFlags.UniqueInstance)
                is not IShellItemArray itemsArray) return null;

            if (itemsArray.GetCount(out var count) != S_OK || count == 0) return null;
            if (itemsArray.GetItemAt(0, out var itemPtr) != S_OK) return null;

            if (ComWrappers.GetOrCreateObjectForComInstance(itemPtr, CreateObjectFlags.UniqueInstance)
                is not IShellItem item) return null;

            if (item.GetDisplayName(SIGDN_FILESYSPATH, out var namePtr) != S_OK) return null;

            return Marshal.PtrToStringUni(namePtr);
        }
    }

    [GeneratedComClass]
    internal sealed partial class ExplorerCommandFactory : ComServer.IClassFactory
    {
        public uint CreateInstance(IntPtr pUnkOuter, in Guid riid, out IntPtr ppvObject)
        {
            Log.Debug("ExplorerCommandFactory.CreateInstance called (riid={Riid})", riid);
            ppvObject = IntPtr.Zero;

            if (pUnkOuter != IntPtr.Zero)
            {
                Log.Warning("CreateInstance: aggregation requested, rejecting (CLASS_E_NOAGGREGATION)");
                return CLASS_E_NOAGGREGATION;
            }

            try
            {
                var instance = new ScanWithVolumetricCommand();
                var iUnknownPtr = ComWrappers.GetOrCreateComInterfaceForObject(instance, CreateComInterfaceFlags.None);

                if (riid == new Guid(IID_IUnknown))
                {
                    ppvObject = iUnknownPtr;
                    return S_OK;
                }

                var riidLocal = riid;
                var hr = Marshal.QueryInterface(iUnknownPtr, ref riidLocal, out ppvObject);
                Marshal.Release(iUnknownPtr);

                if (hr != S_OK)
                {
                    Log.Warning("CreateInstance: Interface {Riid} not supported (E_NOINTERFACE)", riid);
                    return E_NOINTERFACE;
                }

                Log.Information("CreateInstance: Successfully created object for interface {Riid}", riid);
                return S_OK;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "CreateInstance: failed to create native COM wrapper");
                return E_NOINTERFACE;
            }
        }

        public uint LockServer(bool lockServer) => S_OK;
    }
}
