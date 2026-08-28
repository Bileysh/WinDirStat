using System.Runtime.InteropServices;
using System.Linq;
using Windows.ApplicationModel.Background;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Windows.Storage;

namespace WinDirStat.WinRT;

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
[Guid("F14A5D3E-9B71-4C82-8E2A-6D9F3C1B7A44")]
[ComSourceInterfaces(typeof(IBackgroundTask))]
public sealed class BackgroundScanTask : IBackgroundTask
{
    private const string ThresholdKey = "BackgroundScan.LowFreeSpaceThresholdPercent";
    private const double DefaultLowFreeSpacePercentThreshold = 10.0;

    private static double LowFreeSpacePercentThreshold
    {
        get
        {
            var values = ApplicationData.Current.LocalSettings.Values;
            return values.TryGetValue(ThresholdKey, out var v) && v is double stored
                ? stored
                : DefaultLowFreeSpacePercentThreshold;
        }
    }


    public static event EventHandler? Completed;

    private BackgroundTaskDeferral? _deferral;

    public void Run(IBackgroundTaskInstance taskInstance)
    {
        _deferral = taskInstance.GetDeferral();
        taskInstance.Canceled += OnCanceled;

        try
        {
            RunScanAndNotify();
        }
        finally
        {
            _deferral.Complete();
            Completed?.Invoke(this, EventArgs.Empty);
        }
    }

    public static void RunScanAndNotify()
    {
        var results = ScanReadyDrives();
        PersistResults(results);
        ShowNotifications(results);
    }
    
    private static IReadOnlyList<DriveScanResult> ScanReadyDrives()
    {
        var results = new List<DriveScanResult>();

        foreach (var drive in DriveInfo.GetDrives())
        {
             if (!drive.IsReady) continue;

            try
            {
                results.Add(new DriveScanResult(drive.Name, drive.TotalSize, drive.AvailableFreeSpace));
            }
            catch (IOException)
            {
            }
        }

        return results;
    }

    private static void PersistResults(IReadOnlyList<DriveScanResult> results)
    {
        var lines = results.Select(r => $"{r.DriveName}|{r.TotalBytes}|{r.FreeBytes}");
        var path = Path.Combine(ApplicationData.Current.LocalFolder.Path, "bgtask-last-scan.txt");
        File.WriteAllLines(path, lines);
    }
    private static void ShowNotifications(IReadOnlyList<DriveScanResult> results)
    {
        if (results.Count == 0) return;

        var summaryLines = results.Select(r =>
            $"{r.DriveName} — вільно {FormatBytes(r.FreeBytes)} з {FormatBytes(r.TotalBytes)}");

        var summary = new AppNotificationBuilder()
            .AddText("Стан дисків")
            .AddText(string.Join("\n", summaryLines))
            .BuildNotification();
        AppNotificationManager.Default.Show(summary);

        foreach (var drive in results.Where(r => 100.0 - r.UsedPercent < LowFreeSpacePercentThreshold))
        {
            var warning = new AppNotificationBuilder()
                .AddText("Мало вільного місця")
                .AddText($"{drive.DriveName}: залишилось лише {FormatBytes(drive.FreeBytes)} " +
                         $"({100.0 - drive.UsedPercent:F0}% вільно)")
                .BuildNotification();
            AppNotificationManager.Default.Show(warning);
        }
    }

    private static string FormatBytes(long bytes)
    {
        const double gb = 1024.0 * 1024 * 1024;
        return $"{bytes / gb:F2} GB";
    }

    private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
    {
        _deferral?.Complete();
        Completed?.Invoke(this, EventArgs.Empty);
    }
}