using WinDirStat.Core.Entities;

namespace WinDirStat.Core.Interfaces;

public interface IFileIdentityService
{
    FileIdentity? GetIdentity(string fullPath);
}
