using Volumetric.Core.Interfaces;

namespace Volumetric.Tests.FakeService;

public class FakeThemeService : IThemeService
{
    public bool IsDarkTheme { get; private set; } = false;

    public event EventHandler<bool>? ThemeChanged;

    public void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        ThemeChanged?.Invoke(this, IsDarkTheme);
    }
}