using Microsoft.UI.Xaml;

namespace Volumetric.UITests;

public class WinUiHostSmokeTests
{
    [Fact]
    public void TestHost_CanResolveWinUITypes()
    {
        Assert.Equal(Visibility.Collapsed, Visibility.Collapsed);
    }
}
