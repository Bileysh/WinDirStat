namespace WinDirStat.Core.Entities;

public class FileSystemNode
{
    public required string Name { get; init; }
    public required string FullPath { get; init; }
    public bool IsDirectory { get; init; }
    public string Extension { get; init; } = string.Empty;
    public long SizeLogical { get; set; }
    public long SizePhysical { get; set; }
    public DateTime LastModified { get; init; }
    public List<FileSystemNode> Children { get; } = [];
    public ScanStatus Status { get; set; } = ScanStatus.Ok;
    public string? ErrorMessage { get; set; }
    public bool IsDuplicateHardLink { get; set; }
}