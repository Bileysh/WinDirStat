using System.Runtime.InteropServices;

namespace WinDirStat.Services;

public static class ClassicFolderPicker
{
    private const int ErrorCancelled = unchecked((int)0x800704C7);

    public static string? Show(IntPtr ownerHwnd, string? title = null)
    {
        var dialog = (IFileOpenDialog)new FileOpenDialogComObject();
        dialog.SetOptions(FOS.FOS_PICKFOLDERS | FOS.FOS_FORCEFILESYSTEM);

        if (title != null)
            dialog.SetTitle(title);

        var hr = dialog.Show(ownerHwnd);
        if (hr == ErrorCancelled) return null;
        Marshal.ThrowExceptionForHR(hr);

        dialog.GetResult(out var item);
        item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var path);
        return path;
    }

    [ComImport, Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
    private class FileOpenDialogComObject
    {
    }

    [ComImport, Guid("d57c7288-d4ad-4768-be02-9d969532d960"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFileOpenDialog
    {
        [PreserveSig]
        int Show(IntPtr parent);

        [PreserveSig]
        int SetFileTypes();

        [PreserveSig]
        int SetFileTypeIndex(int iFileType);

        [PreserveSig]
        int GetFileTypeIndex(out int piFileType);

        [PreserveSig]
        int Advise();

        [PreserveSig]
        int Unadvise();

        [PreserveSig]
        int SetOptions(FOS fos);

        [PreserveSig]
        int GetOptions(out FOS pfos);

        [PreserveSig]
        int SetDefaultFolder(IShellItem psi);

        [PreserveSig]
        int SetFolder(IShellItem psi);

        [PreserveSig]
        int GetFolder(out IShellItem ppsi);

        [PreserveSig]
        int GetCurrentSelection(out IShellItem ppsi);

        [PreserveSig]
        int SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);

        [PreserveSig]
        int GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);

        [PreserveSig]
        int SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);

        [PreserveSig]
        int SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);

        [PreserveSig]
        int SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

        [PreserveSig]
        int GetResult(out IShellItem ppsi);

        [PreserveSig]
        int AddPlace(IShellItem psi, int alignment);

        [PreserveSig]
        int SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);

        [PreserveSig]
        int Close(int hr);

        [PreserveSig]
        int SetClientGuid();

        [PreserveSig]
        int ClearClientData();

        [PreserveSig]
        int SetFilter([MarshalAs(UnmanagedType.IUnknown)] object pFilter);

        [PreserveSig]
        int GetResults(out IShellItemArray ppenum);

        [PreserveSig]
        int GetSelectedItems([MarshalAs(UnmanagedType.IUnknown)] out object ppsai);
    }

    [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
        [PreserveSig]
        int BindToHandler();

        [PreserveSig]
        int GetParent();

        [PreserveSig]
        int GetDisplayName(SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);

        [PreserveSig]
        int GetAttributes();

        [PreserveSig]
        int Compare();
    }

    [ComImport, Guid("b63ea76d-1f85-456f-a19c-48159efa858b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemArray
    {
        [PreserveSig]
        int BindToHandler();

        [PreserveSig]
        int GetPropertyStore();

        [PreserveSig]
        int GetPropertyDescriptionList();

        [PreserveSig]
        int GetAttributes();

        [PreserveSig]
        int GetCount(out int pdwNumItems);

        [PreserveSig]
        int GetItemAt(int dwIndex, out IShellItem ppsi);

        [PreserveSig]
        int EnumItems();
    }

    private enum SIGDN : uint
    {
        SIGDN_FILESYSPATH = 0x80058000
    }

    [Flags]
    private enum FOS
    {
        FOS_PICKFOLDERS = 0x20,
        FOS_FORCEFILESYSTEM = 0x40
    }
}