using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Text;
using Serilog;

namespace Volumetric_App.UserControls;

public sealed partial class ScanReportControl : UserControl
{
    private readonly WebView2 _webView = new();
    private readonly string _html;
    private Task? _initializationTask;
    private bool _failureDisplayed;
    private string? _reportPath;

    public ScanReportControl(string html)
    {
        _html = html;
        Content = _webView;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _webView.Close();
        DeleteReportFile();
    }

    private void DeleteReportFile()
    {
        if (_reportPath is null) return;

        try
        {
            File.Delete(_reportPath);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Could not delete temporary scan report file {Path}.", _reportPath);
        }

        _reportPath = null;
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
            await _webView.EnsureCoreWebView2Async();

            _reportPath = Path.Combine(Path.GetTempPath(), $"volumetric-report-{Guid.NewGuid():N}.html");
            await File.WriteAllTextAsync(_reportPath, _html, new UTF8Encoding(true));
            _webView.Source = new Uri(_reportPath);
        }
        catch (Exception ex)
        {
            _failureDisplayed = true;
            Log.Error(ex, "Failed to initialize WebView2 scan report.");
            Content = CreateFailureView(ex);
        }
    }

    private static TextBlock CreateFailureView(Exception exception)
    {
        var innerInfo = exception.InnerException is { } inner
            ? $"\nInner: {inner.GetType().Name}: {inner.Message}"
            : "";

        return new TextBlock
        {
            Text = "Unable to load the scan report because WebView2 could not be initialized.\n\n" +
                   "Verify that the Microsoft Edge WebView2 Runtime is installed, then restart Volumetric.\n" +
                   "Download: https://developer.microsoft.com/microsoft-edge/webview2/\n\n" +
                   $"Technical details: {exception.GetType().Name} (0x{exception.HResult:X8}): {exception.Message}" +
                   innerInfo,
            Margin = new Thickness(16),
            TextWrapping = TextWrapping.Wrap,
            IsTextSelectionEnabled = true
        };
    }
}
