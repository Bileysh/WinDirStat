using WinDirStat.Core.Entities;
using WinDirStat.Core.Interfaces;

namespace WinDirStat.Services;

public class DriveInfoService : IDriveInfoService
{
    public IReadOnlyList<DriveItem> GetDrives()
    {
        var result = new List<DriveItem>();

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType is not (DriveType.Fixed or DriveType.Removable or DriveType.Network))
                continue;

            if (!drive.IsReady) continue;

            try
            {
                var totalBytes = drive.TotalSize;
                var freeBytes = drive.AvailableFreeSpace;
                var label = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? string.Empty : drive.VolumeLabel;

                result.Add(new DriveItem(drive.RootDirectory.FullName, label, totalBytes, freeBytes));
            }
            catch (IOException)
            {
            }
        }

        return result;
    }
}