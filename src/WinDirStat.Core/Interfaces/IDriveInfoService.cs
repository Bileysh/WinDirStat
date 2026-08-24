using WinDirStat.Core.Entities;

namespace WinDirStat.Core.Interfaces;

public interface IDriveInfoService
{
    IReadOnlyList<DriveItem> GetDrives();
}