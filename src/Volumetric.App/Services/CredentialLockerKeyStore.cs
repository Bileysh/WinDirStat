using Windows.Security.Credentials;
using Volumetric.Core.Interfaces;
using Volumetric.Services;

namespace Volumetric_App.Services;

public sealed class CredentialLockerKeyStore : IScanResultKeyStore
{
    private const string Resource = "Volumetric.ScanResultEncryption";
    private const string UserName = "ScanResultKey";
    private const int ElementNotFound = unchecked((int)0x80070490);

    private readonly PasswordVault _vault = new();
    private readonly Lock _gate = new();

    public byte[] GetOrCreateKey()
    {
        lock (_gate)
        {
            if (TryGetKey() is { } existing)
            {
                return existing;
            }

            var key = ScanResultCipher.GenerateKey();
            _vault.Add(new PasswordCredential(Resource, UserName, Convert.ToBase64String(key)));
            return key;
        }
    }

    public byte[]? TryGetKey()
    {
        try
        {
            var credential = _vault.Retrieve(Resource, UserName);
            credential.RetrievePassword();
            return Convert.FromBase64String(credential.Password);
        }
        catch (Exception ex) when (ex.HResult == ElementNotFound)
        {
            return null;
        }
    }
}
