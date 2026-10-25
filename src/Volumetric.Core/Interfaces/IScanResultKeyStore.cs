namespace Volumetric.Core.Interfaces;

public interface IScanResultKeyStore
{
    byte[] GetOrCreateKey();
    byte[]? TryGetKey();
}
