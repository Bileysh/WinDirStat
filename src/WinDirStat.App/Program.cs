using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using WinDirStat_App.Services;
using WinDirStat.Core.Entities;
using WinDirStat.Services;
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
        if (args.Length >= 3 &&
            args[0].Equals(ElevatedScanHelperClient.ElevatedScanArg, StringComparison.OrdinalIgnoreCase))
        {
            Environment.ExitCode = RunAsElevatedScanHelper(inputFile: args[1], outputFile: args[2]);
            return;
        }

        if (args.Any(a => a.Equals("-Embedding", StringComparison.OrdinalIgnoreCase)
                          || a.Equals(RegisterForBgTaskServerArg, StringComparison.OrdinalIgnoreCase)))
        {
            RunAsBackgroundTaskServer();
            return;
        }

        RunAsInteractiveApp();
    }

    private static int RunAsElevatedScanHelper(string inputFile, string outputFile)
    {
        try
        {
            PrivilegeHelper.EnableBackupPrivilege();

            var paths = File.ReadAllLines(inputFile).Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
            var results = new Dictionary<string, FileSystemNode>();
            var scanService = new DiskScanService(new FileIdentityService());

            foreach (var path in paths)
            {
                try
                {
                    var result = scanService.ScanAsync(path).GetAwaiter().GetResult();
                    results[path] = result.RootNode;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ElevatedScanHelper] Scan of '{path}' failed: {ex}");
                }
            }

            var json = System.Text.Json.JsonSerializer.Serialize(
                results, FileSystemNodeJsonContext.Default.DictionaryStringFileSystemNode);
            File.WriteAllText(outputFile, json);

            return 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ElevatedScanHelper] Batch scan failed: {ex}");
            return 1;
        }
    }

    private static void RunAsBackgroundTaskServer()
    {
        var taskGuid = typeof(BackgroundScanTask).GUID;

        NotificationRegistration.TryRegister("BGTask");

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