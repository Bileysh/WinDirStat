using ActivationArgumentParser = Volumetric.Services.ActivationArgumentParser;

namespace Volumetric.Tests.Tests;

public class ActivationArgumentParserTests
{
    [Fact]
    public void ParseLaunchArguments_NullOrWhitespace_ReturnsNone()
    {
        var result = ActivationArgumentParser.ParseLaunchArguments(null, _ => true);
        Assert.Equal(ActivationArgumentParser.ParsedKind.None, result.Kind);
        Assert.Null(result.Path);

        var whitespace = ActivationArgumentParser.ParseLaunchArguments("   ", _ => true);
        Assert.Equal(ActivationArgumentParser.ParsedKind.None, whitespace.Kind);
    }

    [Fact]
    public void ParseLaunchArguments_SingleUnquotedPath_ExistingPath_ReturnsPath()
    {
        var result = ActivationArgumentParser.ParseLaunchArguments(
            @"C:\Users\test\Documents", path => path == @"C:\Users\test\Documents");

        Assert.Equal(ActivationArgumentParser.ParsedKind.Path, result.Kind);
        Assert.Equal(@"C:\Users\test\Documents", result.Path);
    }

    [Fact]
    public void ParseLaunchArguments_NonExistentPath_ReturnsInvalidPath()
    {
        var result = ActivationArgumentParser.ParseLaunchArguments(
            @"C:\does\not\exist", _ => false);

        Assert.Equal(ActivationArgumentParser.ParsedKind.InvalidPath, result.Kind);
        Assert.Equal(@"C:\does\not\exist", result.Path);
    }

    [Fact]
    public void ParseLaunchArguments_QuotedPathWithSpaces_TakesLastToken()
    {
        var result = ActivationArgumentParser.ParseLaunchArguments(
            "\"C:\\Program Files\\MyApp.exe\" \"C:\\Users\\test\\My Documents\"",
            path => path == @"C:\Users\test\My Documents");

        Assert.Equal(ActivationArgumentParser.ParsedKind.Path, result.Kind);
        Assert.Equal(@"C:\Users\test\My Documents", result.Path);
    }

    [Fact]
    public void ParseLaunchArguments_ExeNameOnly_TreatsExeAsCandidatePath()
    {
        var result = ActivationArgumentParser.ParseLaunchArguments(
            "\"C:\\Program Files\\MyApp.exe\"", _ => false);

        Assert.Equal(ActivationArgumentParser.ParsedKind.InvalidPath, result.Kind);
        Assert.Equal(@"C:\Program Files\MyApp.exe", result.Path);
    }

    [Fact]
    public void ParseLaunchArguments_NullPathExists_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ActivationArgumentParser.ParseLaunchArguments("anything", null!));
    }

    [Theory]
    [InlineData("scan://open?path=C%3A%5CUsers%5Ctest", @"C:\Users\test")]
    [InlineData("scan://open?path=C:\\Users\\test%5CMy%20Docs", @"C:\Users\test\My Docs")]
    public void ExtractPathFromProtocolUri_ValidMarker_DecodesPath(string uri, string expected)
    {
        var path = ActivationArgumentParser.ExtractPathFromProtocolUri(uri);
        Assert.Equal(expected, path);
    }

    [Fact]
    public void ExtractPathFromProtocolUri_NoMarker_ReturnsNull()
    {
        var path = ActivationArgumentParser.ExtractPathFromProtocolUri("scan://open?foo=bar");
        Assert.Null(path);
    }

    [Fact]
    public void ExtractPathFromProtocolUri_MarkerWithEmptyValue_ReturnsNull()
    {
        var path = ActivationArgumentParser.ExtractPathFromProtocolUri("scan://open?path=");
        Assert.Null(path);
    }

    [Fact]
    public void ExtractPathFromProtocolUri_MarkerWithOnlyWhitespaceValue_ReturnsNull()
    {
        var path = ActivationArgumentParser.ExtractPathFromProtocolUri("scan://open?path=%20%20");
        Assert.Null(path);
    }

    [Fact]
    public void ExtractPathFromProtocolUri_NullOrEmptyUri_ReturnsNull()
    {
        Assert.Null(ActivationArgumentParser.ExtractPathFromProtocolUri(null));
        Assert.Null(ActivationArgumentParser.ExtractPathFromProtocolUri(string.Empty));
    }

    [Fact]
    public void ExtractPathFromProtocolUri_CaseInsensitiveMarker()
    {
        var path = ActivationArgumentParser.ExtractPathFromProtocolUri("scan://open?PATH=C%3A%5CFoo");
        Assert.Equal(@"C:\Foo", path);
    }

    [Fact]
    public void SplitCommandLine_EmptyString_ReturnsEmptyArray()
    {
        Assert.Empty(ActivationArgumentParser.SplitCommandLine(""));
        Assert.Empty(ActivationArgumentParser.SplitCommandLine("   "));
    }

    [Fact]
    public void SplitCommandLine_MultipleTokens_SplitsOnUnquotedWhitespace()
    {
        var tokens = ActivationArgumentParser.SplitCommandLine("app.exe --flag \"C:\\a b\\c\"");
        Assert.Equal(new[] { "app.exe", "--flag", @"C:\a b\c" }, tokens);
    }
}
