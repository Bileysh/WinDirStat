using WinDirStat.Core.Classification;
using WinDirStat.Core.Entities;

namespace WinDirStat.Tests.Tests;

public class FileStatisticsAggregatorTests
{
    [Fact]
    public void ByExtension_GroupsFilesByExtensionCorrectly()
    {
        var root = new FileSystemNode
        {
            Name = "root", IsDirectory = true,
            Children =
            {
                new FileSystemNode { Name = "a.mp4", Extension = ".mp4", SizeLogical = 100 },
                new FileSystemNode { Name = "b.mp4", Extension = ".mp4", SizeLogical = 200 },
                new FileSystemNode { Name = "c.txt", Extension = ".txt", SizeLogical = 50 }
            }
        };

        var result = FileStatisticsAggregator.ByExtension(root);

        Assert.Equal(2, result.Count);
        var mp4 = result.Single(s => s.Label == ".mp4");
        Assert.Equal(300, mp4.TotalSize);
        Assert.Equal(2, mp4.FileCount);
    }

    [Fact]
    public void ByCategory_GroupsFilesByCategoryCorrectly()
    {
        var root = new FileSystemNode
        {
            Name = "root", IsDirectory = true,
            Children =
            {
                new FileSystemNode { Name = "a.mp4", Extension = ".mp4", SizeLogical = 100 },
                new FileSystemNode { Name = "b.mkv", Extension = ".mkv", SizeLogical = 200 }
            }
        };

        var result = FileStatisticsAggregator.ByCategory(root);

        Assert.Single(result);
        Assert.Equal("Videos", result[0].Label);
        Assert.Equal(300, result[0].TotalSize);
    }
}