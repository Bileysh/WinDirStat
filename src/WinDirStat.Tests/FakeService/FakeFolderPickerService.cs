using WinDirStat.Core.Interfaces;

namespace WinDirStat.Tests.FakeService;

public class FakeFolderPickerService : IFolderPickerService
{
    public string? PathToReturn { get; set; }
    public Task<string?> PickFolderAsync() => Task.FromResult(PathToReturn);
}