using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.ApplicationModel.Background;
using WinDirStat.WinRT;
using WinRT;

namespace WinDirStat_App.Services;

public static partial class ComServer
{
    public const uint CLSCTX_LOCAL_SERVER = 4;
    public const uint REGCLS_MULTIPLEUSE = 1;

    private const uint S_OK = 0x00000000;
    private const uint CLASS_E_NOAGGREGATION = 0x80040110;
    private const uint E_NOINTERFACE = 0x80004002;
    private const string IID_IUnknown = "00000000-0000-0000-C000-000000000046";
    private const string IID_IClassFactory = "00000001-0000-0000-C000-000000000046";

    [LibraryImport("ole32.dll")]
    public static partial int CoRegisterClassObject(
        ref Guid classId,
        [MarshalAs(UnmanagedType.Interface)] IClassFactory objectAsUnknown,
        uint executionContext, uint flags, out uint registrationToken);

    [LibraryImport("ole32.dll")]
    public static partial int CoRevokeClassObject(uint registrationToken);

    [GeneratedComInterface, Guid(IID_IClassFactory), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IClassFactory
    {
        [PreserveSig] uint CreateInstance(IntPtr objectAsUnknown, in Guid interfaceId, out IntPtr objectPointer);
        [PreserveSig] uint LockServer([MarshalAs(UnmanagedType.Bool)] bool lockServer);
    }

    [GeneratedComClass]
    internal sealed partial class BackgroundTaskFactory : IClassFactory
    {
        public uint CreateInstance(IntPtr objectAsUnknown, in Guid interfaceId, out IntPtr objectPointer)
        {
            if (objectAsUnknown != IntPtr.Zero)
            {
                objectPointer = IntPtr.Zero;
                return CLASS_E_NOAGGREGATION;
            }

            if (interfaceId != typeof(BackgroundScanTask).GUID && interfaceId != new Guid(IID_IUnknown))
            {
                objectPointer = IntPtr.Zero;
                return E_NOINTERFACE;
            }

            objectPointer = MarshalInterface<IBackgroundTask>.FromManaged(new BackgroundScanTask());
            return S_OK;
        }

        public uint LockServer(bool lockServer) => S_OK;
    }
}