using System.Globalization;
using System.Net;
using System.Text;
using Volumetric.Core.Classification;
using Volumetric.Core.Entities;

namespace Volumetric_App.Services;

public static class ScanReportHtmlBuilder
{
    public static string Build(FileSystemNode root)
    {
        var builder = new StringBuilder("""
            <!doctype html><html><head><meta charset="utf-8">
            <title>Volumetric scan report</title>
            <style>body{font:14px Segoe UI, sans-serif;margin:24px;color:#222}
            table{border-collapse:collapse;width:100%}th,td{padding:6px 8px;border-bottom:1px solid #ddd;text-align:left}
            th{background:#f3f3f3}h1{font-size:22px}h2{font-size:16px;margin-top:32px}
            .legend{display:flex;flex-wrap:wrap;gap:12px;margin:12px 0;font-size:13px}
            .legend span{display:inline-flex;align-items:center;gap:6px}
            .swatch{width:10px;height:10px;border-radius:2px;display:inline-block}</style></head><body>
            """);
        builder.Append("<h1>").Append(Html(root.Name)).Append("</h1>");

        var byCategory = FileStatisticsAggregator.ByCategory(root);
        AppendCategoryChart(builder, byCategory);

        builder.Append("<h2>All files and folders</h2>");
        builder.Append("<table><thead><tr><th>Path</th><th>Type</th><th>Size</th><th>Modified</th></tr></thead><tbody>");
        AppendNode(builder, root, root.Name);
        builder.Append("</tbody></table></body></html>");
        return builder.ToString();
    }

    private static void AppendCategoryChart(StringBuilder builder, List<FileTypeStatisticsEntry> entries)
    {
        if (entries.Count == 0) return;

        const int chartWidth = 720;
        const int barHeight = 28;
        const int barGap = 8;
        const int labelWidth = 160;
        var ordered = entries.OrderByDescending(e => e.TotalSize).ToList();
        var maxSize = ordered[0].TotalSize;
        var chartHeight = ordered.Count * (barHeight + barGap);
        var barAreaWidth = chartWidth - labelWidth - 90; // leaves room for the size label on the right

        builder.Append("<h2>Size by category</h2>");
        builder.Append($"""<svg viewBox="0 0 {chartWidth} {chartHeight}" width="{chartWidth}" height="{chartHeight}" role="img">""");

        for (var i = 0; i < ordered.Count; i++)
        {
            var entry = ordered[i];
            var y = i * (barHeight + barGap);
            var barWidth = maxSize > 0 ? Math.Max(2, entry.TotalSize / (double)maxSize * barAreaWidth) : 0;
            var color = ColorFor(entry.Category);

            builder.Append($"""<text x="0" y="{y + barHeight / 2 + 4}" font-size="12">{Html(entry.Label)}</text>""");
            builder.Append(
                $"""<rect x="{labelWidth}" y="{y}" width="{barWidth.ToString("F1", CultureInfo.InvariantCulture)}" height="{barHeight}" rx="3" fill="{color}"/>""");
            var textX = (labelWidth + barWidth + 8).ToString("F1", CultureInfo.InvariantCulture);
            builder.Append(
                $"""<text x="{textX}" y="{y + barHeight / 2 + 4}" font-size="12">{FormatBytes(entry.TotalSize)} ({entry.PercentOfTotal:F1}%)</text>""");
        }

        builder.Append("</svg>");
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:F1} {units[unit]}";
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

    private static void AppendNode(StringBuilder builder, FileSystemNode node, string path)
    {
        builder.Append("<tr><td>").Append(Html(path)).Append("</td><td>")
            .Append(node.IsDirectory ? "Folder" : "File").Append("</td><td>")
            .Append(node.SizeLogical.ToString("N0")).Append("</td><td>")
            .Append(node.LastModified.ToString("g")).Append("</td></tr>");
        foreach (var child in node.Children)
        {
            AppendNode(builder, child, path + "\\" + child.Name);
        }
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
