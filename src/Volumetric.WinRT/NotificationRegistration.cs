using Microsoft.Windows.AppNotifications;

namespace Volumetric.WinRT;

public static class NotificationRegistration
{
    public static bool TryRegister(string callerTag)
    {
        try
        {
            AppNotificationManager.Default.Register();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[{callerTag}] AppNotificationManager.Register() failed: {ex}");
            return false;
        }
    }
}
