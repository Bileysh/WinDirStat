using Volumetric.Core.BackgroundScan;

namespace Volumetric.Tests.Tests;

public class BackgroundTaskRegistrationPolicyTests
{
    private const string TaskName = "Volumetric.BackgroundScan";

    [Fact]
    public void IsAlreadyRegistered_ReturnsFalse_WhenNoTasksExist()
    {
        var existing = Array.Empty<string>();

        Assert.False(BackgroundTaskRegistrationPolicy.IsAlreadyRegistered(existing, TaskName));
    }

    [Fact]
    public void IsAlreadyRegistered_ReturnsFalse_WhenOnlyOtherTasksExist()
    {
        var existing = new[] { "SomeOtherApp.Task", "AnotherTask" };

        Assert.False(BackgroundTaskRegistrationPolicy.IsAlreadyRegistered(existing, TaskName));
    }

    [Fact]
    public void IsAlreadyRegistered_ReturnsTrue_WhenTaskNameMatches()
    {
        var existing = new[] { "SomeOtherApp.Task", TaskName };

        Assert.True(BackgroundTaskRegistrationPolicy.IsAlreadyRegistered(existing, TaskName));
    }

    [Fact]
    public void IsAlreadyRegistered_IsCaseSensitive()
    {
        var existing = new[] { TaskName.ToUpperInvariant() };

        Assert.False(BackgroundTaskRegistrationPolicy.IsAlreadyRegistered(existing, TaskName));
    }

    [Fact]
    public void FindLegacyRegistrations_ReturnsEmpty_WhenNoLegacyNamesPresent()
    {
        var existing = new[] { TaskName, "SomeOtherApp.Task" };
        var legacyNames = new[] { "WinDirStat.BackgroundScan" };

        Assert.Empty(BackgroundTaskRegistrationPolicy.FindLegacyRegistrations(existing, legacyNames));
    }

    [Fact]
    public void FindLegacyRegistrations_ReturnsMatch_WhenLegacyNamePresent()
    {
        var existing = new[] { TaskName, "WinDirStat.BackgroundScan" };
        var legacyNames = new[] { "WinDirStat.BackgroundScan" };

        Assert.Equal(["WinDirStat.BackgroundScan"],
            BackgroundTaskRegistrationPolicy.FindLegacyRegistrations(existing, legacyNames));
    }

    [Fact]
    public void FindLegacyRegistrations_ReturnsAllMatches_WhenMultipleLegacyNamesRegistered()
    {
        var existing = new[] { "WinDirStat.BackgroundScan", "OldName.BackgroundScan", TaskName };
        var legacyNames = new[] { "WinDirStat.BackgroundScan", "OldName.BackgroundScan" };

        Assert.Equal(2, BackgroundTaskRegistrationPolicy.FindLegacyRegistrations(existing, legacyNames).Count());
    }

    [Fact]
    public void FindLegacyRegistrations_IsCaseSensitive()
    {
        var existing = new[] { "WINDIRSTAT.BACKGROUNDSCAN" };
        var legacyNames = new[] { "WinDirStat.BackgroundScan" };

        Assert.Empty(BackgroundTaskRegistrationPolicy.FindLegacyRegistrations(existing, legacyNames));
    }
}
