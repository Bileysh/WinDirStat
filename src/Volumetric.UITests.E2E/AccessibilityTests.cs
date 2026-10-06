using System.Diagnostics;
using Axe.Windows.Automation;
using Axe.Windows.Automation.Data;
using Xunit.Abstractions;

namespace Volumetric.UITests.E2E;

[Collection(VolumetricAppCollection.Name)]
public class AccessibilityTests(VolumetricAppFixture app, ITestOutputHelper output)
{
    [E2EFact]
    public void MainWindow_HasNoAutomatedAccessibilityErrors()
    {
        AssertNoErrors(ScanApp());
    }

    [E2EFact]
    public async Task SettingsWindow_HasNoAutomatedAccessibilityErrors()
    {
        IReadOnlyCollection<WindowScanOutput> windows = [];
        await app.Session.WithSettingsWindowAsync(_ =>
        {
            windows = ScanApp();
            return Task.CompletedTask;
        });

        Assert.Equal(2, windows.Count);
        AssertNoErrors(windows);
    }

    [E2EFact]
    public async Task ScanResultsWindow_HasNoAutomatedAccessibilityErrors()
    {
        var folder = Directory.CreateTempSubdirectory("volumetric-e2e-a11y-");
        await File.WriteAllBytesAsync(Path.Combine(folder.FullName, "report.docx"), new byte[4096]);
        await File.WriteAllBytesAsync(Path.Combine(folder.FullName, "photo.jpg"), new byte[2048]);
        var nested = folder.CreateSubdirectory("nested");
        await File.WriteAllBytesAsync(Path.Combine(nested.FullName, "video.mp4"), new byte[8192]);

        var session = app.Session;
        var mainWindow = await session.GetWindowHandleAsync();
        var knownWindows = await session.GetWindowHandlesAsync();
        var timeout = TimeSpan.FromSeconds(30);

        Process.Start(new ProcessStartInfo($"volumetric://scan?path={Uri.EscapeDataString(folder.FullName)}")
        {
            UseShellExecute = true
        });

        var scanWindow = await session.WaitForNewWindowAsync(knownWindows, timeout);
        try
        {
            await session.SwitchToWindowAsync(scanWindow);
            await session.WaitForElementAsync("xpath", $"//*[contains(@Name, '{folder.Name}')]", timeout);

            var windows = ScanApp();

            Assert.Equal(2, windows.Count);
            AssertNoErrors(windows);
        }
        finally
        {
            await session.CloseCurrentWindowAsync();
            await session.SwitchToWindowAsync(mainWindow);
            folder.Delete(recursive: true);
        }
    }

    private static readonly string ResultsDirectory = Path.Combine(AppContext.BaseDirectory, "AxeResults");

    private static IReadOnlyCollection<WindowScanOutput> ScanApp()
    {
        var processId = Process.GetProcessesByName("Volumetric.App").Single().Id;
        var config = Config.Builder.ForProcessId(processId)
            .WithOutputFileFormat(OutputFileFormat.A11yTest)
            .WithOutputDirectory(ResultsDirectory)
            .Build();

        return ScannerFactory.CreateScanner(config).Scan(new ScanOptions()).WindowScanOutputs;
    }

    private void AssertNoErrors(IReadOnlyCollection<WindowScanOutput> windows)
    {
        Assert.NotEmpty(windows);

        var errors = windows.SelectMany(window => window.Errors).ToList();
        foreach (var error in errors)
        {
            output.WriteLine($"{error.Rule.ID}: {error.Rule.Description} | {Describe(error.Element)}");
        }

        if (errors.Count > 0)
        {
            foreach (var file in windows.Select(window => window.OutputFile.A11yTest).Where(file => file is not null))
            {
                output.WriteLine($"Open in Accessibility Insights for Windows: {file}");
            }
        }

        Assert.Empty(errors);
    }

    private static string Describe(ElementInfo element)
    {
        string Get(string property) =>
            element.Properties.TryGetValue(property, out var value) ? value : string.Empty;

        return $"{Get("ControlType")} name='{Get("Name")}' automationId='{Get("AutomationId")}' " +
               $"class='{Get("ClassName")}'";
    }
}
