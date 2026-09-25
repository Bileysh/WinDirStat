using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;
using Serilog;
using Volumetric.Core.Interfaces;
using ActivationArgumentParser = Volumetric.Services.ActivationArgumentParser;

namespace Volumetric_App;

public static class ActivationDispatcher
{
    private const string ProtocolSchemeHost = "scan";
    private const string ProtocolPathMarker = ActivationArgumentParser.DefaultProtocolPathMarker;
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
        if (args is null)
        {
            return new ExtractedActivation(ActivationAction.None, null);
        }

        switch (args.Kind)
        {
            case ExtendedActivationKind.Launch:
                if (args.Data is ICommandLineActivatedEventArgs cmdArgs)
                {
                    var rawArgs = cmdArgs.Operation.Arguments;
                    Log.Debug("Extract[Launch]: raw Operation.Arguments = '{RawArgs}'", rawArgs);

                    var parsed = ActivationArgumentParser.ParseLaunchArguments(rawArgs, Directory.Exists);
                    Log.Information("Extract[Launch]: candidate path='{Path}', kind={Kind}", parsed.Path,
                        parsed.Kind);

                    if (parsed.Kind != ActivationArgumentParser.ParsedKind.None)
                    {
                        return new ExtractedActivation(
                            parsed.Kind == ActivationArgumentParser.ParsedKind.Path
                                ? ActivationAction.Path
                                : ActivationAction.InvalidPath, parsed.Path);
                    }
                }
                else if (args.Data is ILaunchActivatedEventArgs launchArgs)
                {
                    var rawLaunchArgs = launchArgs.Arguments;
                    Log.Debug("Extract[Launch]: ILaunchActivatedEventArgs.Arguments = '{RawArgs}'", rawLaunchArgs);

                    var parsed = ActivationArgumentParser.ParseLaunchArguments(rawLaunchArgs, Directory.Exists);
                    Log.Information(
                        "Extract[Launch/ILaunchActivatedEventArgs]: candidate path='{Path}', kind={Kind}",
                        parsed.Path, parsed.Kind);

                    if (parsed.Kind != ActivationArgumentParser.ParsedKind.None)
                    {
                        return new ExtractedActivation(
                            parsed.Kind == ActivationArgumentParser.ParsedKind.Path
                                ? ActivationAction.Path
                                : ActivationAction.InvalidPath, parsed.Path);
                    }
                }
                else
                {
                    Log.Debug("Extract[Launch]: args.Data was not ICommandLineActivatedEventArgs ({Type})",
                        args.Data?.GetType().Name);
                }

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

                    var path = ActivationArgumentParser.ExtractPathFromProtocolUri(uriStr, ProtocolPathMarker);
                    Log.Information("Extract[Protocol]: decoded path='{Path}'", path);

                    if (path is not null)
                    {
                        return new ExtractedActivation(
                            Directory.Exists(path) ? ActivationAction.Path : ActivationAction.InvalidPath, path);
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
        var title = localization?.GetString(ResourceKeys.InvalidPathTitle) ?? DefaultInvalidPathTitle;
        notificationService?.ShowNotification(title, path ?? string.Empty);
    }

    private static async void ImportScanFile(string? path, bool isColdStart)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        var fileService = App.StaticServices?.GetService(typeof(IScanResultFileService)) as IScanResultFileService;
        if (fileService is null)
        {
            return;
        }

        var rootNode = await fileService.ImportFromPathAsync(path);

        if (rootNode is null)
        {
            Log.Warning("ImportScanFile: failed to import '{Path}'", path);
            return;
        }

        Log.Information("ImportScanFile: successfully imported '{Path}'", path);
        App.MainDispatcherQueue?.TryEnqueue(() => DispatchImportedResult(rootNode, isColdStart));
    }

    private static void DispatchImportedResult(Volumetric.Core.Entities.FileSystemNode rootNode, bool isColdStart)
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

    private static void HandleStartupTask()
    {
        Log.Information("Extract[StartupTask]: activation received, no action needed (task already registered)");
    }

    public static void Handle(AppActivationArguments? args, bool isColdStart = false)
    {
        HandleExtracted(Extract(args), isColdStart);
    }
}
