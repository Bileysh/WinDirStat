namespace Volumetric.Core.BackgroundScan;

public static class BackgroundTaskRegistrationPolicy
{
    public static bool IsAlreadyRegistered(IEnumerable<string> existingTaskNames, string taskName) =>
        existingTaskNames.Contains(taskName, StringComparer.Ordinal);

    public static IEnumerable<string> FindLegacyRegistrations(
        IEnumerable<string> existingTaskNames, IReadOnlyCollection<string> legacyTaskNames) =>
        existingTaskNames.Where(n => legacyTaskNames.Contains(n, StringComparer.Ordinal));
}
