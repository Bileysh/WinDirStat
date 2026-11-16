using Microsoft.Win32;
using Serilog;

namespace Volumetric_App.Services;

public static class LegacyRegistryCleanup
{
    private static readonly string[] StaleKeyPaths =
    [
        @"Software\Classes\Directory\shell\WinDirStat",
        @"Software\Classes\Directory\Background\shell\WinDirStat",
    ];

    public static void RemoveStaleClassicContextMenuKeys()
    {
        foreach (var keyPath in StaleKeyPaths)
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "LegacyRegistryCleanup: failed to remove stale key '{KeyPath}'", keyPath);
            }
        }
    }
}
