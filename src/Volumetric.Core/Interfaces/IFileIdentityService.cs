using Volumetric.Core.Entities;

namespace Volumetric.Core.Interfaces;

public interface IFileIdentityService
{
    FileIdentity? GetIdentity(string fullPath);
}
