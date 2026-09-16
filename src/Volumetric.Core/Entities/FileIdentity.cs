namespace Volumetric.Core.Entities;
public readonly record struct FileIdentity(uint VolumeSerialNumber, ulong FileIndex, uint LinkCount);
