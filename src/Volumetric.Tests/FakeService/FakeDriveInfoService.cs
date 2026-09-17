using Volumetric.Core.Entities;
using Volumetric.Core.Interfaces;

namespace Volumetric.Tests.FakeService;

public class FakeDriveInfoService : IDriveInfoService
{
    public List<DriveItem> DrivesToReturn { get; set; } = [];

    public IReadOnlyList<DriveItem> GetDrives() => DrivesToReturn;
}