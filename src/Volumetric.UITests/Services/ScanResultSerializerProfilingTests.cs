using System.Diagnostics;
using Volumetric_App.Services;
using Volumetric.Core.Entities;
using Volumetric.Core.Interfaces;
using Volumetric.Services;
using Xunit.Abstractions;

namespace Volumetric.UITests.Services;

public class ScanResultSerializerProfilingTests(ITestOutputHelper output)
{
    private const string PathVariable = "VOLUMETRIC_PROFILE_PATH";

    [ProfilingFact]
    public async Task ExportAndImport_LargeScan_ReportsPeakMemoryOverhead()
    {
        var scan = await new DiskScanService(new FileIdentityService())
            .ScanAsync(Environment.GetEnvironmentVariable(PathVariable)!);
        var keyStore = new InMemoryKeyStore();
        var file = Path.Combine(Path.GetTempPath(), $"volumetric-profile-{Guid.NewGuid():N}.volscan");

        try
        {
            foreach (var encrypt in new[] { true, false })
            {
                var serializer = new ScanResultSerializer(new FakeSecuritySettings(encrypt), keyStore);
                var label = encrypt ? "encrypted" : "plain    ";

                var (exportMb, exportAllocatedMb, exportTime) = await MeasureAsync(async () =>
                {
                    await using var stream = new FileStream(file, FileMode.Create, FileAccess.ReadWrite);
                    await serializer.WriteAsync(stream, scan.RootNode);
                });
                var size = new FileInfo(file).Length / 1024 / 1024;

                FileSystemNode? imported = null;
                var (importMb, importAllocatedMb, importTime) = await MeasureAsync(async () =>
                {
                    await using var stream = new FileStream(file, FileMode.Open, FileAccess.Read);
                    imported = await serializer.ReadAsync(stream);
                });

                Assert.Equal(scan.RootNode.Children.Count, imported!.Children.Count);
                output.WriteLine($"{label} file {size,4} MB | export peak +{exportMb,4} MB, allocated {exportAllocatedMb,4} MB, " +
                                 $"{exportTime.TotalSeconds:N1} s | import peak +{importMb,4} MB, allocated " +
                                 $"{importAllocatedMb,4} MB, {importTime.TotalSeconds:N1} s");
                imported = null;
            }
        }
        finally
        {
            File.Delete(file);
        }
    }

    private static async Task<(long PeakMb, long AllocatedMb, TimeSpan Elapsed)> MeasureAsync(Func<Task> operation)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        using var process = Process.GetCurrentProcess();
        process.Refresh();
        var baseline = process.WorkingSet64;
        var peak = baseline;
        using var cts = new CancellationTokenSource();

        var sampler = Task.Run(async () =>
        {
            while (!cts.IsCancellationRequested)
            {
                process.Refresh();
                peak = Math.Max(peak, process.WorkingSet64);
                await Task.Delay(20);
            }
        });

        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        var elapsed = Stopwatch.StartNew();
        await operation();
        elapsed.Stop();
        var allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
        await cts.CancelAsync();
        await sampler;

        return ((peak - baseline) / 1024 / 1024, allocated / 1024 / 1024, elapsed.Elapsed);
    }

    private sealed class ProfilingFactAttribute : FactAttribute
    {
        public ProfilingFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(PathVariable)))
            {
                Skip = $"Set {PathVariable} to a folder or drive to profile export and import of a real scan.";
            }
        }
    }

    private sealed class FakeSecuritySettings(bool encrypt) : ISecuritySettingsService
    {
        public bool EncryptScanResults { get; set; } = encrypt;
        public bool RequireWindowsHelloOnLaunch { get; set; }
    }

    private sealed class InMemoryKeyStore : IScanResultKeyStore
    {
        private byte[]? _key;

        public byte[] GetOrCreateKey() => _key ??= ScanResultCipher.GenerateKey();

        public byte[]? TryGetKey() => _key;
    }
}
