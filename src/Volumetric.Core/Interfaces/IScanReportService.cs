using Volumetric.Core.Entities;

namespace Volumetric.Core.Interfaces;

public interface IScanReportService
{
    string GenerateReportHtml(ScanResult result);
}
