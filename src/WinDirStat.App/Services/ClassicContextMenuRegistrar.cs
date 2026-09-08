using Microsoft.Win32;

namespace WinDirStat_App.Services;

public static class ClassicContextMenuRegistrar
{
    private const string MenuText = "Scan with WinDirStat";

    public static void EnsureRegistered()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath)) return;

        RegisterFor(@"Directory\shell\WinDirStat", exePath, "\"%1\"");
        RegisterFor(@"Directory\Background\shell\WinDirStat", exePath, "\"%V\"");
    }

    private static void RegisterFor(string keyPath, string exePath, string argPattern)
    {
        using var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{keyPath}");
        key.SetValue(null, MenuText);
        key.SetValue("Icon", exePath);

        using var commandKey = key.CreateSubKey("command");
        commandKey.SetValue(null, $"\"{exePath}\" {argPattern}");
    }
}