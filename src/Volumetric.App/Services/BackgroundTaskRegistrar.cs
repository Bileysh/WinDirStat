using System.Linq;
using Serilog;
using Windows.ApplicationModel.Background;
using Volumetric.Core.BackgroundScan;
using Volumetric.Core.Interfaces;

namespace Volumetric_App.Services;

public sealed class BackgroundTaskRegistrar(IBackgroundScanSettingsService settings) : IBackgroundScanTaskRegistrar
{
    private const string TaskName = "Volumetric.BackgroundScan";
    private static readonly string[] LegacyTaskNames = ["WinDirStat.BackgroundScan"];

    public void EnsureRegistered()
    {
        var existingNames = BackgroundTaskRegistration.AllTasks.Values.Select(t => t.Name).ToList();

        if (!BackgroundTaskRegistrationPolicy.IsAlreadyRegistered(existingNames, TaskName))
        {
            try
            {
                Register();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "EnsureRegistered: failed to register '{TaskName}'; leaving any " +
                              "legacy registration in place.", TaskName);
                return;
            }
        }

        MigrateLegacyRegistrations();
    }

    private static void MigrateLegacyRegistrations()
    {
        var existingNames = BackgroundTaskRegistration.AllTasks.Values.Select(t => t.Name).ToList();
        var legacyNames = BackgroundTaskRegistrationPolicy.FindLegacyRegistrations(existingNames, LegacyTaskNames);

        foreach (var legacyName in legacyNames)
        {
            var legacy = BackgroundTaskRegistration.AllTasks.Values.FirstOrDefault(t => t.Name == legacyName);
            if (legacy is null)
                continue;

            Log.Information("Migrating orphaned legacy background task registration '{LegacyTaskName}'", legacyName);
            legacy.Unregister(cancelTask: false);
        }
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
