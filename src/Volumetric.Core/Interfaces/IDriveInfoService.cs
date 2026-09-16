using Volumetric.Core.Entities;

namespace Volumetric.Core.Interfaces;

public interface IDriveInfoService
{
    IReadOnlyList<DriveItem> GetDrives();
}