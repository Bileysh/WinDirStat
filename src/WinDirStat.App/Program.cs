using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppNotifications;
using WinDirStat_App.Services;
using WinDirStat.WinRT;

namespace WinDirStat_App;

public static class Program
{
    private const string RegisterForBgTaskServerArg = "-RegisterForBGTaskServer";
    private static readonly ManualResetEvent ExitEvent = new(false);
    private static uint _registrationToken;

    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Any(a => a.Equals("-Embedding", StringComparison.OrdinalIgnoreCase)
                          || a.Equals(RegisterForBgTaskServerArg, StringComparison.OrdinalIgnoreCase)))
        {
            RunAsBackgroundTaskServer();
            return;
        }

        RunAsInteractiveApp();
    }

    private static void RunAsBackgroundTaskServer()
    {
        var taskGuid = typeof(BackgroundScanTask).GUID;

        try { AppNotificationManager.Default.Register(); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[BGTask] Notification register failed: {ex}"); }

        BackgroundScanTask.Completed += OnBackgroundScanTaskCompleted;

        ComServer.CoRegisterClassObject(
            ref taskGuid,
            new ComServer.BackgroundTaskFactory(),
            ComServer.CLSCTX_LOCAL_SERVER,
            ComServer.REGCLS_MULTIPLEUSE,
            out _registrationToken);

        ExitEvent.WaitOne();

        BackgroundScanTask.Completed -= OnBackgroundScanTaskCompleted;
        ComServer.CoRevokeClassObject(_registrationToken);
    }

    private static void OnBackgroundScanTaskCompleted(object? sender, EventArgs e) => ExitEvent.Set();

    private static void RunAsInteractiveApp()
    {
        Application.Start(_ =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }
}