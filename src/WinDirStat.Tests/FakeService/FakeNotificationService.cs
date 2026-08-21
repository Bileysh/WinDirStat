using WinDirStat.Core.Interfaces;

namespace WinDirStat.Tests.FakeService;

public class FakeNotificationService : INotificationService
{
    public void ShowNotification(string title, string message)
    {
    }
}