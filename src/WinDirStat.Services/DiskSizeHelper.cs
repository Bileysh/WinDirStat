using System.Runtime.InteropServices;

namespace WinDirStat.Services;

public static class DiskSizeHelper
{
    private const uint INVALID_FILE_SIZE = 0xFFFFFFFF;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetCompressedFileSizeW(string lpFileName, out uint lpFileSizeHigh);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetDiskFreeSpaceW(
        string lpRootPathName,
        out uint lpSectorsPerCluster,
        out uint lpBytesPerSector,
        out uint lpNumberOfFreeClusters,
        out uint lpTotalNumberOfClusters);

    public static uint GetClusterSize(string rootPath)
    {
        if (GetDiskFreeSpaceW(rootPath, out var sectorsPerCluster, out var bytesPerSector, out _, out _))
            return sectorsPerCluster * bytesPerSector;

        return 4096;
    }

    public static long GetPhysicalSize(string filePath, uint clusterSize)
    {
        var low = GetCompressedFileSizeW(filePath, out var high);
        if (low == INVALID_FILE_SIZE && Marshal.GetLastWin32Error() != 0)
            return -1;

        var rawSize = ((long)high << 32) | low;
        return RoundUpToCluster(rawSize, clusterSize);
    }

    public static long RoundUpToCluster(long rawSize, uint clusterSize)
    {
        if (clusterSize == 0) return rawSize;
        return (rawSize + clusterSize - 1) / clusterSize * clusterSize;
    }
}