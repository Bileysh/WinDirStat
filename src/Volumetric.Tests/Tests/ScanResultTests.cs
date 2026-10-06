using Volumetric.Core.Entities;

namespace Volumetric.Tests.Tests;

public class ScanResultTests
{
    [Fact]
    public void Counts_IncludeTheRootFolderAndEveryNestedNode()
    {
        var root = new FileSystemNode { Name = "root", IsDirectory = true };
        var docs = new FileSystemNode { Name = "docs", IsDirectory = true };
        var empty = new FileSystemNode { Name = "empty", IsDirectory = true };
        root.AddChild(docs);
        root.AddChild(empty);
        root.AddChild(new FileSystemNode { Name = "a.txt" });
        docs.AddChild(new FileSystemNode { Name = "b.txt" });
        docs.AddChild(new FileSystemNode { Name = "c.txt" });

        var result = new ScanResult("root", root, [], [], TimeSpan.Zero);

        Assert.Equal(3, result.FileCount);
        Assert.Equal(3, result.FolderCount);
    }

    [Fact]
    public void Counts_VeryDeepTree_DoNotOverflowTheStack()
    {
        const int levels = 16_000;
        var root = new FileSystemNode { Name = "root", IsDirectory = true };
        var current = root;
        for (var i = 0; i < levels; i++)
        {
            var next = new FileSystemNode { Name = $"d{i}", IsDirectory = true };
            current.AddChild(next);
            current = next;
        }

        current.AddChild(new FileSystemNode { Name = "leaf.txt" });

        var result = new ScanResult("root", root, [], [], TimeSpan.Zero);

        Assert.Equal(1, result.FileCount);
        Assert.Equal(levels + 1, result.FolderCount);
    }
}
