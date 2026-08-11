using WinDirStat.Services;
using WinDirStat.Tests.FakeService;
using WinDirStat.ViewModels;

namespace WinDirStat.Tests.Tests;

public class MainPageViewModelTests
{
    [Fact]
    public async Task OpenFolderAsync_WhenFolderSelected_PopulatesRootNodes()
    {
        var tempFolder = Directory.CreateTempSubdirectory();
        var vm = new MainPageViewModel(
            new DiskScanService(),
            new FakeFolderPickerService { PathToReturn = tempFolder.FullName });

        await vm.OpenFolderCommand.ExecuteAsync(null);

        Assert.Single(vm.RootNodes);
    }
}