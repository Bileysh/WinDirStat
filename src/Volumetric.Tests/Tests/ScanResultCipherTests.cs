using System.Security.Cryptography;
using System.Text;
using Volumetric.Services;

namespace Volumetric.Tests.Tests;

public class ScanResultCipherTests
{
    private static readonly byte[] Plaintext = Encoding.UTF8.GetBytes("{\"Name\":\"C:\\\\secret-folder\"}");

    [Fact]
    public void EncryptThenDecrypt_ReturnsOriginalBytes()
    {
        var key = ScanResultCipher.GenerateKey();

        var encrypted = ScanResultCipher.Encrypt(Plaintext, key);

        Assert.Equal(Plaintext, ScanResultCipher.Decrypt(encrypted, key));
    }

    [Fact]
    public void Encrypt_OutputIsMarkedAndDoesNotContainPlaintext()
    {
        var encrypted = ScanResultCipher.Encrypt(Plaintext, ScanResultCipher.GenerateKey());

        Assert.True(ScanResultCipher.IsEncrypted(encrypted));
        Assert.DoesNotContain("secret-folder", Encoding.UTF8.GetString(encrypted));
    }

    [Fact]
    public void Encrypt_SamePlaintextTwice_ProducesDifferentOutput()
    {
        var key = ScanResultCipher.GenerateKey();

        Assert.NotEqual(ScanResultCipher.Encrypt(Plaintext, key), ScanResultCipher.Encrypt(Plaintext, key));
    }

    [Fact]
    public void IsEncrypted_PlainJson_IsFalse()
    {
        Assert.False(ScanResultCipher.IsEncrypted(Plaintext));
    }

    [Fact]
    public void Decrypt_WithWrongKey_Throws()
    {
        var encrypted = ScanResultCipher.Encrypt(Plaintext, ScanResultCipher.GenerateKey());

        Assert.ThrowsAny<CryptographicException>(() =>
            ScanResultCipher.Decrypt(encrypted, ScanResultCipher.GenerateKey()));
    }

    [Fact]
    public void Decrypt_TamperedCiphertext_Throws()
    {
        var key = ScanResultCipher.GenerateKey();
        var encrypted = ScanResultCipher.Encrypt(Plaintext, key);
        encrypted[^1] ^= 0xFF;

        Assert.ThrowsAny<CryptographicException>(() => ScanResultCipher.Decrypt(encrypted, key));
    }

    [Fact]
    public void Decrypt_PlainData_Throws()
    {
        Assert.ThrowsAny<CryptographicException>(() =>
            ScanResultCipher.Decrypt(Plaintext, ScanResultCipher.GenerateKey()));
    }
}
