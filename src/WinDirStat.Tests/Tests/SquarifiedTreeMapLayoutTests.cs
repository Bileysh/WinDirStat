using WinDirStat.Core.Classification;
using WinDirStat.Core.Entities;

namespace WinDirStat.Tests.Tests;

public class SquarifiedTreeMapLayoutTests
{
    private static FileSystemNode BuildRoot() => new()
    {
        Name = "root", IsDirectory = true,
        Children =
        {
            new FileSystemNode { Name = "a", SizeLogical = 500 },
            new FileSystemNode { Name = "b", SizeLogical = 300 },
            new FileSystemNode { Name = "c", SizeLogical = 200 }
        }
    };

    [Fact]
    public void Compute_ReturnsOneRectPerChild()
    {
        var rects = SquarifiedTreeMapLayout.Compute(BuildRoot(), 0, 0, 100, 50);
        Assert.Equal(3, rects.Count);
    }

    [Fact]
    public void Compute_AllRectsFitWithinContainerBounds()
    {
        var rects = SquarifiedTreeMapLayout.Compute(BuildRoot(), 0, 0, 100, 50);

        Assert.All(rects, r =>
        {
            Assert.True(r.X >= -0.01 && r.X + r.Width <= 100.01);
            Assert.True(r.Y >= -0.01 && r.Y + r.Height <= 50.01);
        });
    }

    [Fact]
    public void Compute_TotalAreaMatchesContainerArea()
    {
        var rects = SquarifiedTreeMapLayout.Compute(BuildRoot(), 0, 0, 100, 50);
        var totalArea = rects.Sum(r => r.Width * r.Height);

        Assert.Equal(100 * 50, totalArea, precision: 1);
    }

    [Fact]
    public void Compute_ReturnsEmptyList_WhenNoChildren()
    {
        var root = new FileSystemNode { Name = "root", IsDirectory = true };
        Assert.Empty(SquarifiedTreeMapLayout.Compute(root, 0, 0, 100, 50));
    }
}