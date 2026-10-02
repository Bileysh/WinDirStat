namespace Volumetric.UITests.E2E;

[Collection(VolumetricAppCollection.Name)]
public class SettingsWindowTests(VolumetricAppFixture app)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    [E2EFact]
    public async Task OptionsMenu_OpensSettingsWindowWithSecuritySection()
    {
        var session = app.Session;
        var mainWindow = await session.GetWindowHandleAsync();
        var knownWindows = await session.GetWindowHandlesAsync();

        await session.ClickAsync(await session.WaitForElementAsync("accessibility id", "MenuOptions", Timeout));
        await session.ClickAsync(await session.WaitForElementAsync("accessibility id", "MenuOptionsSettings", Timeout));

        var settingsWindow = await session.WaitForNewWindowAsync(knownWindows, Timeout);
        try
        {
            await session.SwitchToWindowAsync(settingsWindow);

            var securityTitle = await session.WaitForElementAsync("accessibility id", "SecuritySettingsTitle", Timeout);
            Assert.True(await session.IsDisplayedAsync(securityTitle));
            Assert.NotNull(await session.FindElementAsync("accessibility id", "EncryptScanResultsToggle"));
            Assert.NotNull(await session.FindElementAsync("accessibility id", "RequireWindowsHelloToggle"));
        }
        finally
        {
            await session.CloseCurrentWindowAsync();
            await session.SwitchToWindowAsync(mainWindow);
        }
    }
}
