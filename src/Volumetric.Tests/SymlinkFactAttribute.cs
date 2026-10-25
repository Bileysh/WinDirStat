namespace Volumetric.Tests;

public sealed class SymlinkFactAttribute : FactAttribute
{
    private static readonly Lazy<bool> CanCreateSymlinks = new(Probe);

    public SymlinkFactAttribute()
    {
        if (!CanCreateSymlinks.Value)
        {
            Skip = "Creating symbolic links requires Developer Mode or administrator rights.";
        }
    }

    private static bool Probe()
    {
        var probeDir = Directory.CreateTempSubdirectory("volumetric-symlink-probe-");
        try
        {
            var target = Path.Combine(probeDir.FullName, "target.txt");
            File.WriteAllText(target, "x");
            File.CreateSymbolicLink(Path.Combine(probeDir.FullName, "link.txt"), target);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
        finally
        {
            probeDir.Delete(recursive: true);
        }
    }
}
