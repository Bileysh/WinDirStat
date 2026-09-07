using System.Diagnostics;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;
using WinDirStat.Core.Interfaces;
using System.Threading.Tasks;

namespace WinDirStat_App;

public static class ActivationDispatcher
{
    public static void Handle(AppActivationArguments? args)
    {
        if (args is null) return;

        switch (args.Kind)
        {
            case ExtendedActivationKind.Launch:
                HandleLaunch(args);
                break;
            case ExtendedActivationKind.File:
                HandleFile(args);
                break;
            case ExtendedActivationKind.Protocol:
                HandleProtocol(args);
                break;
            case ExtendedActivationKind.StartupTask:
                HandleStartupTask(args);
                break;
            default:
                Debug.WriteLine($"[ActivationDispatcher] Непідтримуваний тип активації: {args.Kind}");
                break;
        }
    }

    private static void HandleLaunch(AppActivationArguments args)
    {
        if (args.Data is ICommandLineActivatedEventArgs cmdArgs)
        {
            var path = cmdArgs.Operation.Arguments.Trim(' ', '"');
            if (!string.IsNullOrWhiteSpace(path) && System.IO.Directory.Exists(path))
            {
                App.MainDispatcherQueue?.TryEnqueue(() => App.RootViewModel?.ScanPathAsync(path));
            }
        }
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
                Debug.WriteLine($"[ActivationDispatcher] File-активація: не вдалось імпортувати '{path}'.");
                return;
            }

            App.MainDispatcherQueue?.TryEnqueue(() => App.RootViewModel?.LoadImportedResult(rootNode));
        });
    }

    private static void HandleProtocol(AppActivationArguments args)
    {
        if (args.Data is IProtocolActivatedEventArgs protocolArgs)
        {
            var uri = protocolArgs.Uri;
            if (uri.Host.Equals("scan", StringComparison.OrdinalIgnoreCase))
            {
                var path = uri.Query.Replace("?path=", "").Trim();
                if (!string.IsNullOrEmpty(path) && System.IO.Directory.Exists(path))
                {
                    App.MainDispatcherQueue?.TryEnqueue(() => App.RootViewModel?.ScanPathAsync(path));
                }
            }
        }
    }

    private static void HandleStartupTask(AppActivationArguments args)
    {
        Debug.WriteLine("[ActivationDispatcher] StartupTask-активація виконана.");
    }
}