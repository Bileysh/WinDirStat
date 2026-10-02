using System.Diagnostics;

namespace Volumetric.UITests.E2E;

public sealed class VolumetricAppFixture : IDisposable
{
    private readonly Process? _startedDriver;
    private readonly WinAppDriverSession? _session;

    public WinAppDriverSession Session =>
        _session ?? throw new InvalidOperationException(E2EPrerequisites.MissingPrerequisite.Value);

    public VolumetricAppFixture()
    {
        if (E2EPrerequisites.MissingPrerequisite.Value is not null)
        {
            return;
        }

        KillRunningInstances();
        _startedDriver = WinAppDriverServer.EnsureRunning();

        try
        {
            _session = WinAppDriverSession.StartAsync(WinAppDriverServer.Uri, VolumetricPackage.GetAppUserModelId())
                .GetAwaiter().GetResult();
        }
        catch
        {
            KillRunningInstances();
            StopStartedDriver();
            throw;
        }
    }

    private static void KillRunningInstances()
    {
        foreach (var process in Process.GetProcessesByName("Volumetric.App"))
        {
            try
            {
                process.Kill();
                process.WaitForExit(5000);
            }
            catch (InvalidOperationException)
            {
            }
        }
    }

    private void StopStartedDriver()
    {
        if (_startedDriver is { HasExited: false })
        {
            _startedDriver.Kill();
            _startedDriver.WaitForExit(5000);
        }

        _startedDriver?.Dispose();
    }

    public void Dispose()
    {
        _session?.Dispose();
        StopStartedDriver();
    }
}
