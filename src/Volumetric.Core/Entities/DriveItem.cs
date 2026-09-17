namespace Volumetric.Core.Entities;

public record DriveItem(
    string RootPath,
    string VolumeLabel,
    long TotalBytes,
    long FreeBytes);