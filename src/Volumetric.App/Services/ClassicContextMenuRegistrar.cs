using Microsoft.Win32;
using Windows.Storage;

namespace Volumetric_App.Services;

public static class ClassicContextMenuRegistrar
{
    private const string MenuText = "Scan with Volumetric";
    
    public static string? EnsureRegistered()
    {
        var exePath = Environment.ProcessPath;
        Log($"EnsureRegistered starting. Environment.ProcessPath = '{exePath ?? "(null)"}'");

        if (string.IsNullOrEmpty(exePath))
        {
            const string reason = "Environment.ProcessPath was null/empty - cannot register a verb with no exe path.";
            Log(reason);
            return reason;
        }

        var directoryError = TryRegisterFor(@"Directory\shell\Volumetric", exePath, "\"%1\"");
        var backgroundError = TryRegisterFor(@"Directory\Background\shell\Volumetric", exePath, "\"%V\"");

        var error = directoryError ?? backgroundError;
        Log(error is null
            ? "EnsureRegistered completed with no errors."
            : $"EnsureRegistered completed with an error: {error}");

        return error;
    }

    private static string? TryRegisterFor(string keyPath, string exePath, string argPattern)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{keyPath}");
            if (key is null)
            {
                var reason = $"CreateSubKey returned null for '{keyPath}'.";
                Log(reason);
                return reason;
            }

            key.SetValue(null, MenuText);
            key.SetValue("Icon", exePath);

            using var commandKey = key.CreateSubKey("command");
            if (commandKey is null)
            {
                var reason = $"CreateSubKey('command') returned null under '{keyPath}'.";
                Log(reason);
                return reason;
            }

            commandKey.SetValue(null, $"\"{exePath}\" {argPattern}");
            
            var readBack = key.GetValue(null) as string;
            Log($"Registered '{keyPath}' - read back default value: '{readBack ?? "(null)"}'");

            return readBack == MenuText ? null : $"Wrote to '{keyPath}' but read-back didn't match expected value.";
        }
        catch (Exception ex)
        {
            var reason = $"Exception registering '{keyPath}': {ex}";
            Log(reason);
            return reason;
        }
    }

    private static void Log(string message)
    {
        System.Diagnostics.Debug.WriteLine($"[ClassicContextMenuRegistrar] {message}");

        try
        {
            var logFile = Path.Combine(ApplicationData.Current.LocalFolder.Path, "context-menu-log.txt");
            File.AppendAllText(logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}");
        }
        catch
        {
            
        }
    }
}
