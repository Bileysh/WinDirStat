using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using WinDirStat_App.Services;
using WinDirStat.Core.Entities;
using WinDirStat.Services;
using WinDirStat.WinRT;

namespace WinDirStat_App;

public static partial class Program
{
    private const string RegisterForBgTaskServerArg = "-RegisterForBGTaskServer";
    private const string SingleInstanceKey = "WinDirStat.MainInstance";
    private static readonly ManualResetEvent ExitEvent = new(false);
    private static readonly ManualResetEvent RedirectEvent = new(false);
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

        var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();

        if (RedirectToExistingInstanceIfAny(activatedArgs))
        {
            return;
        }

        RunAsInteractiveApp(activatedArgs);
    }

    private static bool RedirectToExistingInstanceIfAny(AppActivationArguments activatedArgs)
    {
        var mainInstance = AppInstance.FindOrRegisterForKey(SingleInstanceKey);

        if (mainInstance.IsCurrent)
        {
            mainInstance.Activated += OnActivatedFromAnotherInstance;
            return false;
        }

        Task.Run(async () =>
        {
            await mainInstance.RedirectActivationToAsync(activatedArgs);
            RedirectEvent.Set();
        });
        RedirectEvent.WaitOne();

        return true;
    }

    private static void OnActivatedFromAnotherInstance(object? sender, AppActivationArguments args)
    {
        App.MainDispatcherQueue?.TryEnqueue(() =>
        {
            if (App.MainWindow is not { } window) return;

            window.Activate();
            BringToForeground(window);

            ActivationDispatcher.Handle(args);
        });
    }

    private static void BringToForeground(Window window)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

        if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter &&
            presenter.State == Microsoft.UI.Windowing.OverlappedPresenterState.Minimized)
        {
            presenter.Restore();
        }
        NativeMethods.SetForegroundWindow(hwnd);
    }

    private static partial class NativeMethods
    {
        [System.Runtime.InteropServices.LibraryImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static partial bool SetForegroundWindow(IntPtr hWnd);
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

    private static void RunAsInteractiveApp(AppActivationArguments initialActivationArgs)
    {
        Application.Start(_ =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new App(initialActivationArgs);
        });
    }
}