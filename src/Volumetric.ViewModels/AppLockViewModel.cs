using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Volumetric.Core.Interfaces;

namespace Volumetric.ViewModels;

public partial class AppLockViewModel : ObservableObject
{
    private readonly IWindowsHelloService _windowsHello;
    private readonly ILocalizationService _localizationService;
    private readonly IAppLogger _logger;

    public AppLockViewModel(IWindowsHelloService windowsHello, ISecuritySettingsService securitySettings,
        ILocalizationService localizationService, IAppLogger logger)
    {
        _windowsHello = windowsHello;
        _localizationService = localizationService;
        _logger = logger;

        IsLocked = securitySettings.RequireWindowsHelloOnLaunch;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUnlocked))]
    public partial bool IsLocked { get; private set; }

    public bool IsUnlocked => !IsLocked;

    [ObservableProperty]
    public partial string? StatusMessage { get; private set; }

    public IntPtr WindowHandle { get; set; }

    public async Task InitializeAsync()
    {
        if (!IsLocked)
        {
            return;
        }

        var availability = await _windowsHello.CheckAvailabilityAsync();
        if (availability != WindowsHelloAvailability.Available)
        {
            SkipLock(availability);
            return;
        }

        await UnlockAsync();
    }

    [RelayCommand]
    private async Task UnlockAsync()
    {
        var result = await _windowsHello.RequestVerificationAsync(WindowHandle,
            _localizationService.GetString(ResourceKeys.WindowsHelloLaunchPrompt));

        switch (result)
        {
            case WindowsHelloVerificationResult.Verified:
                IsLocked = false;
                StatusMessage = null;
                break;
            case WindowsHelloVerificationResult.Unavailable:
                SkipLock(result);
                break;
            default:
                StatusMessage = _localizationService.GetString(ResourceKeys.WindowsHelloVerificationFailed);
                break;
        }
    }

    private void SkipLock(object reason)
    {
        _logger.Warning("Windows Hello is required on launch but is unavailable ({Reason}); opening unlocked", reason);
        IsLocked = false;
        StatusMessage = null;
    }
}
