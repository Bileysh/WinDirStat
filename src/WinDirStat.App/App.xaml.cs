using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using WinDirStat_App.Services;
using WinDirStat.Core.Interfaces;
using WinDirStat.Services;
using WinDirStat.ViewModels;

namespace WinDirStat_App;

public partial class App : Application
{
    public IServiceProvider Services { get; }
    private Window? _mWindow;
    private readonly AppActivationArguments? _initialActivationArgs;
    public static Window? MainWindow { get; private set; }
    public static IServiceProvider? StaticServices { get; private set; }
    public static MainPageViewModel? RootViewModel { get; internal set; }

    public static DispatcherQueue? MainDispatcherQueue { get; private set; }

    public App() : this(null)
    {
    }

    public App(AppActivationArguments? initialActivationArgs)
    {
        InitializeComponent();
        _initialActivationArgs = initialActivationArgs;
        MainDispatcherQueue = DispatcherQueue.GetForCurrentThread();

        Services = ConfigureServices();
        StaticServices = Services;

        try
        {
            var registrationError = ClassicContextMenuRegistrar.EnsureRegistered();
            if (registrationError is not null)
            {
                var notificationService = Services.GetService<INotificationService>();
                notificationService?.ShowNotification(
                    "Context menu registration failed",
                    registrationError.Length > 200 ? registrationError[..200] : registrationError);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App] ClassicContextMenuRegistrar threw unexpectedly: {ex}");
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            if (_initialActivationArgs?.Kind == ExtendedActivationKind.StartupTask)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[App] StartupTask activation: registering background scan task, then exiting without a window.");

                try
                {
                    ActivationDispatcher.Handle(_initialActivationArgs);
                }
                catch (Exception ex)
                {
                    var logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "windirstat_registry.log");
                    System.IO.File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss} FAILED: {ex}\n");
                }

                System.Diagnostics.Debug.WriteLine("[App] StartupTask activation: exiting (Environment.Exit(0)).");
                Environment.Exit(0);
                return;
            }

            var windowManager = Services.GetRequiredService<WindowManagerService>();
            var mainPage = windowManager.CreateScopedMainPage();
            RootViewModel = mainPage.ViewModel;
            _mWindow = new MainWindow(mainPage);
            MainWindow = _mWindow;
            _mWindow.Closed += OnMainWindowClosed;
            windowManager.SetRootWindowHandle(_mWindow);
            _mWindow.Activate();

            Services.GetRequiredService<INotificationService>();
            Services.GetRequiredService<IBackgroundScanTaskRegistrar>().EnsureRegistered();

            ActivationDispatcher.Handle(_initialActivationArgs, isColdStart: true);
        }
        catch (Exception ex)
        {
            var logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "windirstat_crash.log");
            System.IO.File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss} OnLaunched CRASHED: {ex}\n");
            throw;
        }
    }

    private static void OnMainWindowClosed(object sender, WindowEventArgs args)
    {
        MainWindow = null;
        RootViewModel = null;
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddScoped<MainPageViewModel>();
        services.AddTransient<MainPage>();
        services.AddSingleton<IDiskScanService, DiskScanService>();
        services.AddSingleton<IElevatedScanHelper, ElevatedScanHelperClient>();
        services.AddScoped<IFolderPickerService, FolderPickerService>();
        services.AddScoped<IWindowHandleProvider, WindowHandleProvider>();
        services.AddScoped<IScanStateService, ScanStateService>();
        services.AddSingleton<WindowManagerService>();
        services.AddSingleton<IWindowManagerService>(sp => sp.GetRequiredService<WindowManagerService>());
        services.AddScoped<IDialogService, WinUiDialogService>();
        services.AddScoped<ICurrentXamlRootProvider, CurrentXamlRootProvider>();
        services.AddSingleton<ILocalizationService, WinUiLocalizationService>();
        services.AddSingleton<IThemeService, WinUiThemeService>();
        services.AddSingleton<INotificationService, AppNotificationService>();
        services.AddSingleton<IDriveInfoService, DriveInfoService>();
        services.AddSingleton<IFileIdentityService, FileIdentityService>();
        services.AddSingleton<IBackgroundScanSettingsService, BackgroundScanSettingsService>();
        services.AddSingleton<IBackgroundScanTaskRegistrar, BackgroundTaskRegistrar>();
        services.AddSingleton<ISettingsFileService, SettingsFileService>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SettingsWindow>();
        services.AddSingleton<IBackgroundScanTestRunner, BackgroundScanTestRunner>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IFileExplorerService, FileExplorerService>();
        services.AddSingleton<IScanResultFileService, ScanResultFileService>();
        return services.BuildServiceProvider();
    }
}