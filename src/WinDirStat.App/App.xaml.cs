using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using WinDirStat_App.Services;
using WinDirStat.Core.Interfaces;
using WinDirStat.Services;
using WinDirStat.ViewModels;

namespace WinDirStat_App;

public partial class App : Application
{
    public IServiceProvider Services { get; }
    private Window? _mWindow;
    public static Window? MainWindow { get; private set; }

    public App()
    {
        InitializeComponent();
        Services = ConfigureServices();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var mainPage = Services.GetRequiredService<MainPage>();
        _mWindow = new MainWindow(mainPage);
        MainWindow = _mWindow; 
        _mWindow.Activate();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddTransient<MainPageViewModel>();
        services.AddTransient<MainPage>();
        services.AddSingleton<IDiskScanService, DiskScanService>();
        services.AddSingleton<IFolderPickerService, FolderPickerService>(); 
        services.AddSingleton<IScanStateService, ScanStateService>();
        return services.BuildServiceProvider();
    }
}