using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using WinDirStat.ViewModels;

namespace WinDirStat_App;

public partial class App : Application
{
    public IServiceProvider Services { get; }
    private Window? _mWindow;
    
    public App()
    {
        InitializeComponent();
        Services = ConfigureServices();
    }
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var mainPage = Services.GetRequiredService<MainPage>();
        _mWindow = new MainWindow(mainPage);
        _mWindow.Activate();
    }
    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddTransient<MainPageViewModel>();
        services.AddTransient<MainPage>();
        return services.BuildServiceProvider();
    }
}
