namespace Volumetric.UITests.E2E;

public static class SettingsWindowHelper
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan ColdWindowTimeout = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan MenuItemTimeout = TimeSpan.FromSeconds(5);
    private const int MenuAttempts = 3;

    public static async Task WithSettingsWindowAsync(this WinAppDriverSession session,
        Func<WinAppDriverSession, Task> act)
    {
        var mainWindow = await session.GetWindowHandleAsync();
        var knownWindows = await session.GetWindowHandlesAsync();

        await session.ClickAsync(await OpenOptionsMenuItemAsync(session));

        var settingsWindow = await session.WaitForNewWindowAsync(knownWindows, ColdWindowTimeout);
        try
        {
            await session.SwitchToWindowAsync(settingsWindow);
            await act(session);
        }
        finally
        {
            await session.CloseCurrentWindowAsync();
            await session.SwitchToWindowAsync(mainWindow);
        }
    }

    private static async Task<string> OpenOptionsMenuItemAsync(WinAppDriverSession session)
    {
        for (var attempt = 1; ; attempt++)
        {
            await session.ClickAsync(await session.WaitForElementAsync("accessibility id", "MenuOptions", Timeout));
            try
            {
                return await session.WaitForElementAsync("accessibility id", "MenuOptionsSettings", MenuItemTimeout);
            }
            catch (TimeoutException) when (attempt < MenuAttempts)
            {
            }
        }
    }
}
