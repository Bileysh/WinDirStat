using Volumetric.Core.Entities;
using Volumetric.ViewModels;

namespace Volumetric.Tests.Tests;

public class NodeViewModelSearchTests
{
    private static FileSystemNode Dir(string name, params FileSystemNode[] children)
    {
        var node = new FileSystemNode { Name = name, IsDirectory = true };
        foreach (var child in children)
        {
            node.AddChild(child);
        }

        return node;
    }

    private static FileSystemNode File(string name) => new() { Name = name };

    [Fact]
    public void ApplySearchFilter_DoesNotMaterializeChildViewModels()
    {
        var root = Dir("root",
            Dir("src", Dir("deep", File("needle.txt"), File("hay.txt")), File("a.cs")),
            Dir("docs", File("readme.md")));
        var vm = new NodeViewModel(root);

        var matched = vm.ApplySearchFilter("needle");

        Assert.True(matched);
        Assert.False(vm.AreChildrenMaterialized);
    }

    [Fact]
    public void ApplySearchFilter_OnlyUpdatesViewModelsThatAlreadyExist()
    {
        var root = Dir("root",
            Dir("src", Dir("deep", File("needle.txt"))),
            Dir("docs", File("readme.md")));
        var vm = new NodeViewModel(root);
        var src = vm.Children[0];

        vm.ApplySearchFilter("needle");

        Assert.True(vm.AreChildrenMaterialized);
        Assert.False(src.AreChildrenMaterialized);
        Assert.True(src.IsSearchMatch);
        Assert.False(vm.Children[1].IsSearchMatch);
    }

    [Fact]
    public void ChildrenCreatedAfterFilter_PickUpTheCurrentFilter()
    {
        var root = Dir("root",
            Dir("src", File("needle.txt"), File("hay.txt")),
            Dir("docs", File("readme.md")));
        var vm = new NodeViewModel(root);

        vm.ApplySearchFilter("needle");
        var srcChildren = vm.Children[0].Children;

        Assert.True(srcChildren.Single(c => c.Name == "needle.txt").IsSearchMatch);
        Assert.False(srcChildren.Single(c => c.Name == "hay.txt").IsSearchMatch);
    }

    [Fact]
    public void ApplySearchFilter_KeepsAncestorsOfMatchesVisible_AndIsCaseInsensitive()
    {
        var root = Dir("root", Dir("src", Dir("deep", File("Needle.TXT"))), File("other.txt"));
        var vm = new NodeViewModel(root);
        var src = vm.Children[0];
        var deep = src.Children[0];
        var other = vm.Children[1];

        var matched = vm.ApplySearchFilter("needle");

        Assert.True(matched);
        Assert.True(vm.IsSearchMatch);
        Assert.True(src.IsSearchMatch);
        Assert.True(deep.IsSearchMatch);
        Assert.False(other.IsSearchMatch);
    }

    [Fact]
    public void ApplySearchFilter_NoMatches_HidesEverything()
    {
        var root = Dir("root", File("a.txt"));
        var vm = new NodeViewModel(root);
        var child = vm.Children[0];

        var matched = vm.ApplySearchFilter("zzz");

        Assert.False(matched);
        Assert.False(vm.IsSearchMatch);
        Assert.False(child.IsSearchMatch);
    }

    [Fact]
    public void ApplySearchFilter_ClearedQuery_ShowsEverythingAgain()
    {
        var root = Dir("root", Dir("src", File("needle.txt")), File("other.txt"));
        var vm = new NodeViewModel(root);
        var other = vm.Children[1];
        vm.ApplySearchFilter("needle");

        var matched = vm.ApplySearchFilter("  ");

        Assert.True(matched);
        Assert.True(other.IsSearchMatch);
        Assert.True(vm.Children[0].Children[0].IsSearchMatch);
    }

    [Fact]
    public void ApplySearchFilter_ClearedQuery_DoesNotMaterializeChildViewModels()
    {
        var root = Dir("root", Dir("src", File("needle.txt")));
        var vm = new NodeViewModel(root);

        vm.ApplySearchFilter("needle");
        vm.ApplySearchFilter(null);

        Assert.False(vm.AreChildrenMaterialized);
    }
}
