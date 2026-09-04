using System.Text.Json.Serialization;

namespace WinDirStat.Core.Entities;

public class FileSystemNode
{
    public required string Name { get; init; }

    [JsonIgnore] public FileSystemNode? Parent { get; set; }

    public string? RootFullPathOverride { get; set; }

    [JsonIgnore]
    public string FullPath => Parent is not null
        ? Path.Combine(Parent.FullPath, Name)
        : RootFullPathOverride ?? Name;

    public bool IsDirectory { get; init; }
    public string Extension { get; init; } = string.Empty;
    public long SizeLogical { get; set; }
    public long SizePhysical { get; set; }
    public DateTime LastModified { get; init; }
    public List<FileSystemNode> Children { get; } = [];
    public ScanStatus Status { get; set; } = ScanStatus.Ok;
    public string? ErrorMessage { get; set; }
    public bool IsDuplicateHardLink { get; set; }

    public void AddChild(FileSystemNode child)
    {
        child.Parent = this;
        Children.Add(child);
    }
}