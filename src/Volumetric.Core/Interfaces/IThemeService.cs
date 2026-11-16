namespace Volumetric.Core.Interfaces;

public interface IThemeService
{
    bool IsDarkTheme { get; }
    void ToggleTheme();
    event EventHandler<bool>? ThemeChanged;
}
