using ModelContextProtocol;
using Volumetric.Mcp;
using Volumetric.Tests.FakeService;

namespace Volumetric.Tests.Tests;

public class VolumetricToolsTests
{
    private readonly FakeDiskScanService _scanService = new();
    private readonly VolumetricTools _tools;

    public VolumetricToolsTests()
    {
        _tools = new VolumetricTools(new ScanJobManager(_scanService));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Users")]
    [InlineData(@"relative\folder")]
    [InlineData(@"..\parent")]
    [InlineData("C:drive-relative")]
    [InlineData(@"\no-drive")]
    public void Scan_NonAbsolutePath_ThrowsMcpException(string path)
    {
        var ex = Assert.Throws<McpException>(() => _tools.Scan(path));

        Assert.Contains("absolute path", ex.Message);
        Assert.Equal(0, _scanService.ScanCount);
    }

    [Fact]
    public void Scan_AbsolutePathToMissingDirectory_ThrowsMcpException()
    {
        var missing = Path.Combine(Path.GetTempPath(), "volumetric-missing-" + Guid.NewGuid().ToString("N"));

        var ex = Assert.Throws<McpException>(() => _tools.Scan(missing));

        Assert.Contains("Directory not found", ex.Message);
        Assert.Equal(0, _scanService.ScanCount);
    }

    [Fact]
    public void Scan_AbsolutePathToFile_ThrowsMcpException()
    {
        var file = Path.GetTempFileName();
        try
        {
            var ex = Assert.Throws<McpException>(() => _tools.Scan(file));

            Assert.Contains("Directory not found", ex.Message);
            Assert.Equal(0, _scanService.ScanCount);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-scan-id")]
    [InlineData("00000000000000000000000000000000")]
    public void GetStatus_UnknownScanId_ThrowsMcpException(string scanId)
    {
        var ex = Assert.Throws<McpException>(() => _tools.GetStatus(scanId));

        Assert.Contains("Unknown scanId", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-scan-id")]
    [InlineData("00000000000000000000000000000000")]
    public void GetResults_UnknownScanId_ThrowsMcpException(string scanId)
    {
        var ex = Assert.Throws<McpException>(() => _tools.GetResults(scanId));

        Assert.Contains("Unknown scanId", ex.Message);
    }
}
