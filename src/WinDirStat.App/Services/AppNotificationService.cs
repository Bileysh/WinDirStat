using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using WinDirStat.Core.Interfaces;

namespace WinDirStat_App.Services;

public class AppNotificationService : INotificationService, IDisposable
{
    private bool _isRegistered;

    public AppNotificationService()
    {
        AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;

        try
        {
            AppNotificationManager.Default.Register();
            _isRegistered = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppNotificationService] Register failed: {ex}");
        }
    }

    private void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
    {
        // TODO: маршрутизація за args.Argument, коли з'являться клікабельні дії в тостах.
    }

    public void ShowScanComplete(TimeSpan elapsed, int fileCount, int folderCount)
    {
        if (!_isRegistered) return;

        var notification = new AppNotificationBuilder()
            .AddText("Сканування завершено")
            .AddText($"Час: {elapsed.TotalSeconds:F1} сек. Файлів: {fileCount} Папок: {folderCount}")
            .BuildNotification();

        AppNotificationManager.Default.Show(notification);
    }

    public void ShowNotification(string title, string message)
    {
        if (!_isRegistered) return;

        var notification = new AppNotificationBuilder()
            .AddText(title)
            .AddText(message)
            .BuildNotification();

        AppNotificationManager.Default.Show(notification);
    }

    public void Dispose()
    {
        AppNotificationManager.Default.NotificationInvoked -= OnNotificationInvoked;
        if (_isRegistered)
        {
            AppNotificationManager.Default.Unregister();
        }
    }
}