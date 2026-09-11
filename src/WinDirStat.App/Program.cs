using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using WinDirStat_App.Services;
using WinDirStat.WinRT;

namespace WinDirStat_App;

public static partial class Program
{
    private const string ElevatedScanArg = "--elevated-scan";
    private const string RegisterForBgTaskServerArg = "-RegisterForBGTaskServer";
    private const string SingleInstanceKey = "WinDirStat.MainInstance";
    private static readonly ManualResetEvent ExitEvent = new(false);
    private static readonly ManualResetEvent RedirectEvent = new(false);
    private static uint _registrationToken;

    [STAThread]
    static void Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        if (args.Length >= 3 && args[0] == ElevatedScanArg)
        {
            var exitCode = ElevatedScanServer.Run(args[1], args[2]);
            Environment.Exit(exitCode);
            return;
        }
        if (args.Any(a => a.Equals("--explorer-command-server", StringComparison.OrdinalIgnoreCase)))
        {
            RunAsExplorerCommandServer();
            return;
        }

        if (args.Any(a =>
                a.Equals("-Embedding", StringComparison.OrdinalIgnoreCase) || a.Equals(RegisterForBgTaskServerArg,
                    StringComparison.OrdinalIgnoreCase)))
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
        var mainInstance = AppInstance.FindOrRegisterForKey(SingleInstanceKey);

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
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                RedirectEvent.Set();
            }
        });
        RedirectEvent.WaitOne();

        if (redirectSucceeded)
        {
            return true;
        }

        var retryInstance = AppInstance.FindOrRegisterForKey(SingleInstanceKey);
        if (retryInstance.IsCurrent)
        {
            retryInstance.Activated += OnActivatedFromAnotherInstance;
        }

        return false;
    }

    private static void OnActivatedFromAnotherInstance(object? sender, AppActivationArguments args)
    {
        var extracted = ActivationDispatcher.Extract(args);

        App.MainDispatcherQueue?.TryEnqueue(() =>
        {
            if (App.MainWindow is not null)
            {
                BringToForeground(App.MainWindow);
            }

            ActivationDispatcher.HandleExtracted(extracted, isColdStart: false);
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
        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool SetForegroundWindow(IntPtr hWnd);
    }

    private static void RunAsInteractiveApp(AppActivationArguments initialActivationArgs)
    {
        Application.Start(_ =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new App(initialActivationArgs);
        });
    }

    private static void RunAsExplorerCommandServer()
    {
        var clsid = new Guid(ExplorerCommandServer.ExplorerCommandClsid);

        ComServer.CoRegisterClassObject(
            ref clsid,
            new ExplorerCommandServer.ExplorerCommandFactory(),
            ComServer.CLSCTX_LOCAL_SERVER,
            ComServer.REGCLS_MULTIPLEUSE,
            out var explorerCommandToken);

        ExitEvent.WaitOne();

        ComServer.CoRevokeClassObject(explorerCommandToken);
    }
}