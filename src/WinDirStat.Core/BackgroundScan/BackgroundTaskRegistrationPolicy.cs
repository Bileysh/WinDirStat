namespace WinDirStat.Core.BackgroundScan;

public static class BackgroundTaskRegistrationPolicy
{
    public static bool IsAlreadyRegistered(IEnumerable<string> existingTaskNames, string taskName) =>
        existingTaskNames.Contains(taskName, StringComparer.Ordinal);
}
