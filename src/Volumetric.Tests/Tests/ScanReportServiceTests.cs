using Volumetric.Core.Entities;
using Volumetric.Services;
using Volumetric.Tests.FakeService;

namespace Volumetric.Tests.Tests;

public class ScanReportServiceTests
{
    [Fact]
    public void GenerateReportHtml_EscapesLocalDataAndBuildsSizeTables()
    {
        var service = new ScanReportService(new FakeLocalizationService());
        var rootNode = new FileSystemNode
        {
            Name = "C:\\<unsafe>",
            IsDirectory = true,
            SizeLogical = 1536
        };

        var result = new ScanResult(
            "C:\\<unsafe>&\"root\"",
            rootNode,
            [
                new FileTypeStatisticsEntry
                {
                    Label = "<script>alert(1)</script>",
                    Category = FileCategory.Other,
                    FileCount = 2,
                    TotalSize = 1024,
                    PercentOfTotal = 66.67
                }
            ],
            [
                new FileTypeStatisticsEntry
                {
                    Label = ".txt&",
                    Category = FileCategory.Documents,
                    FileCount = 1,
                    TotalSize = 512,
                    PercentOfTotal = 33.33
                }
            ],
            TimeSpan.FromSeconds(1));

        var html = service.GenerateReportHtml(result);

        Assert.Contains("ReportSizeBreakdownByCategory", html);
        Assert.Contains("ReportSizeBreakdownByExtension", html);
        Assert.Contains("C:\\&lt;unsafe&gt;&amp;&quot;root&quot;", html);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html);
        Assert.Contains(".txt&amp;", html);
        Assert.DoesNotContain("<script>alert(1)</script>", html);
        Assert.DoesNotContain("http://", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("https://", html, StringComparison.OrdinalIgnoreCase);
    }
}
