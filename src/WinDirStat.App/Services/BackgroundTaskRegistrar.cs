using System.Linq;
using Windows.ApplicationModel.Background;
using WinDirStat.Core.BackgroundScan;
using WinDirStat.Core.Interfaces;

namespace WinDirStat_App.Services;

public sealed class BackgroundTaskRegistrar(IBackgroundScanSettingsService settings) : IBackgroundScanTaskRegistrar
{
    private const string TaskName = "WinDirStat.BackgroundScan";

    public void EnsureRegistered()
    {
        var existingNames = BackgroundTaskRegistration.AllTasks.Values.Select(t => t.Name);
        if (BackgroundTaskRegistrationPolicy.IsAlreadyRegistered(existingNames, TaskName))
        {
            return;
        }

        Register();
    }

    public void ReRegister()
    {
        var existing = BackgroundTaskRegistration.AllTasks.Values
            .FirstOrDefault(t => t.Name == TaskName);
        existing?.Unregister(cancelTask: false);

        Register();
    }

    private void Register()
    {
        var builder = new BackgroundTaskBuilder
        {
            Name = TaskName,
            TaskEntryPoint = "Microsoft.Windows.ApplicationModel.Background.UniversalBGTask.Task"
        };

        builder.SetTrigger(new TimeTrigger(settings.ScanIntervalMinutes, false));
        builder.Register();
    }
}