using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Volumetric.Core.Entities;

public class FileSystemNode
{
    private static readonly FileSystemNode[] NoChildren = [];

    private List<FileSystemNode>? _children;

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
    public DateTime LastModified { get; set; }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Replace)]
    public IReadOnlyList<FileSystemNode> Children
    {
        get => _children is null ? NoChildren : _children;
        set => _children = value.Count == 0 ? null : [.. value];
    }

    public ScanStatus Status { get; set; } = ScanStatus.Ok;
    public string? ErrorMessage { get; set; }
    public bool IsDuplicateHardLink { get; set; }

    public void AddChild(FileSystemNode child)
    {
        child.Parent = this;
        (_children ??= []).Add(child);
    }

    public void ReplaceChildren(IEnumerable<FileSystemNode> children)
    {
        _children = null;
        foreach (var child in children)
        {
            AddChild(child);
        }
    }

    public void EstablishParentLinksRecursively()
    {
        foreach (var child in Children)
        {
            child.Parent = this;
            child.EstablishParentLinksRecursively();
        }
    }
}
