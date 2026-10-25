using Volumetric.Services;
using Volumetric.Core.Entities;
using Volumetric.Tests.FakeService;
using Volumetric.ViewModels;

namespace Volumetric.Tests.Tests;

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
            new FakeClipboardService(),
            new FakeFileExplorerService(),
            new FakeBackgroundScanSettingsService(),
            new FakeScanResultFileService(),
            new FakeWindowHandleProvider(),
            new FakeAppLogger(),
            new FakeRecentScansService(),
            new FakeScanReportService());

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
            new FakeClipboardService(),
            new FakeFileExplorerService(),
            new FakeBackgroundScanSettingsService(),
            new FakeScanResultFileService(),
            new FakeWindowHandleProvider(),
            new FakeAppLogger(),
            new FakeRecentScansService(),
            new FakeScanReportService());

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
            new FakeClipboardService(),
            new FakeFileExplorerService(),
            new FakeBackgroundScanSettingsService(),
            new FakeScanResultFileService(),
            new FakeWindowHandleProvider(),
            new FakeAppLogger(),
            new FakeRecentScansService(),
            new FakeScanReportService());

        await vmA.OpenFolderCommand.ExecuteAsync(null);

        Assert.Single(vmA.RootNodes);
        Assert.Empty(vmB.RootNodes);
        Assert.False(vmB.IsScanning);
    }

    [Fact]
    public async Task OpenScanReportAsync_WithScanResult_OpensWindowWithGeneratedHtml()
    {
        var fakeWindowManager = new FakeWindowManagerService();
        var fakeReportService = new FakeScanReportService { ReportHtmlToReturn = "<html><body>report</body></html>" };
        var vm = new MainPageViewModel(
            new DiskScanService(new FileIdentityService()),
            new FakeFolderPickerService(),
            new ScanStateService(),
            fakeWindowManager,
            new FakeDialogService(),
            new FakeLocalizationService(),
            new FakeThemeService(),
            new FakeNotificationService(),
            new DriveInfoService(),
            new FakeClipboardService(),
            new FakeFileExplorerService(),
            new FakeBackgroundScanSettingsService(),
            new FakeScanResultFileService(),
            new FakeWindowHandleProvider(),
            new FakeAppLogger(),
            new FakeRecentScansService(),
            fakeReportService);

        var root = new FileSystemNode
        {
            Name = "C:\\",
            IsDirectory = true,
            SizeLogical = 1
        };
        var child = new FileSystemNode
        {
            Name = "a.txt",
            IsDirectory = false,
            Extension = ".txt",
            SizeLogical = 1
        };
        root.AddChild(child);
        vm.LoadImportedResult(root);

        await vm.OpenScanReportCommand.ExecuteAsync(null);

        Assert.NotNull(fakeReportService.LastResult);
        Assert.Equal(fakeReportService.ReportHtmlToReturn, fakeWindowManager.LastReportHtml);
    }
}
