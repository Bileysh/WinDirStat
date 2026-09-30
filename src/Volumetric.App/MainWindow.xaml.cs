using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
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
    private const long TrayUpdateThrottleMs = 500;

    private readonly ILocalizationService? _localizationService;
    private readonly System.Drawing.Icon _idleTrayIcon;
    private readonly System.Drawing.Icon _scanningTrayIcon;
    private readonly IntPtr _scanningTrayIconHandle;
    private long _lastTrayUpdateTicks;

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
        ApplyTransparentTitleBarButtons();

        AppWindow.SetIcon("Assets/AppIcon.ico");

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        _idleTrayIcon = new System.Drawing.Icon(iconPath);
        (_scanningTrayIcon, _scanningTrayIconHandle) = CreateScanningTrayIcon(_idleTrayIcon);
        TrayIcon.Icon = _idleTrayIcon;

        RootFrame.Content = mainPage;

        AttachViewModelEvents(ViewModel);
        UpdateTrayStatus();

        AppWindow.Changed += AppWindow_Changed;
        Activated += (_, _) => UpdateTitleBarInsets();
    }

    private void AttachViewModelEvents(MainPageViewModel viewModel)
    {
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void DetachViewModelEvents(MainPageViewModel viewModel)
    {
        viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainPageViewModel.IsScanning))
        {
            UpdateTrayStatus();
            return;
        }

        if (e.PropertyName is nameof(MainPageViewModel.ScanFilesCount) or nameof(MainPageViewModel.ScanFoldersCount))
        {
            var now = Environment.TickCount64;
            if (now - _lastTrayUpdateTicks < TrayUpdateThrottleMs)
            {
                return;
            }

            UpdateTrayStatus();
        }
    }

    private void UpdateTrayStatus()
    {
        _lastTrayUpdateTicks = Environment.TickCount64;
        var sourceIcon = ViewModel.IsScanning ? _scanningTrayIcon : _idleTrayIcon;
        TrayIcon.Icon = (System.Drawing.Icon)sourceIcon.Clone();

        var idleTooltip = _localizationService?.GetString(ResourceKeys.TrayIconToolTipText) ?? "Volumetric";
        var scanningFormat = _localizationService?.GetString(ResourceKeys.TrayIconScanningToolTipFormat)
                              ?? "Volumetric — Scanning… Files: {0:N0}  Folders: {1:N0}";

        TrayIcon.ToolTipText = TrayIconStatusFormatter.BuildTooltip(ViewModel.IsScanning, ViewModel.ScanFilesCount,
            ViewModel.ScanFoldersCount, idleTooltip, scanningFormat);
    }

    private static (System.Drawing.Icon Icon, IntPtr Handle) CreateScanningTrayIcon(System.Drawing.Icon baseIcon)
    {
        using var bitmap = baseIcon.ToBitmap();
        using var graphics = System.Drawing.Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var badgeSize = bitmap.Width / 2;
        var badgeRect = new System.Drawing.Rectangle(bitmap.Width - badgeSize, bitmap.Height - badgeSize, badgeSize,
            badgeSize);

        using var badgeBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 16, 124, 16));
        graphics.FillEllipse(badgeBrush, badgeRect);
        using var borderPen = new System.Drawing.Pen(System.Drawing.Color.White, 1.5f);
        graphics.DrawEllipse(borderPen, badgeRect);

        var handle = bitmap.GetHicon();
        return (System.Drawing.Icon.FromHandle(handle), handle);
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyIcon(IntPtr hIcon);

    private void ApplyTransparentTitleBarButtons()
    {
        if (!AppWindowTitleBar.IsCustomizationSupported())
        {
            return;
        }

        var titleBar = AppWindow.TitleBar;
        titleBar.BackgroundColor = Microsoft.UI.Colors.Transparent;
        titleBar.InactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
        titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
        titleBar.ButtonHoverBackgroundColor = Microsoft.UI.Colors.Transparent;
        titleBar.ButtonPressedBackgroundColor = Microsoft.UI.Colors.Transparent;
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

        try
        {
            var titleBar = AppWindow.TitleBar;
            var scale = Content?.XamlRoot?.RasterizationScale ?? 1.0;
            var reservedInset = Math.Max(titleBar.RightInset, titleBar.LeftInset) / scale;

            SearchBox.Margin = new Thickness(0, 8, Math.Max(12, reservedInset), 8);
        }
        catch (COMException)
        {
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
        _idleTrayIcon.Dispose();
        _scanningTrayIcon.Dispose();
        DestroyIcon(_scanningTrayIconHandle);
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
        DetachViewModelEvents(ViewModel);
        RootFrame.Content = page;
        ViewModel = page.ViewModel;
        Bindings.Update();
        AttachViewModelEvents(ViewModel);
        UpdateTrayStatus();
    }
}
