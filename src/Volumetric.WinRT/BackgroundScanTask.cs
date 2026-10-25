using System.Runtime.InteropServices;
using System.Linq;
using System.Threading;
using Windows.ApplicationModel.Background;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.ApplicationModel.Resources;
using Microsoft.Windows.Globalization;
using Windows.Storage;

namespace Volumetric.WinRT;

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
[Guid("F14A5D3E-9B71-4C82-8E2A-6D9F3C1B7A44")]
[ComSourceInterfaces(typeof(IBackgroundTask))]
public sealed class BackgroundScanTask : IBackgroundTask
{
    private const string ThresholdKey = "BackgroundScan.LowFreeSpaceThresholdPercent";
    private const double DefaultLowFreeSpacePercentThreshold = 10.0;
    private const string DriveStatusSummaryLineKey = "DriveStatusSummaryLine";
    private const string DriveStatusNotificationTitleKey = "DriveStatusNotificationTitle";
    private const string LowSpaceNotificationTitleKey = "LowSpaceNotificationTitle";
    private const string LowSpaceNotificationBodyKey = "LowSpaceNotificationBody";

    private static readonly ResourceManager ResourceManager = new();

    private static string GetString(string key)
    {
        var context = ResourceManager.CreateResourceContext();
        context.QualifierValues["Language"] = ApplicationLanguages.PrimaryLanguageOverride;
        return ResourceManager.MainResourceMap.GetValue($"Resources/{key}", context).ValueAsString;
    }

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
    private int _deferralCompleted;

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
            CompleteDeferralOnce();
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
            if (!drive.IsReady)
            {
                continue;
            }

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
        if (results.Count == 0)
        {
            return;
        }

        var summaryLines = results.Select(r =>
            string.Format(GetString(DriveStatusSummaryLineKey), r.DriveName, FormatBytes(r.FreeBytes), FormatBytes(r.TotalBytes)));

        var summary = new AppNotificationBuilder()
            .AddText(GetString(DriveStatusNotificationTitleKey))
            .AddText(string.Join("\n", summaryLines))
            .AddArgument("path", results[0].DriveName)
            .BuildNotification();
        AppNotificationManager.Default.Show(summary);

        foreach (var drive in results.Where(r => 100.0 - r.UsedPercent < LowFreeSpacePercentThreshold))
        {
            var warning = new AppNotificationBuilder()
                .AddText(GetString(LowSpaceNotificationTitleKey))
                .AddText(string.Format(GetString(LowSpaceNotificationBodyKey), drive.DriveName,
                    FormatBytes(drive.FreeBytes), (100.0 - drive.UsedPercent).ToString("F0")))
                .AddArgument("path", drive.DriveName)
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
        CompleteDeferralOnce();
        Completed?.Invoke(this, EventArgs.Empty);
    }

    private void CompleteDeferralOnce()
    {
        if (Interlocked.Exchange(ref _deferralCompleted, 1) == 0)
        {
            _deferral?.Complete();
        }
    }
}
