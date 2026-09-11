using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using WinRT;

namespace WinDirStat_App.Services;

public static partial class ExplorerCommandServer
{
    private const uint S_OK = 0x00000000;
    private const uint E_NOTIMPL = 0x80004001;
    private const uint CLASS_E_NOAGGREGATION = 0x80040110;
    private const uint E_NOINTERFACE = 0x80004002;
    private const string IID_IUnknown = "00000000-0000-0000-C000-000000000046";

    public const string ExplorerCommandClsid = "6C3F1A9D-2E48-4B7A-9F0C-1D8E5A3B7C21";

    [GeneratedComInterface, Guid("a08ce4d0-fa25-44ab-b57c-c7b1c323e0b9"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IExplorerCommand
    {
        [PreserveSig] uint GetTitle(IntPtr items, out IntPtr name);
        [PreserveSig] uint GetIcon(IntPtr items, out IntPtr icon);
        [PreserveSig] uint GetToolTip(IntPtr items, out IntPtr infoTip);
        [PreserveSig] uint GetCanonicalName(out Guid guidCommandName);
        [PreserveSig] uint GetState(IntPtr items, [MarshalAs(UnmanagedType.Bool)] bool okToBeSlow, out int cmdState);
        [PreserveSig] uint Invoke(IntPtr items, IntPtr bindCtx);
        [PreserveSig] uint GetFlags(out int flags);
        [PreserveSig] uint EnumSubCommands(out IntPtr enumCommands);
    }

    [GeneratedComInterface, Guid("b63ea76d-1f85-456f-a19c-48159efa858b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IShellItemArray
    {
        [PreserveSig] uint BindToHandler(IntPtr bc, in Guid bhid, in Guid riid, out IntPtr ppv);
        [PreserveSig] uint GetPropertyStore(int flags, in Guid riid, out IntPtr ppv);
        [PreserveSig] uint GetPropertyDescriptionList(IntPtr keyType, in Guid riid, out IntPtr ppv);
        [PreserveSig] uint GetAttributes(int attribFlags, int mask, out int attribs);
        [PreserveSig] uint GetCount(out uint count);
        [PreserveSig] uint GetItemAt(uint index, out IntPtr item);
        [PreserveSig] uint EnumItems(out IntPtr enumShellItems);
    }

    [GeneratedComInterface, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface IShellItem
    {
        [PreserveSig] uint BindToHandler(IntPtr bc, in Guid bhid, in Guid riid, out IntPtr ppv);
        [PreserveSig] uint GetParent(out IntPtr parent);
        [PreserveSig] uint GetDisplayName(int sigdnName, out IntPtr name);
        [PreserveSig] uint GetAttributes(int sfgaoMask, out int attribs);
        [PreserveSig] uint Compare(IntPtr other, int hint, out int order);
    }

    private const int SIGDN_FILESYSPATH = unchecked((int)0x80058000);

    [GeneratedComClass]
    public sealed partial class ScanWithWinDirStatCommand : IExplorerCommand
    {
        public uint GetTitle(IntPtr items, out IntPtr name)
        {
            name = Marshal.StringToCoTaskMemUni("Scan with WinDirStat");
            return S_OK;
        }

        public uint GetIcon(IntPtr items, out IntPtr icon)
        {
            icon = IntPtr.Zero;
            return E_NOTIMPL;
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
            try
            {
                var path = GetFirstSelectedPath(itemsPtr);
                if (path is null) return S_OK;

                var exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath)) return S_OK;

                System.Diagnostics.Process.Start(exePath, $"\"{path}\"");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ExplorerCommandServer] Invoke failed: {ex}");
            }

            return S_OK;
        }

        private static string? GetFirstSelectedPath(IntPtr itemsPtr)
        {
            if (itemsPtr == IntPtr.Zero) return null;

            var itemsArray = MarshalInterface<object>.FromAbi(itemsPtr) as IShellItemArray;
            if (itemsArray is null) return null;

            if (itemsArray.GetCount(out var count) != S_OK || count == 0) return null;
            if (itemsArray.GetItemAt(0, out var itemPtr) != S_OK) return null;

            var item = MarshalInterface<object>.FromAbi(itemPtr) as IShellItem;
            if (item is null) return null;

            if (item.GetDisplayName(SIGDN_FILESYSPATH, out var namePtr) != S_OK) return null;

            return Marshal.PtrToStringUni(namePtr);
        }
    }

    [GeneratedComClass]
    internal sealed partial class ExplorerCommandFactory : ComServer.IClassFactory
    {
        public uint CreateInstance(IntPtr objectAsUnknown, in Guid interfaceId, out IntPtr objectPointer)
        {
            if (objectAsUnknown != IntPtr.Zero)
            {
                objectPointer = IntPtr.Zero;
                return CLASS_E_NOAGGREGATION;
            }

            objectPointer = MarshalInterface<IExplorerCommand>.FromManaged(new ScanWithWinDirStatCommand());
            return S_OK;
        }

        public uint LockServer(bool lockServer) => S_OK;
    }
}