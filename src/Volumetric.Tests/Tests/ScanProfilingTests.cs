using System.Collections.Concurrent;
using System.Diagnostics;
using Volumetric.Services;
using Volumetric.Tests.FakeService;
using Volumetric.ViewModels;
using Xunit.Abstractions;

namespace Volumetric.Tests.Tests;

public class ScanProfilingTests(ITestOutputHelper output)
{
    private const string PathVariable = "VOLUMETRIC_PROFILE_PATH";

    [ProfilingFact]
    public async Task ScanPathAsync_LargeDrive_ReportsUiThreadAndMemoryCost()
    {
        var path = Environment.GetEnvironmentVariable(PathVariable)!;
        using var uiThread = new RecordingUiContext();
        var scanState = new ScanStateService();
        var vm = CreateViewModel(scanState);
        var scanFinished = new TaskCompletionSource();
        var total = Stopwatch.StartNew();

        uiThread.Post(async _ =>
        {
            await vm.ScanPathAsync(path);
            scanFinished.SetResult();
        }, null);
        await scanFinished.Task;
        total.Stop();

        var callbacks = uiThread.Callbacks.ToList();
        var nodes = CountNodes(scanState.CurrentResult!.RootNode);

        output.WriteLine($"Path                         : {path}");
        output.WriteLine($"Nodes                        : {nodes:N0}");
        output.WriteLine($"Wall time                    : {total.Elapsed.TotalSeconds:N1} s");
        output.WriteLine($"UI callbacks                 : {callbacks.Count:N0}");
        output.WriteLine($"UI busy total                : {callbacks.Sum(c => c.Duration.TotalMilliseconds):N0} ms");
        output.WriteLine($"Longest UI callback          : {callbacks.Max(c => c.Duration.TotalMilliseconds):N0} ms");
        output.WriteLine($"UI callbacks over 50 ms      : {callbacks.Count(c => c.Duration.TotalMilliseconds > 50):N0}");
        output.WriteLine($"Post-scan UI work            : {callbacks[^1].Duration.TotalMilliseconds:N0} ms");
        output.WriteLine($"Managed heap after scan      : {GC.GetTotalMemory(forceFullCollection: true) / 1024 / 1024:N0} MB");
        output.WriteLine($"Peak working set             : {Process.GetCurrentProcess().PeakWorkingSet64 / 1024 / 1024:N0} MB");

        Assert.False(vm.IsScanning);
        Assert.True(nodes > 0);
    }

    private static MainPageViewModel CreateViewModel(ScanStateService scanState) =>
        new(
            new DiskScanService(new FileIdentityService()),
            new FakeFolderPickerService(),
            scanState,
            new FakeWindowManagerService(),
            new FakeDialogService(),
            new FakeLocalizationService(),
            new FakeThemeService(),
            new FakeNotificationService(),
            new DriveInfoService(),
            new FakeClipboardService(),
            new FakeFileExplorerService(),
            new FakeBackgroundScanSettingsService(),
            new FakeScanResultFileService(),
            new FakeWindowHandleProvider(),
            new FakeAppLogger(),
            new FakeRecentScansService());

    private static long CountNodes(Core.Entities.FileSystemNode root)
    {
        var stack = new Stack<Core.Entities.FileSystemNode>([root]);
        long count = 0;
        while (stack.TryPop(out var node))
        {
            count++;
            foreach (var child in node.Children)
            {
                stack.Push(child);
            }
        }

        return count;
    }

    private sealed class ProfilingFactAttribute : FactAttribute
    {
        public ProfilingFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(PathVariable)))
            {
                Skip = $"Set {PathVariable} to a folder or drive to profile a real scan.";
            }
        }
    }

    private sealed record UiCallback(TimeSpan StartedAt, TimeSpan Duration);

    private sealed class RecordingUiContext : SynchronizationContext, IDisposable
    {
        private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = new();
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly Thread _thread;

        public ConcurrentQueue<UiCallback> Callbacks { get; } = new();

        public RecordingUiContext()
        {
            _thread = new Thread(Run) { IsBackground = true, Name = "Simulated UI thread" };
            _thread.Start();
        }

        public override void Post(SendOrPostCallback d, object? state) => _queue.Add((d, state));

        public override void Send(SendOrPostCallback d, object? state) =>
            throw new NotSupportedException();

        private void Run()
        {
            SetSynchronizationContext(this);
            foreach (var (callback, state) in _queue.GetConsumingEnumerable())
            {
                var startedAt = _clock.Elapsed;
                callback(state);
                Callbacks.Enqueue(new UiCallback(startedAt, _clock.Elapsed - startedAt));
            }
        }

        public void Dispose() => _queue.CompleteAdding();
    }
}
