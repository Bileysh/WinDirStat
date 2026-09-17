using Microsoft.Windows.AppNotifications;
using Serilog;

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
            Log.Warning(ex, "[{CallerTag}] AppNotificationManager.Register() failed", callerTag);
            return false;
        }
    }
}
