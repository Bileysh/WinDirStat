using Volumetric.Core.Interfaces;

namespace Volumetric.Tests.FakeService;

public class FakeDialogService : IDialogService
{
    public string? LastTitle { get; private set; }
    public string? LastMessage { get; private set; }

    public Task ShowMessageAsync(string title, string message, string closeButtonText = "OK")
    {
        LastTitle = title;
        LastMessage = message;
        return Task.CompletedTask;
    }
}
