namespace Volumetric.UITests.E2E;

public sealed class E2EFactAttribute : FactAttribute
{
    public E2EFactAttribute()
    {
        Skip = E2EPrerequisites.MissingPrerequisite.Value;
    }
}

public static class E2EPrerequisites
{
    public static readonly Lazy<string?> MissingPrerequisite = new(Check);

    private static string? Check()
    {
        if (WinAppDriverServer.ExecutablePath is null)
        {
            return "WinAppDriver is not installed (https://github.com/microsoft/WinAppDriver/releases).";
        }

        if (!WinAppDriverServer.IsDeveloperModeEnabled)
        {
            return "Developer Mode is disabled; WinAppDriver requires it.";
        }

        if (VolumetricPackage.FindFamilyName() is null)
        {
            return $"Volumetric package '{VolumetricPackage.IdentityName}' is not installed.";
        }

        return null;
    }
}
