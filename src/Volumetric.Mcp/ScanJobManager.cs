using System.Collections.Concurrent;
using Volumetric.Core.Interfaces;

namespace Volumetric.Mcp;

public sealed class ScanJobManager
{
    private const int MaxRetainedJobs = 16;

    private readonly IDiskScanService _scanService;
    private readonly ConcurrentDictionary<string, ScanJob> _jobs = new();

    public ScanJobManager(IDiskScanService scanService)
    {
        _scanService = scanService;
    }

    public ScanJob Start(string rootPath)
    {
        PruneFinishedJobs();

        var job = new ScanJob(rootPath);
        _jobs[job.Id] = job;
        _ = Task.Run(() => RunAsync(job));
        return job;
    }

    public ScanJob? Find(string scanId) => _jobs.GetValueOrDefault(scanId);

    private async Task RunAsync(ScanJob job)
    {
        try
        {
            var result = await _scanService.ScanAsync(job.RootPath, job.Cancellation.Token, progress: job);
            job.Complete(result);
        }
        catch (OperationCanceledException)
        {
            job.MarkCancelled();
        }
        catch (Exception ex)
        {
            job.Fail(ex.Message);
        }
    }

    private void PruneFinishedJobs()
    {
        if (_jobs.Count < MaxRetainedJobs)
        {
            return;
        }

        var oldestFinished = _jobs.Values
            .Where(j => j.State != ScanJobState.Running)
            .OrderBy(j => j.FinishedAtUtc)
            .FirstOrDefault();
        if (oldestFinished is not null)
        {
            _jobs.TryRemove(oldestFinished.Id, out _);
        }
    }
}
