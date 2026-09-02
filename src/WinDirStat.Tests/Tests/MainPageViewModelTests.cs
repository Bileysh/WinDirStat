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
            new DiskScanService(new FileIdentityService()),
            new FakeFolderPickerService { PathToReturn = tempFolder.FullName },
            new ScanStateService(),
            new FakeWindowManagerService(),
            new FakeDialogService(),
            new FakeLocalizationService(),
            new FakeThemeService(),
            new FakeNotificationService(),
            new DriveInfoService(),
            new FakeClipboardService(), new FakeFileExplorerService());
        await vm.OpenFolderCommand.ExecuteAsync(null);

        Assert.Single(vm.RootNodes);
    }

    [Fact]
    public async Task TwoViewModels_WithIndependentScanStateServices_DoNotLeakResultsIntoEachOther()
    {
        var folderA = Directory.CreateTempSubdirectory();
        var folderB = Directory.CreateTempSubdirectory();

        var vmA = new MainPageViewModel(
            new DiskScanService(new FileIdentityService()),
            new FakeFolderPickerService { PathToReturn = folderA.FullName },
            new ScanStateService(),
            new FakeWindowManagerService(),
            new FakeDialogService(),
            new FakeLocalizationService(),
            new FakeThemeService(),
            new FakeNotificationService(),
            new DriveInfoService(),
            new FakeClipboardService(), new FakeFileExplorerService());

        var vmB = new MainPageViewModel(
            new DiskScanService(new FileIdentityService()),
            new FakeFolderPickerService { PathToReturn = folderB.FullName },
            new ScanStateService(),
            new FakeWindowManagerService(),
            new FakeDialogService(),
            new FakeLocalizationService(),
            new FakeThemeService(),
            new FakeNotificationService(),
            new DriveInfoService(),
            new FakeClipboardService(), new FakeFileExplorerService());

        await vmA.OpenFolderCommand.ExecuteAsync(null);

        Assert.Single(vmA.RootNodes);
        Assert.Empty(vmB.RootNodes);
        Assert.False(vmB.IsScanning);
    }
}