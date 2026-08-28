using WinDirStat.Core.Entities;
using WinDirStat.Services;
using WinDirStat.Tests.FakeService;

namespace WinDirStat.Tests.Tests;

public class DiskScanServiceHardLinkTests
{
    [Fact]
    public async Task ScanAsync_WhenTwoFilesShareIdentity_ZerosPhysicalSizeOnSecondOccurrenceOnly()
    {
        var tempRoot = Directory.CreateTempSubdirectory();
        await File.WriteAllBytesAsync(Path.Combine(tempRoot.FullName, "a.txt"), new byte[100]);
        await File.WriteAllBytesAsync(Path.Combine(tempRoot.FullName, "b.txt"), new byte[100]);
        await File.WriteAllBytesAsync(Path.Combine(tempRoot.FullName, "c.txt"), new byte[50]);

        var sharedIdentity = new FileIdentity(VolumeSerialNumber: 1, FileIndex: 1000, LinkCount: 2);
        var fakeIdentityService = new FakeFileIdentityService
        {
            IdentitiesByPath =
            {
                [Path.Combine(tempRoot.FullName, "a.txt")] = sharedIdentity,
                [Path.Combine(tempRoot.FullName, "b.txt")] = sharedIdentity,
                [Path.Combine(tempRoot.FullName, "c.txt")] = new FileIdentity(1, 2000, LinkCount: 1)
            }
        };

        var service = new DiskScanService(fakeIdentityService);
        var result = await service.ScanAsync(tempRoot.FullName);

        var a = result.RootNode.Children.Single(n => n.Name == "a.txt");
        var b = result.RootNode.Children.Single(n => n.Name == "b.txt");
        var c = result.RootNode.Children.Single(n => n.Name == "c.txt");

        Assert.False(a.IsDuplicateHardLink);
        Assert.True(a.SizePhysical > 0);

        Assert.True(b.IsDuplicateHardLink);
        Assert.Equal(0, b.SizePhysical);

        Assert.False(c.IsDuplicateHardLink);
        Assert.True(c.SizePhysical > 0);

        Assert.Equal(100, a.SizeLogical);
        Assert.Equal(100, b.SizeLogical);
        Assert.Equal(250, result.RootNode.SizeLogical);

        Assert.Equal(a.SizePhysical + c.SizePhysical, result.RootNode.SizePhysical);
    }

    [Fact]
    public async Task ScanAsync_WhenLinkCountIsOne_NeverMarksAsDuplicate_EvenWithColldingIdentity()
    {
        var tempRoot = Directory.CreateTempSubdirectory();
        await File.WriteAllBytesAsync(Path.Combine(tempRoot.FullName, "a.txt"), new byte[100]);
        await File.WriteAllBytesAsync(Path.Combine(tempRoot.FullName, "b.txt"), new byte[100]);
        
        var suspiciousIdentity = new FileIdentity(1, 1000, LinkCount: 1);
        var fakeIdentityService = new FakeFileIdentityService
        {
            IdentitiesByPath =
            {
                [Path.Combine(tempRoot.FullName, "a.txt")] = suspiciousIdentity,
                [Path.Combine(tempRoot.FullName, "b.txt")] = suspiciousIdentity
            }
        };

        var service = new DiskScanService(fakeIdentityService);
        var result = await service.ScanAsync(tempRoot.FullName);

        Assert.All(result.RootNode.Children, n => Assert.False(n.IsDuplicateHardLink));
        Assert.All(result.RootNode.Children, n => Assert.True(n.SizePhysical > 0));
    }

    [Fact]
    public async Task ScanAsync_WhenIdentityLookupFails_TreatsFileAsNonDuplicate()
    {
        var tempRoot = Directory.CreateTempSubdirectory();
        await File.WriteAllBytesAsync(Path.Combine(tempRoot.FullName, "a.txt"), new byte[100]);

        var service = new DiskScanService(new FakeFileIdentityService());
        var result = await service.ScanAsync(tempRoot.FullName);

        var a = Assert.Single(result.RootNode.Children);
        Assert.False(a.IsDuplicateHardLink);
        Assert.True(a.SizePhysical > 0);
    }
}
