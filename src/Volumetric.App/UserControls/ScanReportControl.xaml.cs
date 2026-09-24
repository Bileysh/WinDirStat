using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;

namespace Volumetric_App.UserControls;

public sealed partial class ScanReportControl : UserControl
{
    private readonly string _html;
    private Task? _initializationTask;
    private bool _failureDisplayed;
    private string? _reportPath;

    public ScanReportControl(string html)
    {
        _html = html;
        InitializeComponent();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        ReportWebView.Close();
        DeleteReportFile();
    }

    private void DeleteReportFile()
    {
        if (_reportPath is null)
        {
            return;
        }

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
            await ReportWebView.EnsureCoreWebView2Async();

            _reportPath = Path.Combine(Path.GetTempPath(), $"volumetric-report-{Guid.NewGuid():N}.html");
            await File.WriteAllTextAsync(_reportPath, _html, new UTF8Encoding(true));
            ReportWebView.Source = new Uri(_reportPath);
        }
        catch (Exception ex)
        {
            _failureDisplayed = true;
            Log.Error(ex, "Failed to initialize WebView2 scan report.");
            ShowFailure(ex);
        }
    }

    private void ShowFailure(Exception exception)
    {
        var innerInfo = exception.InnerException is { } inner
            ? $"{Environment.NewLine}{inner.GetType().Name}: {inner.Message}"
            : string.Empty;

        FailureDetailsText.Text = $"{exception.GetType().Name} (0x{exception.HResult:X8}): {exception.Message}{innerInfo}";
        ReportWebView.Visibility = Visibility.Collapsed;
        FailurePanel.Visibility = Visibility.Visible;
    }
}
