using Microsoft.UI.Xaml;
using WinDirStat.Core.Interfaces;

namespace WinDirStat_App.Services;

public class WinUiThemeService : IThemeService
{
    public bool IsDarkTheme { get; private set; } = Application.Current.RequestedTheme == ApplicationTheme.Dark;
    
    public event EventHandler<bool>? ThemeChanged;

    public void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        ThemeChanged?.Invoke(this, IsDarkTheme);
    }
}