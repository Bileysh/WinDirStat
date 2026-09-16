using Volumetric.Core.Interfaces;

namespace Volumetric_App.Services;

public class WinUiNotificationService : INotificationService
{
    public void ShowNotification(string? title, string? message, string? path = null)
    {
        if (App.MainWindow is MainWindow win)
        {
            win.ShowNotification(title, message);
        }
    }
}