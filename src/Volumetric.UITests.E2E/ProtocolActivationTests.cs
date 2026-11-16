using System.Diagnostics;

namespace Volumetric.UITests.E2E;

[Collection(VolumetricAppCollection.Name)]
public class ProtocolActivationTests(VolumetricAppFixture app)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    [E2EFact]
    public async Task ScanProtocolUri_OpensNewWindowShowingTheScannedFolder()
    {
        var folder = Directory.CreateTempSubdirectory("volumetric-e2e-");
        await File.WriteAllBytesAsync(Path.Combine(folder.FullName, "marker.bin"), new byte[1024]);

        var session = app.Session;
        var mainWindow = await session.GetWindowHandleAsync();
        var knownWindows = await session.GetWindowHandlesAsync();

        Process.Start(new ProcessStartInfo($"volumetric://scan?path={Uri.EscapeDataString(folder.FullName)}")
        {
            UseShellExecute = true
        });

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
