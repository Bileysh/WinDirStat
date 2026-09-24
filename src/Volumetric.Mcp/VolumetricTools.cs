using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Volumetric.Core.Classification;
using Volumetric.Core.Entities;

namespace Volumetric.Mcp;

[McpServerToolType]
public sealed class VolumetricTools
{
    private const int DefaultTopN = 20;
    private const int MaxTopN = 200;

    private readonly ScanJobManager _jobs;

    public VolumetricTools(ScanJobManager jobs)
    {
        _jobs = jobs;
    }

    [McpServerTool(Name = "scan", ReadOnly = true, Destructive = false)]
    [Description("Starts a background disk-usage scan of a folder or drive and returns a scanId immediately. " +
                 "Poll get_status with the scanId, then call get_results once the state is Completed.")]
    public ScanStartedInfo Scan(
        [Description("Absolute path of the folder or drive to scan, e.g. C:\\Users or D:\\.")] string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
            throw new McpException("'path' must be an absolute path such as C:\\Users.");

        var fullPath = Path.GetFullPath(path);
        if (!Directory.Exists(fullPath))
            throw new McpException($"Directory not found: {fullPath}");

        var job = _jobs.Start(fullPath);
        return new ScanStartedInfo(job.Id, job.RootPath, job.State.ToString());
    }

    [McpServerTool(Name = "get_status", ReadOnly = true, Idempotent = true)]
    [Description("Returns the state (Running, Completed, Failed, Cancelled) and live progress of a scan started with scan.")]
    public ScanStatusInfo GetStatus([Description("The scanId returned by scan.")] string scanId)
    {
        var job = FindJob(scanId);
        var progress = job.LatestProgress;
        return new ScanStatusInfo(
            job.Id,
            job.RootPath,
            job.State.ToString(),
            Math.Round(job.Elapsed.TotalSeconds, 1),
            progress?.FilesScanned ?? 0,
            progress?.FoldersScanned ?? 0,
            progress?.CurrentPath,
            job.Error);
    }

    [McpServerTool(Name = "get_results", ReadOnly = true, Idempotent = true)]
    [Description("Returns the summary of a completed scan: total size, breakdown by category and extension, " +
                 "the largest items directly under the scanned root, and the largest individual files.")]
    public ScanResultsInfo GetResults(
        [Description("The scanId returned by scan.")] string scanId,
        [Description("How many entries to return per list (1-200, default 20).")] int topN = DefaultTopN)
    {
        var job = FindJob(scanId);
        var result = job.Result;
        if (job.State != ScanJobState.Completed || result is null)
            throw new McpException(
                $"Scan {job.Id} is {job.State}" + (job.Error is null ? "; results are not available yet." : $": {job.Error}"));

        var limit = Math.Clamp(topN, 1, MaxTopN);
        var root = result.RootNode;

        return new ScanResultsInfo(
            job.Id,
            result.RootPath,
            result.TotalSize,
            SizeFormatter.Format(result.TotalSize),
            Math.Round(job.Elapsed.TotalSeconds, 1),
            result.StatisticsByCategory.Take(limit).Select(ToTypeStats).ToList(),
            result.StatisticsByExtension.Take(limit).Select(ToTypeStats).ToList(),
            root.Children.OrderByDescending(c => c.SizeLogical).Take(limit).Select(ToSizeEntry).ToList(),
            LargestFiles(root, limit).Select(ToSizeEntry).ToList());
    }

    private ScanJob FindJob(string scanId) =>
        _jobs.Find(scanId) ?? throw new McpException($"Unknown scanId \"{scanId}\". Start a scan with the scan tool first.");

    private static TypeStatsEntry ToTypeStats(FileTypeStatisticsEntry e) =>
        new(string.IsNullOrEmpty(e.Label) ? "(no extension)" : e.Label, e.TotalSize, SizeFormatter.Format(e.TotalSize),
            e.FileCount, Math.Round(e.PercentOfTotal, 2));

    private static SizeEntry ToSizeEntry(FileSystemNode n) =>
        new(n.FullPath, n.IsDirectory, n.SizeLogical, SizeFormatter.Format(n.SizeLogical));

    private static List<FileSystemNode> LargestFiles(FileSystemNode root, int limit)
    {
        var files = new PriorityQueue<FileSystemNode, long>();
        var stack = new Stack<FileSystemNode>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (node.IsDirectory)
            {
                foreach (var child in node.Children) stack.Push(child);
                continue;
            }

            files.Enqueue(node, node.SizeLogical);
            if (files.Count > limit) files.Dequeue();
        }

        var top = new List<FileSystemNode>(files.Count);
        while (files.Count > 0) top.Add(files.Dequeue());
        top.Reverse();
        return top;
    }
}
