using System.Globalization;
using System.Net;
using System.Text;
using Volumetric.Core.Classification;
using Volumetric.Core.Entities;
using Volumetric.Core.Interfaces;
using Volumetric.Core.Reports;

namespace Volumetric_App.Services;

public sealed class ScanReportHtmlBuilder
{
    private const int ChartWidth = 720;
    private const int BarHeight = 28;
    private const int BarGap = 8;
    private const int LabelWidth = 160;
    private const int ValueLabelWidth = 90;

    private static readonly string PageTemplate = LoadTemplate("ScanReportPage.html");
    private static readonly string StyleSheet = LoadTemplate("ScanReport.css");
    private static readonly string RowTemplate = LoadTemplate("ScanReportRow.html");
    private static readonly string ChartTemplate = LoadTemplate("ScanReportChart.html");
    private static readonly string ChartBarTemplate = LoadTemplate("ScanReportChartBar.html");

    private readonly ILocalizationService _localizationService;

    public ScanReportHtmlBuilder(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    public string Build(FileSystemNode root)
    {
        var byCategory = FileStatisticsAggregator.ByCategory(root);

        return HtmlTemplate.Render(PageTemplate,
            ("language", Html(_localizationService.CurrentLanguage)),
            ("title", Html(Localize(ResourceKeys.ReportTitle))),
            ("style", StyleSheet),
            ("heading", Html(root.Name)),
            ("chart", BuildCategoryChart(byCategory)),
            ("tableTitle", Html(Localize(ResourceKeys.ReportAllFilesAndFolders))),
            ("pathColumn", Html(Localize(ResourceKeys.ReportColumnPath))),
            ("typeColumn", Html(Localize(ResourceKeys.ReportColumnType))),
            ("sizeColumn", Html(Localize(ResourceKeys.ReportColumnSize))),
            ("modifiedColumn", Html(Localize(ResourceKeys.ReportColumnModified))),
            ("rows", BuildRows(root)));
    }

    private string BuildCategoryChart(List<FileTypeStatisticsEntry> entries)
    {
        if (entries.Count == 0)
        {
            return string.Empty;
        }

        var ordered = entries.OrderByDescending(e => e.TotalSize).ToList();
        var maxSize = ordered[0].TotalSize;
        var chartHeight = ordered.Count * (BarHeight + BarGap);
        var barAreaWidth = ChartWidth - LabelWidth - ValueLabelWidth;

        var bars = new StringBuilder();
        for (var i = 0; i < ordered.Count; i++)
        {
            var entry = ordered[i];
            var y = i * (BarHeight + BarGap);
            var barWidth = maxSize > 0 ? Math.Max(2, entry.TotalSize / (double)maxSize * barAreaWidth) : 0;
            var percent = entry.PercentOfTotal.ToString("F1", CultureInfo.InvariantCulture);

            bars.Append(HtmlTemplate.Render(ChartBarTemplate,
                ("textY", Number(y + BarHeight / 2 + 4)),
                ("label", Html(entry.Label)),
                ("barX", Number(LabelWidth)),
                ("y", Number(y)),
                ("barWidth", Decimal(barWidth)),
                ("barHeight", Number(BarHeight)),
                ("color", ColorFor(entry.Category)),
                ("valueX", Decimal(LabelWidth + barWidth + 8)),
                ("value", Html($"{SizeFormatter.Format(entry.TotalSize)} ({percent}%)"))));
        }

        return HtmlTemplate.Render(ChartTemplate,
            ("title", Html(Localize(ResourceKeys.ReportSizeBreakdownByCategory))),
            ("width", Number(ChartWidth)),
            ("height", Number(chartHeight)),
            ("bars", bars.ToString()));
    }

    private string BuildRows(FileSystemNode root)
    {
        var folderLabel = Html(Localize(ResourceKeys.ReportKindFolder));
        var fileLabel = Html(Localize(ResourceKeys.ReportKindFile));
        var rows = new StringBuilder();
        AppendRows(rows, root, root.Name, folderLabel, fileLabel);
        return rows.ToString();
    }

    private static void AppendRows(StringBuilder rows, FileSystemNode node, string path, string folderLabel,
        string fileLabel)
    {
        rows.Append(HtmlTemplate.Render(RowTemplate,
            ("path", Html(path)),
            ("kind", node.IsDirectory ? folderLabel : fileLabel),
            ("size", node.SizeLogical.ToString("N0", CultureInfo.CurrentCulture)),
            ("modified", Html(node.LastModified.ToString("g", CultureInfo.CurrentCulture)))));

        foreach (var child in node.Children)
        {
            AppendRows(rows, child, path + "\\" + child.Name, folderLabel, fileLabel);
        }
    }

    private static string ColorFor(FileCategory category) => category switch
    {
        FileCategory.Documents => "cornflowerblue",
        FileCategory.Videos => "orangered",
        FileCategory.Audio => "mediumpurple",
        FileCategory.Images => "seagreen",
        FileCategory.Archives => "goldenrod",
        FileCategory.Executables => "crimson",
        FileCategory.Development => "lightseagreen",
        FileCategory.VirtualDisks => "slategray",
        FileCategory.System => "dimgray",
        FileCategory.Folder => "steelblue",
        _ => "gray"
    };

    private string Localize(string key) => _localizationService.GetString(key);

    // SVG attributes must use '.' as the decimal separator regardless of the UI culture.
    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Decimal(double value) => value.ToString("F1", CultureInfo.InvariantCulture);

    private static string Html(string value) => WebUtility.HtmlEncode(value);

    private static string LoadTemplate(string fileName) =>
        HtmlTemplate.Load(typeof(ScanReportHtmlBuilder).Assembly, fileName);
}
