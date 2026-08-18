using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinDirStat.Core.Interfaces;
using WinDirStat.ViewModels;
using WinDirStat_App.UserControls;

namespace WinDirStat_App.Services;

public class WindowManagerService : IWindowManagerService
{
    private readonly IServiceProvider _serviceProvider;

    public WindowManagerService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void OpenMainWindow()
    {
        var newWindow = new Window { ExtendsContentIntoTitleBar = true };
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, WindowManagerConstants.MicaMinBuildNumber) && MicaController.IsSupported())
            newWindow.SystemBackdrop = new MicaBackdrop();

        var viewModel = _serviceProvider.GetRequiredService<MainPageViewModel>();
        newWindow.Content = new MainPage(viewModel);
        newWindow.Title = "WinDirStat - Нове вікно";
        newWindow.Closed += (_, _) => viewModel.Dispose();

        OffsetWindowPosition(newWindow);

        newWindow.Activate();
    }

    private Window CreateDetachedWindow(string title, FrameworkElement content, int width, int height,
        MainPageViewModel viewModel)
    {
        var newWindow = new Window { ExtendsContentIntoTitleBar = true };
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, WindowManagerConstants.MicaMinBuildNumber) && MicaController.IsSupported())
            newWindow.SystemBackdrop = new MicaBackdrop();

        var rootGrid = new Grid();
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(WindowManagerConstants.TitleBarRowHeight) });
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
        newWindow.Title = $"WinDirStat - {title}";
        newWindow.Closed += (_, _) => viewModel.Dispose();
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
        CreateDetachedWindow("Статистика (Відкріплено)", control,
            WindowManagerConstants.StatisticsWindowWidth, WindowManagerConstants.StatisticsWindowHeight, viewModel).Activate();
    }

    public void OpenTreeViewWindow()
    {
        var viewModel = _serviceProvider.GetRequiredService<MainPageViewModel>();
        var control = new TreeViewControl { ViewModel = viewModel };
        CreateDetachedWindow("Дерево файлів (Відкріплено)", control,
            WindowManagerConstants.TreeViewWindowWidth, WindowManagerConstants.TreeViewWindowHeight, viewModel).Activate();
    }

    public void OpenTreeMapWindow()
    {
        var viewModel = _serviceProvider.GetRequiredService<MainPageViewModel>();
        var control = new TreeMapControl { ViewModel = viewModel };
        CreateDetachedWindow("TreeMap (Відкріплено)", control,
            WindowManagerConstants.TreeMapWindowWidth, WindowManagerConstants.TreeMapWindowHeight, viewModel).Activate();
    }
}