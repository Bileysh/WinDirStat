using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using WinDirStat_App.Services;

namespace WinDirStat_App;

public static partial class Program
{
    private const string ElevatedScanArg = "--elevated-scan";
    private const string SingleInstanceKey = "WinDirStat.MainInstance";
    private static readonly ManualResetEvent ExitEvent = new(false);
    private static readonly ManualResetEvent RedirectEvent = new(false);

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
            try
            {
                await mainInstance.RedirectActivationToAsync(activatedArgs);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Program] Redirect failed: {ex}");
            }
            finally
            {
                RedirectEvent.Set();
            }
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