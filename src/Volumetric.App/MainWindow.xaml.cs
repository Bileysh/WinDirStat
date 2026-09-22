using System;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Volumetric.Core.Interfaces;
using Volumetric.ViewModels;

namespace Volumetric_App;

public sealed partial class MainWindow : Window
{
    private readonly ILocalizationService? _localizationService;

    public MainPageViewModel ViewModel { get; private set; }
    public ICommand RestoreWindowCommand { get; }
    public ICommand ExitCommand { get; }
    public ICommand ScanFromTrayCommand { get; }

    public MainWindow(MainPage mainPage)
    {
        ViewModel = mainPage.ViewModel;
        _localizationService = App.StaticServices?.GetService<ILocalizationService>();

        RestoreWindowCommand = new RelayCommand(RestoreWindow);
        ExitCommand = new RelayCommand(ExitApp);
        ScanFromTrayCommand = new RelayCommand(ScanFromTray);

        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        TrayIcon.Icon = new System.Drawing.Icon(iconPath);
        TrayIcon.ToolTipText = _localizationService?.GetString(ResourceKeys.TrayIconToolTipText) ?? "Volumetric";

        RootFrame.Content = mainPage;

        AppWindow.Changed += AppWindow_Changed;
        Activated += (_, _) => UpdateTitleBarInsets();
    }

    private void AppWindow_Changed(Microsoft.UI.Windowing.AppWindow sender,
        Microsoft.UI.Windowing.AppWindowChangedEventArgs args)
    {
        if (args.DidPresenterChange && sender.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
        {
            if (presenter.State == Microsoft.UI.Windowing.OverlappedPresenterState.Minimized)
            {
                sender.Hide();
            }
        }

        if (args.DidPresenterChange || args.DidSizeChange)
        {
            UpdateTitleBarInsets();
        }
    }

    private void UpdateTitleBarInsets()
    {
        if (!AppWindowTitleBar.IsCustomizationSupported())
        {
            return;
        }

        var titleBar = AppWindow.TitleBar;
        var scale = Content?.XamlRoot?.RasterizationScale ?? 1.0;
        var reservedInset = Math.Max(titleBar.RightInset, titleBar.LeftInset) / scale;

        SearchBox.Margin = new Thickness(0, 8, Math.Max(12, reservedInset), 8);
    }

    private void RestoreWindow()
    {
        AppWindow.Show();
        if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
        {
            presenter.Restore();
        }

        Activate();
    }

    private void ExitApp()
    {
        TrayIcon?.Dispose();
        Application.Current.Exit();
    }

    private void ScanFromTray()
    {
        RestoreWindow();
        if (ViewModel.OpenFolderCommand.CanExecute(null))
        {
            ViewModel.OpenFolderCommand.Execute(null);
        }
    }

    private void SearchBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ViewModel.SearchText = sender.Text;
        }
    }

    public void ShowNotification(string title, string message)
    {
        TrayIcon?.ShowNotification(title, message);
    }

    public MainPage? CurrentPage => RootFrame.Content as MainPage;

    public void SetContent(MainPage page)
    {
        RootFrame.Content = page;
        ViewModel = page.ViewModel;
        Bindings.Update();
    }
}
