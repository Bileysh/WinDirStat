using System;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using CommunityToolkit.Mvvm.Input;

namespace WinDirStat_App;

public sealed partial class MainWindow : Window
{
    public ICommand RestoreWindowCommand { get; }
    public ICommand ExitCommand { get; }

    public MainWindow(MainPage mainPage)
    {
        RestoreWindowCommand = new RelayCommand(RestoreWindow);
        ExitCommand = new RelayCommand(ExitApp);

        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        TrayIcon.Icon = new System.Drawing.Icon(iconPath);

        RootFrame.Content = mainPage;

        AppWindow.Changed += AppWindow_Changed;
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

    public void ShowNotification(string title, string message)
    {
        TrayIcon?.ShowNotification(title, message);
    }
    
    public MainPage? CurrentPage => RootFrame.Content as MainPage;

    public void SetContent(MainPage page) => RootFrame.Content = page;
}