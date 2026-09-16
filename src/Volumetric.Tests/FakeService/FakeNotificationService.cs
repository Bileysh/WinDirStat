using Volumetric.Core.Interfaces;

namespace Volumetric.Tests.FakeService;

public class FakeNotificationService : INotificationService
{
    public string? LastTitle { get; private set; }
    public string? LastMessage { get; private set; }
    public string? LastPath { get; private set; }

    public void ShowNotification(string? title, string? message, string? path = null)
    {
        LastTitle = title;
        LastMessage = message;
        LastPath = path;
    }
}