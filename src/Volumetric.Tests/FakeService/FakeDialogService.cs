using Volumetric.Core.Interfaces;

namespace Volumetric.Tests.FakeService;

public class FakeDialogService : IDialogService
{
    public Task ShowMessageAsync(string title, string message, string closeButtonText = "OK")
    {
        return Task.CompletedTask;
    }
}
