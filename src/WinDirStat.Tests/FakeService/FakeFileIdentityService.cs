using WinDirStat.Core.Entities;
using WinDirStat.Core.Interfaces;

namespace WinDirStat.Tests.FakeService;

public class FakeFileIdentityService : IFileIdentityService
{
    public Dictionary<string, FileIdentity?> IdentitiesByPath { get; set; } = new();
    
    public int GetIdentityCallCount { get; private set; }

    public FileIdentity? GetIdentity(string fullPath) =>
        HandleCall(fullPath);

    private FileIdentity? HandleCall(string fullPath)
    {
        GetIdentityCallCount++;
        return IdentitiesByPath.GetValueOrDefault(fullPath);
    }
}
