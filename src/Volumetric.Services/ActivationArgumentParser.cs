using System.Runtime.InteropServices;

namespace Volumetric.Services;

public static partial class ActivationArgumentParser
{
    public const string DefaultProtocolPathMarker = "path=";

    public enum ParsedKind
    {
        None,
        Path,
        InvalidPath
    }

    public readonly record struct ParsedLaunch(ParsedKind Kind, string? Path);

    public static ParsedLaunch ParseLaunchArguments(string? rawArgs, Func<string, bool> pathExists)
    {
        ArgumentNullException.ThrowIfNull(pathExists);

        if (string.IsNullOrWhiteSpace(rawArgs))
        {
            return new ParsedLaunch(ParsedKind.None, null);
        }

        var tokens = SplitCommandLine(rawArgs);
        var path = tokens.Length > 0 ? tokens[^1] : null;

        if (string.IsNullOrWhiteSpace(path))
        {
            return new ParsedLaunch(ParsedKind.None, null);
        }

        return new ParsedLaunch(pathExists(path) ? ParsedKind.Path : ParsedKind.InvalidPath, path);
    }

    public static string? ExtractPathFromProtocolUri(string? uriString, string pathMarker = DefaultProtocolPathMarker)
    {
        if (string.IsNullOrEmpty(uriString))
        {
            return null;
        }

        var pathIdx = uriString.IndexOf(pathMarker, StringComparison.OrdinalIgnoreCase);
        if (pathIdx < 0)
        {
            return null;
        }

        var path = Uri.UnescapeDataString(uriString[(pathIdx + pathMarker.Length)..]).Trim();
        return string.IsNullOrEmpty(path) ? null : path;
    }

    public static string[] SplitCommandLine(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return [];
        }

        var argv = CommandLineToArgvW(commandLine, out var argc);
        if (argv == IntPtr.Zero)
        {
            return [];
        }

        try
        {
            var result = new string[argc];
            for (var i = 0; i < argc; i++)
            {
                var strPtr = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                result[i] = Marshal.PtrToStringUni(strPtr) ?? string.Empty;
            }

            return result;
        }
        finally
        {
            LocalFree(argv);
        }
    }

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial IntPtr CommandLineToArgvW(string cmdLine, out int numArgs);

    [LibraryImport("kernel32.dll")]
    private static partial IntPtr LocalFree(IntPtr hMem);
}
