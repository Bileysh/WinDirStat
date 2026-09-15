using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Serilog;
using WinDirStat_App.Services;
using WinDirStat.WinRT;

namespace WinDirStat_App;

public static partial class Program
{
    private const string ElevatedScanArg = "--elevated-scan";
    private const string RegisterForBgTaskServerArg = "-RegisterForBGTaskServer";
    private const string SingleInstanceKey = "WinDirStat.MainInstance";
    internal static readonly ManualResetEvent ExitEvent = new(false);
    private static readonly ManualResetEvent RedirectEvent = new(false);
    private static uint _registrationToken;

    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();
        }
        catch (InvalidOperationException)
        {
        }

        if (args.Length >= 3 && args[0] == ElevatedScanArg)
        {
            AppLogger.Initialize("ElevatedScan");
            var exitCode = ElevatedScanServer.Run(args[1], args[2]);
            Log.Information("ElevatedScanServer.Run returned exit code {ExitCode}", exitCode);
            AppLogger.Shutdown();
            Environment.Exit(exitCode);
            return;
        }

        if (args.Any(a => a.Equals("--explorer-command-server", StringComparison.OrdinalIgnoreCase)))
        {
            AppLogger.Initialize("ExplorerCommandServer");
            RunAsExplorerCommandServer();
            AppLogger.Shutdown();
            return;
        }

        if (args.Any(a =>
                a.Equals("-Embedding", StringComparison.OrdinalIgnoreCase) || a.Equals(RegisterForBgTaskServerArg,
                    StringComparison.OrdinalIgnoreCase)))
        {
            AppLogger.Initialize("BackgroundTaskServer");
            RunAsBackgroundTaskServer();
            AppLogger.Shutdown();
            return;
        }

        AppLogger.Initialize("Interactive");
        var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        Log.Information("Initial activation kind: {Kind}", activatedArgs.Kind);

        if (RedirectToExistingInstanceIfAny(activatedArgs))
        {
            Log.Information("Redirected to existing instance, exiting this process");
            AppLogger.Shutdown();
            return;
        }

        RunAsInteractiveApp(activatedArgs);
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

    private static bool RedirectToExistingInstanceIfAny(AppActivationArguments activatedArgs)
    {
        Log.Information("RedirectToExistingInstanceIfAny: activation kind={Kind}", activatedArgs.Kind);

        var mainInstance = AppInstance.FindOrRegisterForKey(SingleInstanceKey);
        Log.Information("FindOrRegisterForKey: IsCurrent={IsCurrent}", mainInstance.IsCurrent);

        if (mainInstance.IsCurrent)
        {
            mainInstance.Activated += OnActivatedFromAnotherInstance;
            return false;
        }

        var redirectSucceeded = false;
        Task.Run(async () =>
        {
            try
            {
                await mainInstance.RedirectActivationToAsync(activatedArgs);
                redirectSucceeded = true;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "RedirectActivationToAsync failed (stale registration?)");
            }
            finally
            {
                RedirectEvent.Set();
            }
        });
        var redirectCompleted = RedirectEvent.WaitOne(8000);
        if (!redirectCompleted)
        {
            Log.Warning("RedirectActivationToAsync did not complete within 8s; proceeding without it " +
                        "(if it lands late, both instances may handle the same activation)");
        }

        if (redirectSucceeded)
        {
            Log.Information("Redirect succeeded");
            return true;
        }

        Log.Warning("Redirect failed — retrying FindOrRegisterForKey (self-heal path)");
        var retryInstance = AppInstance.FindOrRegisterForKey(SingleInstanceKey);
        if (retryInstance.IsCurrent)
        {
            Log.Information("Self-heal succeeded: this process is now the registered instance");
            retryInstance.Activated += OnActivatedFromAnotherInstance;
        }

        return false;
    }

    private static void OnActivatedFromAnotherInstance(object? sender, AppActivationArguments args)
    {
        ActivationDispatcher.ExtractedActivation extracted;
        try
        {
            extracted = ActivationDispatcher.Extract(args);
            Log.Information("OnActivatedFromAnotherInstance: extracted {Action} / '{Path}'",
                extracted.Action, extracted.Path);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "OnActivatedFromAnotherInstance: Extract failed (source process likely already exited)");
            return;
        }

        App.MainDispatcherQueue?.TryEnqueue(() => DispatchActivation(extracted));
    }

    private static void DispatchActivation(ActivationDispatcher.ExtractedActivation extracted)
    {
        try
        {
            if (App.MainWindow is not null)
            {
                BringToForeground(App.MainWindow);
            }
            ActivationDispatcher.HandleExtracted(extracted, isColdStart: false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "DispatchActivation failed");
        }
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
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool SetForegroundWindow(IntPtr hWnd);
    }

    private static void RunAsInteractiveApp(AppActivationArguments initialActivationArgs)
    {
        Application.Start(p => StartAppInitialization(p, initialActivationArgs));
    }

    private static void StartAppInitialization(ApplicationInitializationCallbackParams p, AppActivationArguments args)
    {
        var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
        SynchronizationContext.SetSynchronizationContext(context);
        new App(args);
    }

    private static void RunAsExplorerCommandServer()
    {
        Log.Information("RunAsExplorerCommandServer: registering CLSID {Clsid}",
            ExplorerCommandServer.ExplorerCommandClsid);

        var clsid = new Guid(ExplorerCommandServer.ExplorerCommandClsid);

        var hr = ComServer.CoRegisterClassObject(
            ref clsid,
            new ExplorerCommandServer.ExplorerCommandFactory(),
            ComServer.CLSCTX_LOCAL_SERVER,
            ComServer.REGCLS_MULTIPLEUSE,
            out var explorerCommandToken);
        Log.Information("CoRegisterClassObject returned hr=0x{Hr:X8}, token={Token}", hr, explorerCommandToken);

        var activatedInTime = ExitEvent.WaitOne(TimeSpan.FromSeconds(30));
        Log.Information("ExplorerCommandServer exiting (activated-before-timeout={Activated})", activatedInTime);

        ComServer.CoRevokeClassObject(explorerCommandToken);
    }
}