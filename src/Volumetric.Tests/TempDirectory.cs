namespace Volumetric.Tests;

public sealed class TempDirectory : IDisposable
{
    private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("volumetric-test-");

    public string FullName => _directory.FullName;

    public DirectoryInfo CreateSubdirectory(string path) => _directory.CreateSubdirectory(path);

    public void Dispose()
    {
        var links = _directory
            .EnumerateDirectories("*", new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = 0 })
            .Where(d => d.Attributes.HasFlag(FileAttributes.ReparsePoint))
            .ToList();

        foreach (var link in links)
        {
            link.Delete();
        }

        _directory.Delete(recursive: true);
    }
}
