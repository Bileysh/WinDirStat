using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Volumetric_App.Services;
using Volumetric.Core.Interfaces;
using Volumetric.Services;
using Volumetric.ViewModels;

namespace Volumetric_App;

public partial class App
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

        UnhandledException += (_, e) =>
        {
            Serilog.Log.Fatal(e.Exception, "WinUI Application.UnhandledException (Handled will be set to true)");
            e.Handled = true;
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        if (_initialActivationArgs?.Kind == ExtendedActivationKind.StartupTask)
        {
            ActivationDispatcher.Handle(_initialActivationArgs);
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

        if (Program.PendingNotificationPath is { } pendingPath)
        {
            Program.PendingNotificationPath = null;
            ActivationDispatcher.HandleExtracted(
                new ActivationDispatcher.ExtractedActivation(ActivationDispatcher.ActivationAction.Path, pendingPath),
                isColdStart: true);
        }
    }

    private static void OnMainWindowClosed(object sender, WindowEventArgs args)
    {
        MainWindow = null;
        RootViewModel = null;
        Current.Exit();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddScoped<MainPageViewModel>();
        services.AddTransient<MainPage>();
        services.AddSingleton<IDiskScanService, DiskScanService>();
        services.AddSingleton<IAppLogger, SerilogAppLogger>();
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
