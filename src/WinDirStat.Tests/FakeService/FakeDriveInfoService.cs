using WinDirStat.Core.Entities;
using WinDirStat.Core.Interfaces;

namespace WinDirStat.Tests.FakeService;

public class FakeDriveInfoService : IDriveInfoService
{
    public List<DriveItem> DrivesToReturn { get; set; } = [];

    public IReadOnlyList<DriveItem> GetDrives() => DrivesToReturn;
}