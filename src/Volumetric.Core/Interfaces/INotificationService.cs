namespace Volumetric.Core.Interfaces;

public interface INotificationService
{
    void ShowNotification(string? title, string? message, string? path = null);
}