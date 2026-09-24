using System.Globalization;
using System.Net;
using System.Text;
using Volumetric.Core.Classification;
using Volumetric.Core.Entities;
using Volumetric.Core.Interfaces;
using Volumetric.Core.Reports;

namespace Volumetric.Services;

public class ScanReportService : IScanReportService
{
    private static readonly string PageTemplate = LoadTemplate("ScanReportPage.html");
    private static readonly string StyleSheet = LoadTemplate("ScanReport.css");
    private static readonly string TableTemplate = LoadTemplate("ScanReportTable.html");
    private static readonly string TableRowTemplate = LoadTemplate("ScanReportTableRow.html");

    private readonly ILocalizationService _localizationService;

    public ScanReportService(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    public string GenerateReportHtml(ScanResult result)
    {
        var tables = new StringBuilder();
        AppendTable(tables, ResourceKeys.ReportSizeBreakdownByCategory, result.StatisticsByCategory);
        AppendTable(tables, ResourceKeys.ReportSizeBreakdownByExtension, result.StatisticsByExtension);

        return HtmlTemplate.Render(PageTemplate,
            ("language", HtmlEncode(_localizationService.CurrentLanguage)),
            ("title", Localize(ResourceKeys.ReportTitle)),
            ("style", StyleSheet),
            ("rootPathLabel", Localize(ResourceKeys.ReportRootPath)),
            ("rootPath", HtmlEncode(result.RootPath)),
            ("scannedAtLabel", Localize(ResourceKeys.ReportScannedAt)),
            ("scannedAt", HtmlEncode(result.ScannedAt.ToString("u", CultureInfo.InvariantCulture))),
            ("totalSizeLabel", Localize(ResourceKeys.ReportTotalSize)),
            ("totalSize", HtmlEncode(SizeFormatter.Format(result.TotalSize))),
            ("tables", tables.ToString()));
    }

    private void AppendTable(StringBuilder html, string sectionTitleKey, IReadOnlyList<FileTypeStatisticsEntry> entries)
    {
        var noExtensionLabel = Localize(ResourceKeys.NoExtensionLabel);

        var rows = new StringBuilder();
        foreach (var entry in entries)
        {
            var label = string.IsNullOrWhiteSpace(entry.Label) ? noExtensionLabel : HtmlEncode(entry.Label);
            rows.Append(HtmlTemplate.Render(TableRowTemplate,
                ("label", label),
                ("size", HtmlEncode(SizeFormatter.Format(entry.TotalSize))),
                ("files", entry.FileCount.ToString(CultureInfo.InvariantCulture)),
                ("percent", entry.PercentOfTotal.ToString("F2", CultureInfo.InvariantCulture))));
        }

        html.Append(HtmlTemplate.Render(TableTemplate,
            ("sectionTitle", Localize(sectionTitleKey)),
            ("typeColumn", Localize(ResourceKeys.ReportColumnType)),
            ("sizeColumn", Localize(ResourceKeys.ReportColumnSize)),
            ("filesColumn", Localize(ResourceKeys.ReportColumnFiles)),
            ("percentColumn", Localize(ResourceKeys.ReportColumnPercent)),
            ("rows", rows.ToString())));
    }

    // Returns HTML-encoded text so it can be placed straight into a template.
    private string Localize(string key) => HtmlEncode(_localizationService.GetString(key));

    private static string HtmlEncode(string value) => WebUtility.HtmlEncode(value);

    private static string LoadTemplate(string fileName) =>
        HtmlTemplate.Load(typeof(ScanReportService).Assembly, fileName);
}
