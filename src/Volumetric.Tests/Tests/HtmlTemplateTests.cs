using Volumetric.Core.Reports;

namespace Volumetric.Tests.Tests;

public class HtmlTemplateTests
{
    [Fact]
    public void Render_ReplacesEveryTokenOccurrence()
    {
        var html = HtmlTemplate.Render("<h1>{{title}}</h1><p>{{title}} {{body}}</p>",
            ("title", "T"), ("body", "B"));

        Assert.Equal("<h1>T</h1><p>T B</p>", html);
    }

    [Fact]
    public void Render_DoesNotExpandTokensInsideInsertedValues()
    {
        var html = HtmlTemplate.Render("{{first}}|{{second}}", ("first", "{{second}}"), ("second", "S"));

        Assert.Equal("{{second}}|S", html);
    }

    [Fact]
    public void Render_UnknownToken_Throws()
    {
        Assert.Throws<KeyNotFoundException>(() => HtmlTemplate.Render("{{missing}}", ("other", "x")));
    }

    [Fact]
    public void Load_ReadsEmbeddedTemplateFromServicesAssembly()
    {
        var template = HtmlTemplate.Load(typeof(Volumetric.Services.ScanReportService).Assembly, "ScanReportPage.html");

        Assert.Contains("{{tables}}", template);
    }
}
