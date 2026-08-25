using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using WinDirStat.Core.Interfaces;
using WinDirStat.ViewModels;
using WinDirStat_App.UserControls;
using System.Diagnostics;

namespace WinDirStat_App.Services;

public class WindowManagerService : IWindowManagerService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localizationService;
    private readonly List<Window> _openWindows = new();

    public WindowManagerService(IServiceProvider serviceProvider, IThemeService themeService,
        ILocalizationService localizationService)
    {
        _serviceProvider = serviceProvider;
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

    public void OpenMainWindow()
    {
        var newWindow = new Window { ExtendsContentIntoTitleBar = true };
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, WindowManagerConstants.MicaMinBuildNumber) &&
            MicaController.IsSupported())
            newWindow.SystemBackdrop = new MicaBackdrop();

        var viewModel = _serviceProvider.GetRequiredService<MainPageViewModel>();
        var page = new MainPage(viewModel);

        page.RequestedTheme = _themeService.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;

        newWindow.Content = page;

        newWindow.Title = _localizationService.GetString("WindowTitle_New");

        _openWindows.Add(newWindow);
        newWindow.Closed += (_, _) =>
        {
            _openWindows.Remove(newWindow);
            viewModel.Dispose();
        };
        OffsetWindowPosition(newWindow);

        newWindow.Activate();
    }

    private Window CreateDetachedWindow(string title, FrameworkElement content, int width, int height,
        MainPageViewModel viewModel)
    {
        var newWindow = new Window { ExtendsContentIntoTitleBar = true };
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, WindowManagerConstants.MicaMinBuildNumber) &&
            MicaController.IsSupported())
            newWindow.SystemBackdrop = new MicaBackdrop();

        var rootGrid = (Grid)XamlReader.Load(
            "<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' " +
            "Background='{ThemeResource ApplicationPageBackgroundThemeBrush}' />");

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

        rootGrid.RequestedTheme = _themeService.IsDarkTheme ? ElementTheme.Dark : ElementTheme.Light;

        newWindow.Content = rootGrid;
        newWindow.Title = $"WinDirStat - {title}";
        _openWindows.Add(newWindow);
        newWindow.Closed += (_, _) =>
        {
            _openWindows.Remove(newWindow);
            viewModel.Dispose();
        };
        newWindow.AppWindow.Resize(new Windows.Graphics.SizeInt32(width, height));

        OffsetWindowPosition(newWindow);

        return newWindow;
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

    public void OpenStatisticsWindow()
    {
        var viewModel = _serviceProvider.GetRequiredService<MainPageViewModel>();
        var control = new StatisticsControl { ViewModel = viewModel };
        CreateDetachedWindow(_localizationService.GetString("WindowTitle_Statistics"), control,
                WindowManagerConstants.StatisticsWindowWidth, WindowManagerConstants.StatisticsWindowHeight, viewModel)
            .Activate();
    }

    public void OpenTreeViewWindow()
    {
        var viewModel = _serviceProvider.GetRequiredService<MainPageViewModel>();
        var control = new TreeViewControl { ViewModel = viewModel };
        CreateDetachedWindow(_localizationService.GetString("WindowTitle_TreeView"), control,
                WindowManagerConstants.TreeViewWindowWidth, WindowManagerConstants.TreeViewWindowHeight, viewModel)
            .Activate();
    }

    public void OpenTreeMapWindow()
    {
        var viewModel = _serviceProvider.GetRequiredService<MainPageViewModel>();
        var control = new TreeMapControl { ViewModel = viewModel };
        CreateDetachedWindow(_localizationService.GetString("WindowTitle_TreeMap"), control,
                WindowManagerConstants.TreeMapWindowWidth, WindowManagerConstants.TreeMapWindowHeight, viewModel)
            .Activate();
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

        var viewModel = _serviceProvider.GetRequiredService<MainPageViewModel>();

        window.SetContent(new MainPage(viewModel));
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