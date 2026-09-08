using System.Diagnostics;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;
using WinDirStat.Core.Interfaces;

namespace WinDirStat_App;

public static class ActivationDispatcher
{
    public static void Handle(AppActivationArguments? args, bool isColdStart = false)
    {
        if (args is null) return;

        switch (args.Kind)
        {
            case ExtendedActivationKind.Launch:
                HandleLaunch(args, isColdStart);
                break;
            case ExtendedActivationKind.File:
                HandleFile(args);
                break;
            case ExtendedActivationKind.Protocol:
                HandleProtocol(args, isColdStart);
                break;
            case ExtendedActivationKind.StartupTask:
                HandleStartupTask(args);
                break;
            default:
                Debug.WriteLine($"[ActivationDispatcher] Unsupported activation kind: {args.Kind}");
                break;
        }
    }

    private static void HandleLaunch(AppActivationArguments args, bool isColdStart)
    {
        if (args.Data is ICommandLineActivatedEventArgs cmdArgs)
        {
            var rawArgs = cmdArgs.Operation.Arguments;
            if (string.IsNullOrWhiteSpace(rawArgs)) return;

            var path = ParseFirstPathArgument(rawArgs);
            if (path is null) return;

            if (Directory.Exists(path))
            {
                App.MainDispatcherQueue?.TryEnqueue(() => OpenScan(path, isColdStart));
            }
            else
            {
                Debug.WriteLine($"[ActivationDispatcher] Terminal launch: path does not exist: '{path}'");
                App.MainDispatcherQueue?.TryEnqueue(() =>
                {
                    var notificationService = App.StaticServices?.GetService(typeof(INotificationService)) as INotificationService;
                    var localization = App.StaticServices?.GetService(typeof(ILocalizationService)) as ILocalizationService;
                    notificationService?.ShowNotification(localization?.GetString("InvalidPathTitle"), path);
                });
            }
        }
    }
    
    private static void OpenScan(string path, bool isColdStart)
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

    private static void HandleFile(AppActivationArguments args)
    {
        if (args.Data is not IFileActivatedEventArgs fileArgs || fileArgs.Files.Count == 0) return;

        var path = fileArgs.Files[0].Path;

        Task.Run(() =>
        {
            var fileService = App.StaticServices?.GetService(typeof(IScanResultFileService)) as IScanResultFileService;
            var rootNode = fileService?.ImportFromPath(path);

            if (rootNode is null)
            {
                Debug.WriteLine($"[ActivationDispatcher] File activation: failed to import '{path}'.");
                return;
            }

            App.MainDispatcherQueue?.TryEnqueue(() =>
            {
                var windowManager =
                    App.StaticServices?.GetService(typeof(IWindowManagerService)) as IWindowManagerService;
                windowManager?.OpenMainWindowWithImportedResult(rootNode);
            });
        });
    }

    private static void HandleProtocol(AppActivationArguments args, bool isColdStart)
    {
        if (args.Data is IProtocolActivatedEventArgs protocolArgs)
        {
            var uri = protocolArgs.Uri;
            if (uri.Host.Equals("scan", StringComparison.OrdinalIgnoreCase))
            {
                var path = uri.Query.Replace("?path=", "").Trim();
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                {
                    App.MainDispatcherQueue?.TryEnqueue(() => OpenScan(path, isColdStart));
                }
            }
        }
    }

    private static void HandleStartupTask(AppActivationArguments args)
    {
        Debug.WriteLine("[ActivationDispatcher] StartupTask activation — background-only, no window shown.");
        var registrar = App.StaticServices?.GetService(typeof(IBackgroundScanTaskRegistrar)) as IBackgroundScanTaskRegistrar;
        if (registrar != null)
        {
            registrar.EnsureRegistered();
        }
    }
}