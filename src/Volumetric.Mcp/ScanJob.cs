using Volumetric.Core.Entities;

namespace Volumetric.Mcp;

public enum ScanJobState
{
    Running,
    Completed,
    Failed,
    Cancelled
}

public sealed class ScanJob : IProgress<ScanProgress>
{
    private ScanProgress? _latestProgress;
    private volatile int _state = (int)ScanJobState.Running;

    public ScanJob(string rootPath)
    {
        RootPath = rootPath;
    }

    public string Id { get; } = Guid.NewGuid().ToString("N");
    public string RootPath { get; }
    public DateTime StartedAtUtc { get; } = DateTime.UtcNow;
    public DateTime? FinishedAtUtc { get; private set; }
    public CancellationTokenSource Cancellation { get; } = new();

    public ScanJobState State => (ScanJobState)_state;
    public ScanResult? Result { get; private set; }
    public string? Error { get; private set; }
    public ScanProgress? LatestProgress => Volatile.Read(ref _latestProgress);

    public TimeSpan Elapsed => (FinishedAtUtc ?? DateTime.UtcNow) - StartedAtUtc;

    void IProgress<ScanProgress>.Report(ScanProgress value) => Volatile.Write(ref _latestProgress, value);

    public void Complete(ScanResult result)
    {
        Result = result;
        Finish(ScanJobState.Completed);
    }

    public void Fail(string error)
    {
        Error = error;
        Finish(ScanJobState.Failed);
    }

    public void MarkCancelled() => Finish(ScanJobState.Cancelled);

    private void Finish(ScanJobState state)
    {
        FinishedAtUtc = DateTime.UtcNow;
        _state = (int)state;
    }
}
