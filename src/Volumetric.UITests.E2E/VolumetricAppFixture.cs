using System.Diagnostics;

namespace Volumetric.UITests.E2E;

public sealed class VolumetricAppFixture : IDisposable
{
    private const string PackageIdentityName = "4B18DC31-5920-4065-AAE7-05908E7C027E";
    private const string ApplicationId = "App";
    private static readonly Uri WinAppDriverUri = new("http://127.0.0.1:4723");

    public WinAppDriverSession Session { get; }

    public VolumetricAppFixture()
    {
        KillRunningInstances();
        Session = WinAppDriverSession.StartAsync(WinAppDriverUri, ResolveAppUserModelId()).GetAwaiter().GetResult();
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

    private static string ResolveAppUserModelId()
    {
        var familyName = RunPowerShell(
            $"(Get-AppxPackage -Name '{PackageIdentityName}' | Select-Object -First 1).PackageFamilyName");

        if (string.IsNullOrWhiteSpace(familyName))
        {
            throw new InvalidOperationException(
                $"No installed package found for identity '{PackageIdentityName}'. Build and install Volumetric " +
                "first (see src/Volumetric.UITests.E2E/README.md) and make sure WinAppDriver.exe is running.");
        }

        return $"{familyName}!{ApplicationId}";
    }

    private static string RunPowerShell(string command)
    {
        var startInfo = new ProcessStartInfo("powershell.exe",
            $"-NoProfile -NonInteractive -Command \"{command}\"")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();
        return output;
    }

    public void Dispose() => Session.Dispose();
}
