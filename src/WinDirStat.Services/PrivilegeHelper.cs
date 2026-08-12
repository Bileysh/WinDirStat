using System;
using System.Runtime.InteropServices;

namespace WinDirStat.Services;

public static class PrivilegeHelper
{
    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, out IntPtr TokenHandle);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool LookupPrivilegeValue(string? lpSystemName, string lpName, out LUID lpLuid);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

    private const int TOKEN_ADJUST_PRIVILEGES = 0x0020;
    private const int TOKEN_QUERY = 0x0008;
    private const int SE_PRIVILEGE_ENABLED = 0x00000002;
    private const string SE_BACKUP_NAME = "SeBackupPrivilege";

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TOKEN_PRIVILEGES
    {
        public int PrivilegeCount;
        public LUID Luid;
        public int Attributes;
    }
    
    public static bool EnableBackupPrivilege()
    {
        try
        {
            if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out var tokenHandle))
                return false;

            if (!LookupPrivilegeValue(null, SE_BACKUP_NAME, out var luid))
                return false;

            var tp = new TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Luid = luid,
                Attributes = SE_PRIVILEGE_ENABLED
            };

            var result = AdjustTokenPrivileges(tokenHandle, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
            
            return result && Marshal.GetLastWin32Error() == 0;
        }
        catch
        {
            return false;
        }
    }
}