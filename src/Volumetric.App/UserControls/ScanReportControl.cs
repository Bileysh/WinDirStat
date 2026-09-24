﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Serilog;
using Windows.Storage;


namespace Volumetric_App.UserControls;

public sealed partial class ScanReportControl : UserControl
{
    private static readonly Lazy<Task<CoreWebView2Environment>> EnvironmentTask = new(CreateEnvironmentAsync);
    private readonly WebView2 _webView = new();
    private readonly string _html;
    private Task? _initializationTask;
    private bool _failureDisplayed;

    public ScanReportControl(string html)
    {
        _html = html;
        Content = _webView;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        // The window that hosts this control is closing - without this, the
        // underlying CoreWebView2 browser process leaks for the app's lifetime.
        _webView.Close();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_initializationTask is null && !_failureDisplayed)
        {
            _initializationTask = InitializeAsync();
        }
    }

    private async Task InitializeAsync()
    {
        try
        {
            var environment = await EnvironmentTask.Value;
            await _webView.EnsureCoreWebView2Async(environment);
            _webView.NavigateToString(_html);
        }
        catch (Exception ex)
        {
            _failureDisplayed = true;
            Log.Error(ex, "Failed to initialize WebView2 scan report.");
            Content = CreateFailureView(ex);
        }
    }


    private static async Task<CoreWebView2Environment> CreateEnvironmentAsync()
    {
        // WebView2 defaults to a user-data folder next to the exe, which is read-only
        // for an installed MSIX package (Program Files\WindowsApps\...) - it must be
        // pointed at somewhere the app can actually write, hence LocalFolder here.
        var userDataFolder = Path.Combine(ApplicationData.Current.LocalFolder.Path, "WebView2", "ScanReport");

        Directory.CreateDirectory(userDataFolder);

        return await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
    }

    private static TextBlock CreateFailureView(Exception exception) => new()
    {
        Text = "Unable to load the scan report because WebView2 could not be initialized.\n\n" +
               "Verify that the Microsoft Edge WebView2 Runtime is installed, then restart Volumetric.\n" +
               "Download: https://developer.microsoft.com/microsoft-edge/webview2/\n\n" +
               $"Technical details: {exception.Message}",
        Margin = new Thickness(16),
        TextWrapping = TextWrapping.Wrap,
        IsTextSelectionEnabled = true
    };
}
