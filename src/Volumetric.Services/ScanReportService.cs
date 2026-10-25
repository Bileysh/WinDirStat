using System.Globalization;
using System.Net;
using System.Text;
using Volumetric.Core.Classification;
using Volumetric.Core.Entities;
using Volumetric.Core.Interfaces;

namespace Volumetric.Services;

public class ScanReportService : IScanReportService
{
    private readonly ILocalizationService _localizationService;

    public ScanReportService(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    public string GenerateReportHtml(ScanResult result)
    {
        var reportTitle = HtmlEncode(_localizationService.GetString(ResourceKeys.ReportTitle));
        var rootPathLabel = HtmlEncode(_localizationService.GetString(ResourceKeys.ReportRootPath));
        var scannedAtLabel = HtmlEncode(_localizationService.GetString(ResourceKeys.ReportScannedAt));
        var totalSizeLabel = HtmlEncode(_localizationService.GetString(ResourceKeys.ReportTotalSize));
        var byCategoryTitle = HtmlEncode(_localizationService.GetString(ResourceKeys.ReportSizeBreakdownByCategory));
        var byExtensionTitle = HtmlEncode(_localizationService.GetString(ResourceKeys.ReportSizeBreakdownByExtension));
        var typeColumn = HtmlEncode(_localizationService.GetString(ResourceKeys.ReportColumnType));
        var sizeColumn = HtmlEncode(_localizationService.GetString(ResourceKeys.ReportColumnSize));
        var filesColumn = HtmlEncode(_localizationService.GetString(ResourceKeys.ReportColumnFiles));
        var percentColumn = HtmlEncode(_localizationService.GetString(ResourceKeys.ReportColumnPercent));
        var noExtensionLabel = HtmlEncode(_localizationService.GetString(ResourceKeys.NoExtensionLabel));

        var html = new StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"en\">");
        html.AppendLine("<head>");
        html.AppendLine("  <meta charset=\"utf-8\" />");
        html.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        html.AppendLine($"  <title>{reportTitle}</title>");
        html.AppendLine("  <style>");
        html.AppendLine("    body { font-family: Segoe UI, Arial, sans-serif; margin: 20px; color: #1f1f1f; }");
        html.AppendLine("    h1, h2 { margin-bottom: 8px; }");
        html.AppendLine("    .meta { margin-bottom: 20px; }");
        html.AppendLine("    .meta div { margin-bottom: 4px; }");
        html.AppendLine("    table { border-collapse: collapse; width: 100%; margin-bottom: 24px; }");
        html.AppendLine("    th, td { border: 1px solid #d0d0d0; padding: 8px; text-align: left; }");
        html.AppendLine("    th { background: #f3f3f3; }");
        html.AppendLine("    td.number { text-align: right; white-space: nowrap; }");
        html.AppendLine("  </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine($"  <h1>{reportTitle}</h1>");
        html.AppendLine("  <div class=\"meta\">");
        html.AppendLine($"    <div><strong>{rootPathLabel}:</strong> {HtmlEncode(result.RootPath)}</div>");
        html.AppendLine(
            $"    <div><strong>{scannedAtLabel}:</strong> {HtmlEncode(result.ScannedAt.ToString("u", CultureInfo.InvariantCulture))}</div>");
        html.AppendLine($"    <div><strong>{totalSizeLabel}:</strong> {HtmlEncode(SizeFormatter.Format(result.TotalSize))}</div>");
        html.AppendLine("  </div>");

        AppendTable(html, byCategoryTitle, result.StatisticsByCategory, typeColumn, sizeColumn, filesColumn, percentColumn, noExtensionLabel);
        AppendTable(html, byExtensionTitle, result.StatisticsByExtension, typeColumn, sizeColumn, filesColumn, percentColumn, noExtensionLabel);

        html.AppendLine("</body>");
        html.AppendLine("</html>");
        return html.ToString();
    }

    private static void AppendTable(
        StringBuilder html,
        string sectionTitle,
        IReadOnlyList<FileTypeStatisticsEntry> entries,
        string typeColumn,
        string sizeColumn,
        string filesColumn,
        string percentColumn,
        string noExtensionLabel)
    {
        html.AppendLine($"  <h2>{sectionTitle}</h2>");
        html.AppendLine("  <table>");
        html.AppendLine("    <thead>");
        html.AppendLine("      <tr>");
        html.AppendLine($"        <th>{typeColumn}</th>");
        html.AppendLine($"        <th>{sizeColumn}</th>");
        html.AppendLine($"        <th>{filesColumn}</th>");
        html.AppendLine($"        <th>{percentColumn}</th>");
        html.AppendLine("      </tr>");
        html.AppendLine("    </thead>");
        html.AppendLine("    <tbody>");

        foreach (var entry in entries)
        {
            var label = string.IsNullOrWhiteSpace(entry.Label)
                ? noExtensionLabel
                : HtmlEncode(entry.Label);
            html.AppendLine("      <tr>");
            html.AppendLine($"        <td>{label}</td>");
            html.AppendLine($"        <td class=\"number\">{HtmlEncode(SizeFormatter.Format(entry.TotalSize))}</td>");
            html.AppendLine($"        <td class=\"number\">{entry.FileCount.ToString(CultureInfo.InvariantCulture)}</td>");
            html.AppendLine($"        <td class=\"number\">{entry.PercentOfTotal.ToString("F2", CultureInfo.InvariantCulture)}%</td>");
            html.AppendLine("      </tr>");
        }

        html.AppendLine("    </tbody>");
        html.AppendLine("  </table>");
    }

    private static string HtmlEncode(string value) => WebUtility.HtmlEncode(value);
}
