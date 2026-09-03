using Microsoft.UI.Xaml;
using Windows.Storage;
using WinDirStat.Core.Interfaces;

namespace WinDirStat_App.Services;

public class WinUiThemeService : IThemeService
{
    private const string ThemeSettingKey = "IsDarkTheme";

    public bool IsDarkTheme { get; private set; } = LoadSavedTheme();

    public event EventHandler<bool>? ThemeChanged;

    public void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        ApplicationData.Current.LocalSettings.Values[ThemeSettingKey] = IsDarkTheme;
        ThemeChanged?.Invoke(this, IsDarkTheme);
    }

    private static bool LoadSavedTheme()
    {
        var values = ApplicationData.Current.LocalSettings.Values;
        return values.TryGetValue(ThemeSettingKey, out var stored) && stored is bool isDark
            ? isDark
            : Application.Current.RequestedTheme == ApplicationTheme.Dark;
    }
}