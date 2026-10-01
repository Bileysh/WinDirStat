using System.Drawing;
using Volumetric_App.Services;

namespace Volumetric.UITests.Services;

public class TrayIconSetTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CreateIcon_ReturnsNewInstanceOnEveryCall(bool isScanning)
    {
        using var icons = new TrayIconSet((Icon)SystemIcons.Application.Clone());

        using var first = icons.CreateIcon(isScanning);
        using var second = icons.CreateIcon(isScanning);

        Assert.NotSame(first, second);
    }

    [Fact]
    public void CreateIcon_AfterConsumerDisposesPreviousIcons_StillReturnsUsableIcons()
    {
        using var icons = new TrayIconSet((Icon)SystemIcons.Application.Clone());

        for (var i = 0; i < 10; i++)
        {
            var icon = icons.CreateIcon(isScanning: i % 2 == 1);

            Assert.NotEqual(IntPtr.Zero, icon.Handle);

            icon.Dispose();
        }
    }

    [Fact]
    public void CreateIcon_Scanning_DiffersFromIdleInBadgeCorner()
    {
        using var icons = new TrayIconSet((Icon)SystemIcons.Application.Clone());

        using var idle = icons.CreateIcon(isScanning: false).ToBitmap();
        using var scanning = icons.CreateIcon(isScanning: true).ToBitmap();

        var x = scanning.Width * 3 / 4;
        var y = scanning.Height * 3 / 4;

        Assert.NotEqual(idle.GetPixel(x, y), scanning.GetPixel(x, y));
    }
}
