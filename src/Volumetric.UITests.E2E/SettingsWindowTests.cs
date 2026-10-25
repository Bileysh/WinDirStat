using System.Diagnostics;

namespace Volumetric.UITests.E2E;

[Collection(VolumetricAppCollection.Name)]
public class SettingsWindowTests(VolumetricAppFixture app)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    [E2EFact]
    public async Task OptionsMenu_OpensSettingsWindowWithSecuritySection()
    {
        await WithSettingsWindowAsync(async session =>
        {
            var securityTitle = await session.WaitForElementAsync("accessibility id", "SecuritySettingsTitle", Timeout);
            Assert.True(await session.IsDisplayedAsync(securityTitle));
            Assert.NotNull(await session.FindElementAsync("accessibility id", "EncryptScanResultsToggle"));
            Assert.NotNull(await session.FindElementAsync("accessibility id", "RequireWindowsHelloToggle"));
        });
    }

    [E2EFact]
    public async Task TestScanButton_RunsBackgroundScanAndLogsNoProblems()
    {
        var lastScanFile = Path.Combine(VolumetricPackage.GetDataFolder(), "LocalState", "bgtask-last-scan.txt");
        var logFile = VolumetricPackage.GetLatestLogFile();
        var logLinesBefore = VolumetricPackage.ReadLogLines(logFile).Length;
        var clickedAt = DateTime.Now.AddSeconds(-1);

        await WithSettingsWindowAsync(async session =>
        {
            await session.PressSpaceAsync(await session.WaitForElementAsync("accessibility id", "TestScanNowButton", Timeout));
            await WaitUntilAsync(() => File.Exists(lastScanFile) && File.GetLastWriteTime(lastScanFile) >= clickedAt);
        });

        var systemDrive = Path.GetPathRoot(Environment.SystemDirectory)!;
        Assert.Contains(File.ReadAllLines(lastScanFile),
            line => line.StartsWith(systemDrive + "|", StringComparison.OrdinalIgnoreCase));

        var problems = VolumetricPackage.ReadLogLines(logFile).Skip(logLinesBefore)
            .Where(line => line.Contains("[WRN]") || line.Contains("[ERR]") || line.Contains("[FTL]"))
            .ToList();
        Assert.Empty(problems);
    }

    private Task WithSettingsWindowAsync(Func<WinAppDriverSession, Task> act) =>
        app.Session.WithSettingsWindowAsync(act);

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var elapsed = Stopwatch.StartNew();
        while (!condition())
        {
            if (elapsed.Elapsed > Timeout)
            {
                throw new TimeoutException($"Condition was not met within {Timeout}.");
            }

            await Task.Delay(250);
        }
    }
}
