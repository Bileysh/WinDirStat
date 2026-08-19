using WinDirStat.Core.Interfaces;

namespace WinDirStat.Tests.FakeService;

public class FakeDialogService : IDialogService
{
    public Task ShowMessageAsync(string title, string message, string closeButtonText = "OK")
    {
        return Task.CompletedTask;
    }
}