using System.Diagnostics;

namespace Volumetric.UITests.E2E;

[Collection(VolumetricAppCollection.Name)]
public class CommandLineActivationTests(VolumetricAppFixture app)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private static readonly string ExecutionAlias = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Microsoft", "WindowsApps", "volumetric.exe");

    [E2EFact]
    public async Task ScanCommandThroughExecutionAlias_OpensNewWindowShowingTheScannedFolder()
    {
        var folder = Directory.CreateTempSubdirectory("volumetric-e2e-cli-");
        await File.WriteAllBytesAsync(Path.Combine(folder.FullName, "marker.bin"), new byte[1024]);

        var session = app.Session;
        var mainWindow = await session.GetWindowHandleAsync();
        var knownWindows = await session.GetWindowHandlesAsync();

        using (Process.Start(new ProcessStartInfo(ExecutionAlias) { ArgumentList = { "scan", folder.FullName } }))
        {
        }

        var scanWindow = await session.WaitForNewWindowAsync(knownWindows, Timeout);
        try
        {
            await session.SwitchToWindowAsync(scanWindow);

            var scannedRoot = await session.WaitForElementAsync("xpath", $"//*[contains(@Name, '{folder.Name}')]",
                Timeout);
            Assert.True(await session.IsDisplayedAsync(scannedRoot));
        }
        finally
        {
            await session.CloseCurrentWindowAsync();
            await session.SwitchToWindowAsync(mainWindow);
            folder.Delete(recursive: true);
        }
    }
}
