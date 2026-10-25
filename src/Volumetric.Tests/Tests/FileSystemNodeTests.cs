using Volumetric.Core.Entities;

namespace Volumetric.Tests.Tests;

public class FileSystemNodeTests
{
    [Fact]
    public void Children_OfLeafNodes_AreEmptyAndShareOneInstance()
    {
        var first = new FileSystemNode { Name = "a.txt" };
        var second = new FileSystemNode { Name = "b.txt" };

        Assert.Empty(first.Children);
        Assert.Same(first.Children, second.Children);
    }

    [Fact]
    public void AddChild_GivesTheNodeItsOwnListAndSetsParent()
    {
        var folder = new FileSystemNode { Name = "folder", IsDirectory = true };
        var leaf = new FileSystemNode { Name = "leaf.txt" };
        var child = new FileSystemNode { Name = "child.txt" };

        folder.AddChild(child);

        Assert.Same(folder, child.Parent);
        Assert.Same(child, Assert.Single(folder.Children));
        Assert.Empty(leaf.Children);
    }

    [Fact]
    public void ReplaceChildren_ReplacesExistingChildrenAndReparentsNewOnes()
    {
        var folder = new FileSystemNode { Name = "folder", IsDirectory = true };
        folder.AddChild(new FileSystemNode { Name = "old.txt" });
        var donor = new FileSystemNode { Name = "donor", IsDirectory = true };
        donor.AddChild(new FileSystemNode { Name = "new-1.txt" });
        donor.AddChild(new FileSystemNode { Name = "new-2.txt" });

        folder.ReplaceChildren(donor.Children);

        Assert.Equal(["new-1.txt", "new-2.txt"], folder.Children.Select(c => c.Name));
        Assert.All(folder.Children, c => Assert.Same(folder, c.Parent));
    }

    [Fact]
    public void ReplaceChildren_WithNothing_LeavesNodeWithoutOwnList()
    {
        var folder = new FileSystemNode { Name = "folder", IsDirectory = true };
        folder.AddChild(new FileSystemNode { Name = "old.txt" });

        folder.ReplaceChildren([]);

        Assert.Same(new FileSystemNode { Name = "other" }.Children, folder.Children);
    }

    [Fact]
    public void ChildrenSetter_CopiesTheGivenItems()
    {
        var source = new List<FileSystemNode> { new() { Name = "a.txt" } };
        var folder = new FileSystemNode { Name = "folder", IsDirectory = true, Children = source };

        source.Add(new FileSystemNode { Name = "b.txt" });

        Assert.Single(folder.Children);
    }
}
