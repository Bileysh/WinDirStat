using Volumetric.Core.Entities;
using Volumetric.Core.Interfaces;

namespace Volumetric.Tests.FakeService;

public class FakeScanReportService : IScanReportService
{
    public string ReportHtmlToReturn { get; set; } = "<html></html>";
    public ScanResult? LastResult { get; private set; }

    public string GenerateReportHtml(ScanResult result)
    {
        LastResult = result;
        return ReportHtmlToReturn;
    }
}
