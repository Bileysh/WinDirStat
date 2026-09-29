using Microsoft.UI.Xaml;
using Volumetric_App.Converters;

namespace Volumetric.UITests.Converters;

public class StringToVisibilityConverterTests
{
    private readonly StringToVisibilityConverter _converter = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Convert_NullOrWhitespace_ReturnsCollapsed(string? value)
    {
        var result = _converter.Convert(value!, typeof(Visibility), null!, string.Empty);

        Assert.Equal(Visibility.Collapsed, result);
    }

    [Fact]
    public void Convert_NonEmptyString_ReturnsVisible()
    {
        var result = _converter.Convert("C:\\Users", typeof(Visibility), null!, string.Empty);

        Assert.Equal(Visibility.Visible, result);
    }

    [Fact]
    public void ConvertBack_Throws()
    {
        Assert.Throws<NotImplementedException>(() =>
            _converter.ConvertBack(Visibility.Visible, typeof(string), null!, string.Empty));
    }
}
