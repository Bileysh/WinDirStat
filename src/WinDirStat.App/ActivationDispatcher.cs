using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;
using WinDirStat.Core.Interfaces;

namespace WinDirStat_App;

public static class ActivationDispatcher
{
    public enum ActivationAction
    {
        None,
        Path,
        File,
        InvalidPath
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
                    if (string.IsNullOrWhiteSpace(rawArgs)) break;

                    var path = ParseFirstPathArgument(rawArgs);
                    if (path is null) break;

                    return new ExtractedActivation(Directory.Exists(path) ? ActivationAction.Path : ActivationAction.InvalidPath, path);
                }

                break;

            case ExtendedActivationKind.File:
                if (args.Data is IFileActivatedEventArgs fileArgs && fileArgs.Files.Count > 0)
                {
                    return new ExtractedActivation(ActivationAction.File, fileArgs.Files[0].Path);
                }

                break;

            case ExtendedActivationKind.Protocol:
                if (args.Data is IProtocolActivatedEventArgs protocolArgs &&
                    protocolArgs.Uri.Host.Equals("scan", StringComparison.OrdinalIgnoreCase))
                {
                    var path = protocolArgs.Uri.Query.Replace("?path=", "").Trim();
                    if (!string.IsNullOrEmpty(path))
                    {
                        return new ExtractedActivation(Directory.Exists(path) ? ActivationAction.Path : ActivationAction.InvalidPath, path);
                    }
                }

                break;

            case ExtendedActivationKind.StartupTask:
                HandleStartupTask();
                break;
        }

        return new ExtractedActivation(ActivationAction.None, null);
    }

    public static void HandleExtracted(ExtractedActivation extracted, bool isColdStart)
    {
        switch (extracted.Action)
        {
            case ActivationAction.Path:
                OpenScan(extracted.Path, isColdStart);
                break;

            case ActivationAction.InvalidPath:
                var notificationService = App.StaticServices?.GetService(typeof(INotificationService)) as INotificationService;
                var localization = App.StaticServices?.GetService(typeof(ILocalizationService)) as ILocalizationService;
                notificationService?.ShowNotification(localization?.GetString("InvalidPathTitle"), extracted.Path);
                break;

            case ActivationAction.File:
                ImportScanFile(extracted.Path, isColdStart);
                break;
        }
    }

    private static void ImportScanFile(string? path, bool isColdStart)
    {
        Task.Run(() =>
        {
            var fileService = App.StaticServices?.GetService(typeof(IScanResultFileService)) as IScanResultFileService;
            var rootNode = fileService?.ImportFromPath(path);

            if (rootNode is null)
            {
                return;
            }

            App.MainDispatcherQueue?.TryEnqueue(() =>
            {
                if (isColdStart && App.RootViewModel is not null)
                {
                    App.RootViewModel.LoadImportedResult(rootNode);
                    return;
                }

                var windowManager = App.StaticServices?.GetService(typeof(IWindowManagerService)) as IWindowManagerService;
                windowManager?.OpenMainWindowWithImportedResult(rootNode);
            });
        });
    }

    private static void OpenScan(string? path, bool isColdStart)
    {
        if (isColdStart)
        {
            App.RootViewModel?.ScanPathAsync(path);
            return;
        }

        var windowManager = App.StaticServices?.GetService(typeof(IWindowManagerService)) as IWindowManagerService;
        windowManager?.OpenMainWindow(path);
    }

    private static string? ParseFirstPathArgument(string rawArgs)
    {
        var trimmed = rawArgs.Trim();
        if (trimmed.Length == 0) return null;

        if (trimmed[0] == '"')
        {
            var closingQuote = trimmed.IndexOf('"', 1);
            return closingQuote > 0 ? trimmed[1..closingQuote] : trimmed.Trim('"');
        }

        var firstSpace = trimmed.IndexOf(' ');
        return firstSpace > 0 ? trimmed[..firstSpace] : trimmed;
    }

    private static void HandleStartupTask()
    {
        var registrar = App.StaticServices?.GetService(typeof(IBackgroundScanTaskRegistrar)) as IBackgroundScanTaskRegistrar;
        registrar?.EnsureRegistered();
    }

    public static void Handle(AppActivationArguments? args, bool isColdStart = false)
    {
        HandleExtracted(Extract(args), isColdStart);
    }
}