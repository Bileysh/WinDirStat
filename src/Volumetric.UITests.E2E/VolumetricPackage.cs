using System.Diagnostics;

namespace Volumetric.UITests.E2E;

public static class VolumetricPackage
{
    public const string IdentityName = "4B18DC31-5920-4065-AAE7-05908E7C027E";
    private const string ApplicationId = "App";

    public static string? FindFamilyName()
    {
        var familyName = RunPowerShell(
            $"(Get-AppxPackage -Name '{IdentityName}' | Select-Object -First 1).PackageFamilyName");

        return string.IsNullOrWhiteSpace(familyName) ? null : familyName;
    }

    public static string GetAppUserModelId() =>
        $"{FindFamilyName() ?? throw new InvalidOperationException($"Package '{IdentityName}' is not installed.")}!{ApplicationId}";

    private static string RunPowerShell(string command)
    {
        var startInfo = new ProcessStartInfo("powershell.exe", $"-NoProfile -NonInteractive -Command \"{command}\"")
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
}
