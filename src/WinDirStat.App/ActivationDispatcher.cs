using System.Runtime.InteropServices;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;
using Serilog;
using WinDirStat.Core.Interfaces;

namespace WinDirStat_App;

public static partial class ActivationDispatcher
{
    private const string ProtocolSchemeHost = "scan";
    private const string ProtocolPathMarker = "path=";
    private const string InvalidPathResourceKey = "InvalidPathTitle";
    private const string DefaultInvalidPathTitle = "Invalid Path";

    public enum ActivationAction
    {
        None,
        Path,
        File,
        InvalidPath,
        StartupTask
    }

    public readonly record struct ExtractedActivation(ActivationAction Action, string? Path);

    public static ExtractedActivation Extract(AppActivationArguments? args)
    {
        if (args is null) return new ExtractedActivation(ActivationAction.None, null);

        switch (args.Kind)
        {
            case ExtendedActivationKind.Launch:
                if (args.Data is ICommandLineActivatedEventArgs cmdArgs)
                {
                    var rawArgs = cmdArgs.Operation.Arguments;
                    Log.Debug("Extract[Launch]: raw Operation.Arguments = '{RawArgs}'", rawArgs);

                    if (string.IsNullOrWhiteSpace(rawArgs)) break;

                    var tokens = SplitCommandLine(rawArgs);
                    Log.Debug("Extract[Launch]: tokenized to [{Tokens}]", string.Join(" | ", tokens));

                    var path = tokens.Length > 0 ? tokens[^1] : null;
                    if (string.IsNullOrWhiteSpace(path)) break;

                    var exists = Directory.Exists(path);
                    Log.Information("Extract[Launch]: candidate path='{Path}', exists={Exists}", path, exists);

                    return new ExtractedActivation(exists ? ActivationAction.Path : ActivationAction.InvalidPath,
                        path);
                }

                if (args.Data is ILaunchActivatedEventArgs launchArgs)
                {
                    var rawLaunchArgs = launchArgs.Arguments;
                    Log.Debug("Extract[Launch]: ILaunchActivatedEventArgs.Arguments = '{RawArgs}'", rawLaunchArgs);

                    if (!string.IsNullOrWhiteSpace(rawLaunchArgs))
                    {
                        var launchTokens = SplitCommandLine(rawLaunchArgs);
                        var launchPath = launchTokens.Length > 0 ? launchTokens[^1] : null;

                        if (!string.IsNullOrWhiteSpace(launchPath))
                        {
                            var launchExists = Directory.Exists(launchPath);
                            Log.Information(
                                "Extract[Launch/ILaunchActivatedEventArgs]: candidate path='{Path}', exists={Exists}",
                                launchPath, launchExists);
                            return new ExtractedActivation(
                                launchExists ? ActivationAction.Path : ActivationAction.InvalidPath, launchPath);
                        }
                    }
                }

                Log.Debug("Extract[Launch]: args.Data was not ICommandLineActivatedEventArgs ({Type})",
                    args.Data?.GetType().Name);
                break;

            case ExtendedActivationKind.File:
                if (args.Data is IFileActivatedEventArgs fileArgs && fileArgs.Files.Count > 0)
                {
                    Log.Information("Extract[File]: path='{Path}'", fileArgs.Files[0].Path);
                    return new ExtractedActivation(ActivationAction.File, fileArgs.Files[0].Path);
                }

                break;

            case ExtendedActivationKind.Protocol:
                if (args.Data is IProtocolActivatedEventArgs protocolArgs &&
                    protocolArgs.Uri.Host.Equals(ProtocolSchemeHost, StringComparison.OrdinalIgnoreCase))
                {
                    var uriStr = protocolArgs.Uri.ToString();
                    Log.Information("Extract[Protocol]: uri='{Uri}'", uriStr);
                    
                    var pathIdx = uriStr.IndexOf(ProtocolPathMarker, StringComparison.OrdinalIgnoreCase);
                    if (pathIdx >= 0)
                    {
                        var path = Uri.UnescapeDataString(uriStr.Substring(pathIdx + ProtocolPathMarker.Length)).Trim();
                        Log.Information("Extract[Protocol]: decoded path='{Path}'", path);
                        if (!string.IsNullOrEmpty(path))
                        {
                            return new ExtractedActivation(
                                Directory.Exists(path) ? ActivationAction.Path : ActivationAction.InvalidPath, path);
                        }
                    }
                }

                break;

            case ExtendedActivationKind.StartupTask:
                Log.Information("Extract[StartupTask]");
                return new ExtractedActivation(ActivationAction.StartupTask, null);
        }

        return new ExtractedActivation(ActivationAction.None, null);
    }

    public static void HandleExtracted(ExtractedActivation extracted, bool isColdStart)
    {
#if DEBUG
        Log.Information("HandleExtracted: {Action} / '{Path}' (isColdStart={IsColdStart})",
            extracted.Action, extracted.Path, isColdStart);
#endif

        switch (extracted.Action)
        {
            case ActivationAction.Path:
                OpenScan(extracted.Path, isColdStart);
                break;

            case ActivationAction.InvalidPath:
                ShowInvalidPathNotification(extracted.Path);
                break;

            case ActivationAction.File:
                ImportScanFile(extracted.Path, isColdStart);
                break;

            case ActivationAction.StartupTask:
                HandleStartupTask();
                break;
        }
    }

    private static void ShowInvalidPathNotification(string? path)
    {
        Log.Warning("HandleExtracted: path '{Path}' does not exist, showing notification instead of scanning", path);
        var notificationService = App.StaticServices?.GetService(typeof(INotificationService)) as INotificationService;
        var localization = App.StaticServices?.GetService(typeof(ILocalizationService)) as ILocalizationService;
        var title = localization?.GetString(InvalidPathResourceKey) ?? DefaultInvalidPathTitle;
        notificationService?.ShowNotification(title, path ?? string.Empty);
    }

    private static async void ImportScanFile(string? path, bool isColdStart)
    {
        if (string.IsNullOrEmpty(path)) return;

        var fileService = App.StaticServices?.GetService(typeof(IScanResultFileService)) as IScanResultFileService;
        if (fileService is null) return;

        var rootNode = await fileService.ImportFromPathAsync(path);

        if (rootNode is null)
        {
            Log.Warning("ImportScanFile: failed to import '{Path}'", path);
            return;
        }

        Log.Information("ImportScanFile: successfully imported '{Path}'", path);
        App.MainDispatcherQueue?.TryEnqueue(() => DispatchImportedResult(rootNode, isColdStart));
    }

    private static void DispatchImportedResult(WinDirStat.Core.Entities.FileSystemNode rootNode, bool isColdStart)
    {
        if (isColdStart && App.RootViewModel is not null)
        {
            App.RootViewModel.LoadImportedResult(rootNode);
            return;
        }

        var windowManager = App.StaticServices?.GetService(typeof(IWindowManagerService)) as IWindowManagerService;
        windowManager?.OpenMainWindowWithImportedResult(rootNode);
    }

    private static void OpenScan(string? path, bool isColdStart)
    {
        if (isColdStart)
        {
            Log.Information("OpenScan: cold start, scanning '{Path}' in root window", path);
            App.RootViewModel?.ScanPathAsync(path);
            return;
        }

        Log.Information("OpenScan: redirect/second-instance, opening new window for '{Path}'", path);
        var windowManager = App.StaticServices?.GetService(typeof(IWindowManagerService)) as IWindowManagerService;
        windowManager?.OpenMainWindow(path);
    }

    private static string[] SplitCommandLine(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine)) return [];

        var argv = CommandLineToArgvW(commandLine, out var argc);
        if (argv == IntPtr.Zero)
        {
            Log.Warning("SplitCommandLine: CommandLineToArgvW failed for '{CommandLine}' (Win32Error={Error})",
                commandLine, Marshal.GetLastWin32Error());
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

    private static void HandleStartupTask()
    {
        Log.Information("HandleStartupTask: ensuring background scan task is registered");
        var registrar =
            App.StaticServices?.GetService(typeof(IBackgroundScanTaskRegistrar)) as IBackgroundScanTaskRegistrar;

        if (registrar is null)
        {
            Log.Warning("HandleStartupTask: IBackgroundScanTaskRegistrar not available");
            return;
        }

        registrar.EnsureRegistered();
        Log.Information("HandleStartupTask: EnsureRegistered completed");
    }

    public static void Handle(AppActivationArguments? args, bool isColdStart = false)
    {
        HandleExtracted(Extract(args), isColdStart);
    }
}