using System.Diagnostics;
using Volumetric.Core.Entities;
using Volumetric.Services;

namespace Volumetric.Tests.Tests;

[Trait("Category", "LongRunning")]
public class DiskScanServiceLongRunningTests(DiskScanServiceLongRunningTests.LargeTree tree)
    : IClassFixture<DiskScanServiceLongRunningTests.LargeTree>
{
    [Fact]
    public async Task ScanAsync_HundredThousandFiles_ProgressOnlyGrowsAndTotalsMatchTheTree()
    {
        var service = new DiskScanService(new FileIdentityService());
        var progress = new RecordingProgress();

        var result = await service.ScanAsync(tree.Root, progress: progress);

        Assert.Equal(ScanStatus.Ok, result.RootNode.Status);
        Assert.Equal(LargeTree.FileCount, result.TotalSize);
        Assert.Equal(LargeTree.FolderCount, result.RootNode.Children.Count);
        Assert.All(result.RootNode.Children, folder => Assert.Equal(LargeTree.FilesPerFolder, folder.Children.Count));

        var reports = progress.Snapshot();
        Assert.True(reports.Count > LargeTree.FileCount / 100, $"Only {reports.Count} progress reports");

        for (var i = 1; i < reports.Count; i++)
        {
            Assert.True(reports[i].FilesScanned >= reports[i - 1].FilesScanned, $"Files went backwards at report {i}");
            Assert.True(reports[i].FoldersScanned >= reports[i - 1].FoldersScanned,
                $"Folders went backwards at report {i}");
        }

        Assert.InRange(reports[^1].FilesScanned, LargeTree.FileCount - 64, LargeTree.FileCount);
        Assert.InRange(reports[^1].FoldersScanned, 0, LargeTree.FolderCount);
    }

    [Fact]
    public async Task ScanAsync_HundredThousandFiles_CancelledMidway_StopsWithinOneSecond()
    {
        var service = new DiskScanService(new FileIdentityService());
        using var cts = new CancellationTokenSource();
        var cancelledAt = new Stopwatch();
        long filesAtCancel = 0;

        var progress = new CallbackProgress(p =>
        {
            if (p.FilesScanned < LargeTree.FileCount / 4 || cts.IsCancellationRequested)
            {
                return;
            }

            filesAtCancel = p.FilesScanned;
            cancelledAt.Start();
            cts.Cancel();
        });

        var scan = service.ScanAsync(tree.Root, cts.Token, progress: progress);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await scan);
        cancelledAt.Stop();

        Assert.True(filesAtCancel < LargeTree.FileCount, "The scan finished before it could be cancelled");
        Assert.True(cancelledAt.Elapsed < TimeSpan.FromSeconds(1),
            $"Cancellation took {cancelledAt.Elapsed.TotalMilliseconds:N0} ms");
    }

    public sealed class LargeTree : IDisposable
    {
        public const int FolderCount = 100;
        public const int FilesPerFolder = 1000;
        public const int FileCount = FolderCount * FilesPerFolder;

        public string Root { get; } = Directory.CreateTempSubdirectory("volumetric-large-").FullName;

        public LargeTree()
        {
            Parallel.For(0, FolderCount, folder =>
            {
                var folderPath = Directory.CreateDirectory(Path.Combine(Root, $"d{folder:D3}")).FullName;
                for (var file = 0; file < FilesPerFolder; file++)
                {
                    using var stream = new FileStream(Path.Combine(folderPath, $"f{file:D4}.bin"), FileMode.CreateNew);
                    stream.WriteByte(0);
                }
            });
        }

        public void Dispose()
        {
            Parallel.ForEach(Directory.GetDirectories(Root), folder => Directory.Delete(folder, recursive: true));
            Directory.Delete(Root);
        }
    }

    private sealed class RecordingProgress : IProgress<ScanProgress>
    {
        private readonly List<ScanProgress> _reports = [];
        private readonly Lock _gate = new();

        public void Report(ScanProgress value)
        {
            lock (_gate)
            {
                _reports.Add(value);
            }
        }

        public List<ScanProgress> Snapshot()
        {
            lock (_gate)
            {
                return [.. _reports];
            }
        }
    }

    private sealed class CallbackProgress(Action<ScanProgress> onReport) : IProgress<ScanProgress>
    {
        public void Report(ScanProgress value) => onReport(value);
    }
}
