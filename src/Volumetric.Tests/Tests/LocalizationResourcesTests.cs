using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Volumetric.Core.BackgroundScan;
using Volumetric.Core.Interfaces;

namespace Volumetric.Tests.Tests;

public partial class LocalizationResourcesTests
{
    private static readonly Dictionary<string, string> English = Load("en-US");
    private static readonly Dictionary<string, string> Ukrainian = Load("uk-UA");

    public static TheoryData<string> Languages => new() { "en-US", "uk-UA" };

    [Fact]
    public void BothLanguages_HaveTheSameKeys()
    {
        Assert.Empty(English.Keys.Except(Ukrainian.Keys));
        Assert.Empty(Ukrainian.Keys.Except(English.Keys));
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void NoValue_IsEmpty(string language)
    {
        Assert.Empty(ForLanguage(language).Where(e => string.IsNullOrWhiteSpace(e.Value)).Select(e => e.Key));
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void EveryResourceKeyConstant_Exists(string language)
    {
        var prefixes = new[] { ResourceKeys.SettingsErrorPrefix, ResourceKeys.WindowsHelloUnavailablePrefix };
        var keys = typeof(ResourceKeys).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral)
            .Select(f => (string)f.GetRawConstantValue()!)
            .Except(prefixes);

        var missing = keys.Where(k => !ForLanguage(language).ContainsKey(k)).ToList();
        Assert.Empty(missing);
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void EveryPrefixedKey_ExistsForEachEnumValue(string language)
    {
        var expected = Enum.GetNames<SettingsValidationError>()
            .Where(n => n != nameof(SettingsValidationError.None))
            .Select(n => ResourceKeys.SettingsErrorPrefix + n)
            .Concat(Enum.GetNames<WindowsHelloAvailability>()
                .Where(n => n != nameof(WindowsHelloAvailability.Available))
                .Select(n => ResourceKeys.WindowsHelloUnavailablePrefix + n));

        var missing = expected.Where(k => !ForLanguage(language).ContainsKey(k)).ToList();
        Assert.Empty(missing);
    }

    [Fact]
    public void FormatPlaceholders_MatchBetweenLanguages()
    {
        var mismatches = English.Keys.Intersect(Ukrainian.Keys)
            .Where(k => !Placeholders(English[k]).SetEquals(Placeholders(Ukrainian[k])))
            .ToList();

        Assert.Empty(mismatches);
    }

    private static Dictionary<string, string> ForLanguage(string language) =>
        language == "en-US" ? English : Ukrainian;

    private static HashSet<string> Placeholders(string value) =>
        PlaceholderPattern().Matches(value).Select(m => m.Value).ToHashSet();

    private static Dictionary<string, string> Load(string language)
    {
        var path = Path.Combine(FindRepositoryRoot(), "src", "Volumetric.App", "Strings", language, "Resources.resw");
        var entries = XDocument.Load(path).Root!.Elements("data")
            .Select(d => (Key: (string)d.Attribute("name")!, Value: (string?)d.Element("value") ?? string.Empty))
            .ToList();

        var duplicates = entries.GroupBy(e => e.Key).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException($"Duplicate keys in {language}: {string.Join(", ", duplicates)}");
        }

        return entries.ToDictionary(e => e.Key, e => e.Value);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Volumetric.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Volumetric.sln was not found.");
    }

    [GeneratedRegex(@"\{\d+(:[^}]*)?\}")]
    private static partial Regex PlaceholderPattern();
}
