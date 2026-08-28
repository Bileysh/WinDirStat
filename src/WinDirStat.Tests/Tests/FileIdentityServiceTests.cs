using WinDirStat.Services;

namespace WinDirStat.Tests.Tests;

public class FileIdentityServiceTests
{
    [Fact]
    public void GetIdentity_ForExistingFile_ReturnsStableIdentity()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.WriteAllText(tempFile, "windirstat");

        try
        {
            var service = new FileIdentityService();

            var first = service.GetIdentity(tempFile);
            var second = service.GetIdentity(tempFile);

            Assert.NotNull(first);
            Assert.Equal(first, second);
            Assert.Equal(1u, first!.Value.LinkCount);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void GetIdentity_ForMissingFile_ReturnsNull()
    {
        var service = new FileIdentityService();
        var missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        Assert.Null(service.GetIdentity(missingPath));
    }
}
