using WinDirStat.Core.Interfaces;

namespace WinDirStat_App.Services;

public class WinUiNotificationService : INotificationService
{
    public void ShowNotification(string title, string message)
    {
        if (App.MainWindow is MainWindow win)
        {
            win.ShowNotification(title, message);
        }
    }
}