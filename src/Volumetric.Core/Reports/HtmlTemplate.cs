using System.Reflection;
using System.Text.RegularExpressions;

namespace Volumetric.Core.Reports;

/// <summary>
/// Loads HTML/CSS templates embedded in an assembly and fills their {{token}} placeholders.
/// </summary>
public static partial class HtmlTemplate
{
    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex TokenPattern();

    public static string Load(Assembly assembly, string fileName)
    {
        var resourceName = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith("." + fileName, StringComparison.Ordinal));

        using var stream = assembly.GetManifestResourceStream(resourceName)
                           ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Replaces every {{token}} in a single pass. Values are inserted verbatim, so callers must HTML-encode
    /// untrusted text first; a value is never scanned again, so a file named "{{x}}" cannot inject a token.
    /// </summary>
    public static string Render(string template, params (string Name, string Value)[] values)
    {
        return TokenPattern().Replace(template, match =>
        {
            var name = match.Groups[1].Value;
            foreach (var (valueName, value) in values)
            {
                if (valueName == name)
                {
                    return value;
                }
            }

            throw new KeyNotFoundException($"No value supplied for template token '{{{{{name}}}}}'.");
        });
    }
}
