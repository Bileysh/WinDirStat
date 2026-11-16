using Volumetric.Core.Interfaces;
using Volumetric.Tests.FakeService;
using Volumetric.ViewModels;

namespace Volumetric.Tests.Tests;

public class SettingsViewModelSecurityTests
{
    private readonly FakeWindowsHelloService _hello = new();
    private readonly FakeSecuritySettingsService _security = new();

    private SettingsViewModel CreateViewModel() =>
        new(new FakeBackgroundScanSettingsService(), new NoOpRegistrar(), new NoOpSettingsFileService(),
            new NoOpTestRunner(), new FakeLocalizationService(), _security, _hello);

    [Fact]
    public void EncryptScanResults_Toggled_IsPersisted()
    {
        var vm = CreateViewModel();

        vm.EncryptScanResults = true;

        Assert.True(_security.EncryptScanResults);
        Assert.Equal(ResourceKeys.EncryptScanResultsEnabledStatus, vm.StatusMessage);
    }

    [Fact]
    public void RequireWindowsHello_EnabledAndVerified_IsPersisted()
    {
        _hello.EnqueueVerificationResults(WindowsHelloVerificationResult.Verified);
        var vm = CreateViewModel();

        vm.RequireWindowsHelloOnLaunch = true;

        Assert.True(vm.RequireWindowsHelloOnLaunch);
        Assert.True(_security.RequireWindowsHelloOnLaunch);
        Assert.Equal(1, _hello.VerificationRequests);
    }

    [Theory]
    [InlineData(WindowsHelloVerificationResult.Canceled)]
    [InlineData(WindowsHelloVerificationResult.Failed)]
    [InlineData(WindowsHelloVerificationResult.Unavailable)]
    public void RequireWindowsHello_EnabledButNotVerified_RevertsToggle(WindowsHelloVerificationResult result)
    {
        _hello.EnqueueVerificationResults(result);
        var vm = CreateViewModel();

        vm.RequireWindowsHelloOnLaunch = true;

        Assert.False(vm.RequireWindowsHelloOnLaunch);
        Assert.False(_security.RequireWindowsHelloOnLaunch);
        Assert.Equal(ResourceKeys.WindowsHelloVerificationFailed, vm.StatusMessage);
        Assert.Equal(1, _hello.VerificationRequests);
    }

    [Fact]
    public void RequireWindowsHello_Disabled_DoesNotPrompt()
    {
        _security.RequireWindowsHelloOnLaunch = true;
        var vm = CreateViewModel();

        vm.RequireWindowsHelloOnLaunch = false;

        Assert.False(_security.RequireWindowsHelloOnLaunch);
        Assert.Equal(0, _hello.VerificationRequests);
    }

    [Fact]
    public async Task LoadAvailability_Available_EnablesToggleWithoutReason()
    {
        var vm = CreateViewModel();

        await vm.LoadWindowsHelloAvailabilityAsync();

        Assert.True(vm.IsWindowsHelloAvailable);
        Assert.True(vm.IsWindowsHelloToggleEnabled);
        Assert.Null(vm.WindowsHelloUnavailableReason);
    }

    [Fact]
    public async Task LoadAvailability_DeviceNotPresent_DisablesToggleAndExplains()
    {
        _hello.Availability = WindowsHelloAvailability.DeviceNotPresent;
        var vm = CreateViewModel();

        await vm.LoadWindowsHelloAvailabilityAsync();

        Assert.False(vm.IsWindowsHelloToggleEnabled);
        Assert.Equal("WindowsHelloUnavailable_DeviceNotPresent", vm.WindowsHelloUnavailableReason);
    }

    [Fact]
    public async Task LoadAvailability_UnavailableButAlreadyRequired_KeepsToggleEnabledSoItCanBeTurnedOff()
    {
        _security.RequireWindowsHelloOnLaunch = true;
        _hello.Availability = WindowsHelloAvailability.NotConfiguredForUser;
        var vm = CreateViewModel();

        await vm.LoadWindowsHelloAvailabilityAsync();

        Assert.True(vm.IsWindowsHelloToggleEnabled);
    }

    [Fact]
    public async Task Import_UpdatesEncryptionToggleAndReportsTheImportedFile()
    {
        var files = new NoOpSettingsFileService
        {
            ImportResult = ("""{"ScanIntervalMinutes":30,"LowFreeSpaceThresholdPercent":10,"EncryptScanResults":true}""",
                "team-settings.json")
        };
        var vm = new SettingsViewModel(new FakeBackgroundScanSettingsService(), new NoOpRegistrar(), files,
            new NoOpTestRunner(), new FakeLocalizationService(), _security, _hello);

        await vm.ImportCommand.ExecuteAsync(null);

        Assert.True(vm.EncryptScanResults);
        Assert.True(_security.EncryptScanResults);
        Assert.Equal(ResourceKeys.SettingsImportedStatus, vm.StatusMessage);
    }

    private sealed class NoOpRegistrar : IBackgroundScanTaskRegistrar
    {
        public void EnsureRegistered()
        {
        }

        public void ReRegister()
        {
        }
    }

    private sealed class NoOpSettingsFileService : ISettingsFileService
    {
        public (string Json, string FileName)? ImportResult { get; init; }

        public Task<string?> ExportAsync(string json, string suggestedFileName, IntPtr ownerHwnd) =>
            Task.FromResult<string?>(null);

        public Task<(string Json, string FileName)?> ImportAsync(IntPtr ownerHwnd) => Task.FromResult(ImportResult);
    }

    private sealed class NoOpTestRunner : IBackgroundScanTestRunner
    {
        public void RunNow()
        {
        }
    }
}
