using WinDirStat.Core.BackgroundScan;

namespace WinDirStat.Tests.Tests;

public class BackgroundTaskRegistrationPolicyTests
{
    private const string TaskName = "WinDirStat.BackgroundScan";

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
}
