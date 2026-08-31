using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using WinDirStat.Core.Interfaces;
using WinDirStat.WinRT;

namespace WinDirStat_App.Services;

public class AppNotificationService : INotificationService, IDisposable
{
    private readonly bool _isRegistered;

    public AppNotificationService()
    {
        AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;
        _isRegistered = NotificationRegistration.TryRegister("AppNotificationService");
    }

    private void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
    {
        // TODO: route based on args.Argument once clickable toast actions are added.
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