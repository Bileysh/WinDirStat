using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Volumetric.Core.Interfaces;

namespace Volumetric_App.Services;

public class AppNotificationService : INotificationService
{
    public void ShowNotification(string? title, string? message, string? path = null)
    {
        if (!Program.NotificationsRegistered) return;

        var builder = new AppNotificationBuilder()
            .AddText(title)
            .AddText(message);

        if (!string.IsNullOrEmpty(path))
        {
            builder.AddArgument("path", path);
        }

        AppNotificationManager.Default.Show(builder.BuildNotification());
    }
}
