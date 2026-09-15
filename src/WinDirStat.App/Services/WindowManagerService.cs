using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinDirStat.Core.Interfaces;
using WinDirStat.Core.Entities;
using WinDirStat.ViewModels;
using WinDirStat_App.UserControls;
using System.Diagnostics;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;

namespace WinDirStat_App.Services;

public class WindowManagerService : IWindowManagerService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localizationService;
    private readonly List<Window> _openWindows = new();

    public WindowManagerService(IServiceScopeFactory scopeFactory,
        IThemeService themeService, ILocalizationService localizationService)
    {
        _scopeFactory = scopeFactory;
        _themeService = themeService;
        _localizationService = localizationService;

        _themeService.ThemeChanged += OnThemeChanged;
    }

    private void OnThemeChanged(object? sender, bool isDark)
    {
        var theme = isDark ? ElementTheme.Dark : ElementTheme.Light;

        if (App.MainWindow?.Content is FrameworkElement mainContent)
            mainContent.RequestedTheme = theme;

        foreach (var window in _openWindows)
        {
            if (window.Content is FrameworkElement fe)
                fe.RequestedTheme = theme;
        }
    }

    public void OpenMainWindow(string? initialScanPath = null)
    {
        var (viewModel, _) = CreateAndShowNewMainWindow();

        if (!string.IsNullOrWhiteSpace(initialScanPath))
        {
            _ = viewModel.ScanPathAsync(initialScanPath);
        }
    }

    public void OpenMainWindowWithImportedResult(FileSystemNode rootNode)
    {
        var (viewModel, _) = CreateAndShowNewMainWindow();
        viewModel.LoadImportedResult(rootNode);
    }

    private (MainPageViewModel ViewModel, Window Window) CreateAndShowNewMainWindow()
    {
        var newWindow = new Window { ExtendsContentIntoTitleBar = true };
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, WindowManagerConstants.MicaMinBuildNumber) &&
            MicaController.IsSupported())
            newWindow.SystemBackdrop = new MicaBackdrop();

        var scope = _scopeFactory.CreateScope();
        var viewModel = scope.ServiceProvider.GetRequiredService<MainPageViewModel>();
        var xamlRootProvider = scope.ServiceProvider.GetRequiredService<ICurrentXamlRootProvider>();
        var page = new MainPage(viewModel, xamlRootProvider);

        scope.ServiceProvider.GetRequiredService<IWindowHandleProvider>().Hwnd =
            WindowNative.GetWindowHandle(newWindow);

        newWindow.Content = page;

        newWindow.Title = _localizationService.GetString("WindowTitle_New");

        _openWindows.Add(newWindow);
        newWindow.Closed += (_, _) => CleanupNewMainWindow(newWindow, viewModel, scope);
        OffsetWindowPosition(newWindow);

        newWindow.Activate();

        if (newWindow.Content is FrameworkElement fe)
        {
            fe.RequestedTheme = _themeService.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
        }

        return (viewModel, newWindow);
    }

    private Window CreateDetachedWindow(string title, FrameworkElement content, int width, int height)
    {
        var newWindow = new Window { ExtendsContentIntoTitleBar = true };
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, WindowManagerConstants.MicaMinBuildNumber) &&
            MicaController.IsSupported())
            newWindow.SystemBackdrop = new MicaBackdrop();

        var rootGrid = new Grid
        {
            Style = (Style)Application.Current.Resources["DetachedWindowRootGridStyle"],
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
        };

        rootGrid.RowDefinitions.Add(new RowDefinition
            { Height = new GridLength(WindowManagerConstants.TitleBarRowHeight) });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var titleText = new TextBlock
        {
            Text = title,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = WindowManagerConstants.TitleTextMargin,
            FontSize = WindowManagerConstants.TitleTextFontSize,
            Opacity = WindowManagerConstants.TitleTextOpacity
        };
        Grid.SetRow(titleText, 0);
        rootGrid.Children.Add(titleText);

        content.Margin = WindowManagerConstants.ContentMargin;
        Grid.SetRow(content, 1);
        rootGrid.Children.Add(content);

        newWindow.Content = rootGrid;
        newWindow.Title = $"Volumetric - {title}";
        _openWindows.Add(newWindow);
        newWindow.Closed += (_, _) => _openWindows.Remove(newWindow);
        newWindow.AppWindow.Resize(new Windows.Graphics.SizeInt32(width, height));

        OffsetWindowPosition(newWindow);
        
        rootGrid.RequestedTheme = _themeService.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;

        return newWindow;
    }

    private void CleanupNewMainWindow(Window window, MainPageViewModel viewModel, IServiceScope scope)
    {
        _openWindows.Remove(window);
        viewModel.Dispose();
        scope.Dispose();
    }

    private void CleanupSettingsWindow(Window window, IServiceScope scope)
    {
        _openWindows.Remove(window);
        scope.Dispose();
    }

    private void OffsetWindowPosition(Window newWindow)
    {
        if (App.MainWindow != null)
        {
            var mainWindowPos = App.MainWindow.AppWindow.Position;

            var offsetX = mainWindowPos.X + WindowManagerConstants.WindowOffsetX;
            var offsetY = mainWindowPos.Y + WindowManagerConstants.WindowOffsetY;

            newWindow.AppWindow.Move(new Windows.Graphics.PointInt32(offsetX, offsetY));
        }
    }

    public void OpenStatisticsWindow(IMainPageViewModel viewModel)
    {
        var vm = (MainPageViewModel)viewModel;
        var control = new StatisticsControl { ViewModel = vm };
        CreateDetachedWindow(_localizationService.GetString("WindowTitle_Statistics"), control,
                WindowManagerConstants.StatisticsWindowWidth, WindowManagerConstants.StatisticsWindowHeight)
            .Activate();
    }

    public void OpenTreeViewWindow(IMainPageViewModel viewModel)
    {
        var vm = (MainPageViewModel)viewModel;
        var control = new TreeViewControl { ViewModel = vm };
        CreateDetachedWindow(_localizationService.GetString("WindowTitle_TreeView"), control,
                WindowManagerConstants.TreeViewWindowWidth, WindowManagerConstants.TreeViewWindowHeight)
            .Activate();
    }

    public void OpenTreeMapWindow(IMainPageViewModel viewModel)
    {
        var vm = (MainPageViewModel)viewModel;
        var control = new TreeMapControl { ViewModel = vm };
        CreateDetachedWindow(_localizationService.GetString("WindowTitle_TreeMap"), control,
                WindowManagerConstants.TreeMapWindowWidth, WindowManagerConstants.TreeMapWindowHeight)
            .Activate();
    }

    public void OpenSettingsWindow()
    {
        var scope = _scopeFactory.CreateScope();
        var window = scope.ServiceProvider.GetRequiredService<SettingsWindow>();
        var xamlRootProvider = scope.ServiceProvider.GetRequiredService<ICurrentXamlRootProvider>();
        
        window.Title = _localizationService.GetString("WindowTitle_Settings");

        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, WindowManagerConstants.MicaMinBuildNumber) && MicaController.IsSupported())
            window.SystemBackdrop = new MicaBackdrop();

        if (window.Content is FrameworkElement fe)
        {
            fe.RequestedTheme = _themeService.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
            fe.Loaded += (_, _) => xamlRootProvider.XamlRoot = fe.XamlRoot;
        }

        _openWindows.Add(window);
        window.Closed += (_, _) => CleanupSettingsWindow(window, scope);
        OffsetWindowPosition(window);
        window.Activate();
    }

    private IServiceScope? _rootWindowScope;
    
    public MainPage CreateScopedMainPage()
    {
        _rootWindowScope?.Dispose();
        var scope = _scopeFactory.CreateScope();
        _rootWindowScope = scope;

        var viewModel = scope.ServiceProvider.GetRequiredService<MainPageViewModel>();
        var xamlRootProvider = scope.ServiceProvider.GetRequiredService<ICurrentXamlRootProvider>();
        var mainPage = new MainPage(viewModel, xamlRootProvider);
        return mainPage;
    }
    
    public void SetRootWindowHandle(Window window)
    {
        if (_rootWindowScope is null) return;

        var handleProvider = _rootWindowScope.ServiceProvider.GetRequiredService<IWindowHandleProvider>();
        handleProvider.Hwnd = WindowNative.GetWindowHandle(window);
        
        if (window.Content is FrameworkElement fe)
        {
            fe.RequestedTheme = _themeService.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
        }
    }

    public void ReloadMainWindowContent()
    {
        foreach (var win in _openWindows.ToList())
        {
            win.Close();
        }

        _openWindows.Clear();

        if (App.MainWindow is not MainWindow window) return;

        window.CurrentPage?.ViewModel.Dispose();
        var previousResult = _rootWindowScope?.ServiceProvider.GetService<IScanStateService>()?.CurrentResult;
        
        _rootWindowScope?.Dispose();
        var scope = _scopeFactory.CreateScope();
        _rootWindowScope = scope;
        
        if (previousResult is not null)
                    scope.ServiceProvider.GetRequiredService<IScanStateService>().SetResult(previousResult);
        
        var viewModel = scope.ServiceProvider.GetRequiredService<MainPageViewModel>();
        App.RootViewModel = viewModel;
        scope.ServiceProvider.GetRequiredService<IWindowHandleProvider>().Hwnd = WindowNative.GetWindowHandle(window);

        var xamlRootProvider = scope.ServiceProvider.GetRequiredService<ICurrentXamlRootProvider>();
        var mainPage = new MainPage(viewModel, xamlRootProvider);
        window.SetContent(mainPage);
        
        if (window.Content is FrameworkElement fe)
        {
            fe.RequestedTheme = _themeService.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
        }
    }

    public void ExitApplication()
    {
        foreach (var win in _openWindows.ToList())
        {
            win.Close();
        }

        _openWindows.Clear();

        App.MainWindow?.Close();

        Application.Current.Exit();
    }
}