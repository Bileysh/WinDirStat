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

    public App() : this(null) { }

    public App(AppActivationArguments? initialActivationArgs)
    {
        InitializeComponent();
        _initialActivationArgs = initialActivationArgs;
        MainDispatcherQueue = DispatcherQueue.GetForCurrentThread();

        Services = ConfigureServices();
        StaticServices = Services;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var windowManager = Services.GetRequiredService<WindowManagerService>();
        var mainPage = windowManager.CreateScopedMainPage();
        RootViewModel = mainPage.ViewModel;
        _mWindow = new MainWindow(mainPage);
        MainWindow = _mWindow;
        _mWindow.Closed += (_, _) => { MainWindow = null; RootViewModel = null; };
        windowManager.SetRootWindowHandle(_mWindow);
        _mWindow.Activate();

        Services.GetRequiredService<INotificationService>();
        Services.GetRequiredService<IBackgroundScanTaskRegistrar>().EnsureRegistered();

        ActivationDispatcher.Handle(_initialActivationArgs);
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