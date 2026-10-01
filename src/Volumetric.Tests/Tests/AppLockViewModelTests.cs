using Volumetric.Core.Interfaces;
using Volumetric.Tests.FakeService;
using Volumetric.ViewModels;

namespace Volumetric.Tests.Tests;

public class AppLockViewModelTests
{
    private readonly FakeWindowsHelloService _hello = new();
    private readonly FakeSecuritySettingsService _settings = new();

    private AppLockViewModel CreateViewModel() =>
        new(_hello, _settings, new FakeLocalizationService(), new FakeAppLogger());

    [Fact]
    public async Task Initialize_SettingDisabled_StaysUnlockedWithoutPrompting()
    {
        var vm = CreateViewModel();

        await vm.InitializeAsync();

        Assert.False(vm.IsLocked);
        Assert.Equal(0, _hello.VerificationRequests);
    }

    [Fact]
    public void Constructor_SettingEnabled_StartsLockedBeforeAnyCheck()
    {
        _settings.RequireWindowsHelloOnLaunch = true;

        var vm = CreateViewModel();

        Assert.True(vm.IsLocked);
        Assert.False(vm.IsUnlocked);
    }

    [Fact]
    public async Task Initialize_VerifiedOnFirstPrompt_Unlocks()
    {
        _settings.RequireWindowsHelloOnLaunch = true;
        _hello.EnqueueVerificationResults(WindowsHelloVerificationResult.Verified);
        var vm = CreateViewModel();
        vm.WindowHandle = 42;

        await vm.InitializeAsync();

        Assert.False(vm.IsLocked);
        Assert.Equal(1, _hello.VerificationRequests);
        Assert.Equal((nint)42, _hello.LastOwnerHwnd);
    }

    [Theory]
    [InlineData(WindowsHelloVerificationResult.Canceled)]
    [InlineData(WindowsHelloVerificationResult.Failed)]
    public async Task Initialize_PromptNotPassed_StaysLockedAndExplains(WindowsHelloVerificationResult result)
    {
        _settings.RequireWindowsHelloOnLaunch = true;
        _hello.EnqueueVerificationResults(result);
        var vm = CreateViewModel();

        await vm.InitializeAsync();

        Assert.True(vm.IsLocked);
        Assert.Equal(ResourceKeys.WindowsHelloVerificationFailed, vm.StatusMessage);
    }

    [Fact]
    public async Task Unlock_AfterFailedLaunchPrompt_UnlocksOnSuccessfulRetry()
    {
        _settings.RequireWindowsHelloOnLaunch = true;
        _hello.EnqueueVerificationResults(WindowsHelloVerificationResult.Canceled,
            WindowsHelloVerificationResult.Verified);
        var vm = CreateViewModel();
        await vm.InitializeAsync();

        await vm.UnlockCommand.ExecuteAsync(null);

        Assert.False(vm.IsLocked);
        Assert.Null(vm.StatusMessage);
    }

    [Theory]
    [InlineData(WindowsHelloAvailability.DeviceNotPresent)]
    [InlineData(WindowsHelloAvailability.NotConfiguredForUser)]
    [InlineData(WindowsHelloAvailability.DisabledByPolicy)]
    [InlineData(WindowsHelloAvailability.Unknown)]
    public async Task Initialize_HelloUnavailable_OpensUnlockedWithoutPrompting(WindowsHelloAvailability availability)
    {
        _settings.RequireWindowsHelloOnLaunch = true;
        _hello.Availability = availability;
        var vm = CreateViewModel();

        await vm.InitializeAsync();

        Assert.False(vm.IsLocked);
        Assert.Equal(0, _hello.VerificationRequests);
    }

    [Fact]
    public async Task Initialize_HelloBecomesUnavailableDuringPrompt_OpensUnlocked()
    {
        _settings.RequireWindowsHelloOnLaunch = true;
        _hello.EnqueueVerificationResults(WindowsHelloVerificationResult.Unavailable);
        var vm = CreateViewModel();

        await vm.InitializeAsync();

        Assert.False(vm.IsLocked);
    }
}
