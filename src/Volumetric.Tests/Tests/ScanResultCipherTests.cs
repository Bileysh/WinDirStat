using System.Security.Cryptography;
using System.Text;
using Volumetric.Services;

namespace Volumetric.Tests.Tests;

public class ScanResultCipherTests
{
    private const int SmallChunk = 64;

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(SmallChunk - 1)]
    [InlineData(SmallChunk)]
    [InlineData(SmallChunk + 1)]
    [InlineData(SmallChunk * 3 + SmallChunk / 2)]
    public void EncryptThenDecrypt_ReturnsOriginalBytes(int length)
    {
        var key = ScanResultCipher.GenerateKey();
        var plaintext = RandomNumberGenerator.GetBytes(length);

        Assert.Equal(plaintext, Decrypt(Encrypt(plaintext, key), key));
    }

    [Fact]
    public void Encrypt_OutputIsMarkedAndDoesNotContainPlaintext()
    {
        var encrypted = Encrypt(Encoding.UTF8.GetBytes("{\"Name\":\"C:\\\\secret-folder\"}"),
            ScanResultCipher.GenerateKey());

        Assert.True(ScanResultCipher.IsEncrypted(encrypted));
        Assert.DoesNotContain("secret-folder", Encoding.UTF8.GetString(encrypted));
    }

    [Fact]
    public void Encrypt_SamePlaintextTwice_ProducesDifferentOutput()
    {
        var key = ScanResultCipher.GenerateKey();
        var plaintext = RandomNumberGenerator.GetBytes(200);

        Assert.NotEqual(Encrypt(plaintext, key), Encrypt(plaintext, key));
    }

    [Fact]
    public void IsEncrypted_PlainJson_IsFalse()
    {
        Assert.False(ScanResultCipher.IsEncrypted("{\"Name\":\"C:\\\\\"}"u8));
    }

    [Fact]
    public void Decrypt_WithWrongKey_Throws()
    {
        var encrypted = Encrypt(RandomNumberGenerator.GetBytes(200), ScanResultCipher.GenerateKey());

        Assert.ThrowsAny<CryptographicException>(() => Decrypt(encrypted, ScanResultCipher.GenerateKey()));
    }

    [Fact]
    public void Decrypt_TamperedByteInTheMiddle_Throws()
    {
        var key = ScanResultCipher.GenerateKey();
        var encrypted = Encrypt(RandomNumberGenerator.GetBytes(SmallChunk * 4), key);
        encrypted[encrypted.Length / 2] ^= 0xFF;

        Assert.ThrowsAny<CryptographicException>(() => Decrypt(encrypted, key));
    }

    [Fact]
    public void Decrypt_FinalChunkRemoved_Throws()
    {
        var key = ScanResultCipher.GenerateKey();
        var encrypted = Encrypt(RandomNumberGenerator.GetBytes(SmallChunk * 3), key);
        var withoutFinalEmptyChunk = encrypted[..^(1 + 4 + 16)];

        Assert.ThrowsAny<CryptographicException>(() => Decrypt(withoutFinalEmptyChunk, key));
    }

    [Fact]
    public void Decrypt_TruncatedInsideAChunk_Throws()
    {
        var key = ScanResultCipher.GenerateKey();
        var encrypted = Encrypt(RandomNumberGenerator.GetBytes(SmallChunk * 3), key);

        Assert.ThrowsAny<CryptographicException>(() => Decrypt(encrypted[..(encrypted.Length / 2)], key));
    }

    [Fact]
    public void Decrypt_ChunksSwapped_Throws()
    {
        var key = ScanResultCipher.GenerateKey();
        var encrypted = Encrypt(RandomNumberGenerator.GetBytes(SmallChunk * 2), key);
        const int prefix = 8 + 8;
        const int record = 1 + 4 + SmallChunk + 16;
        var swapped = encrypted.ToArray();
        encrypted.AsSpan(prefix, record).CopyTo(swapped.AsSpan(prefix + record));
        encrypted.AsSpan(prefix + record, record).CopyTo(swapped.AsSpan(prefix));

        Assert.ThrowsAny<CryptographicException>(() => Decrypt(swapped, key));
    }

    [Fact]
    public void Decrypt_DataAppendedAfterFinalChunk_Throws()
    {
        var key = ScanResultCipher.GenerateKey();
        var encrypted = Encrypt(RandomNumberGenerator.GetBytes(100), key);

        Assert.ThrowsAny<CryptographicException>(() => Decrypt([.. encrypted, 0x00], key));
    }

    [Fact]
    public void Decrypt_PlainData_Throws()
    {
        Assert.ThrowsAny<CryptographicException>(() =>
            Decrypt("{\"Name\":\"C:\\\\\"}"u8.ToArray(), ScanResultCipher.GenerateKey()));
    }

    [Fact]
    public void Decrypt_LegacySingleShotFormat_StillWorks()
    {
        var key = ScanResultCipher.GenerateKey();
        var plaintext = RandomNumberGenerator.GetBytes(300);

        Assert.Equal(plaintext, Decrypt(EncryptLegacy(plaintext, key), key));
    }

    private static byte[] Encrypt(byte[] plaintext, byte[] key)
    {
        using var output = new MemoryStream();
        using (var encrypting = ScanResultCipher.CreateEncryptingStream(output, key, SmallChunk))
        {
            encrypting.Write(plaintext);
            encrypting.Complete();
        }

        return output.ToArray();
    }

    private static byte[] Decrypt(byte[] data, byte[] key)
    {
        using var input = new MemoryStream(data);
        using var decrypting = ScanResultCipher.OpenDecryptingStream(input, key);
        using var plaintext = new MemoryStream();
        decrypting.CopyTo(plaintext);
        return plaintext.ToArray();
    }

    private static byte[] EncryptLegacy(byte[] plaintext, byte[] key)
    {
        var header = "VOLENC01"u8;
        var output = new byte[header.Length + 12 + 16 + plaintext.Length];
        header.CopyTo(output);
        var nonce = output.AsSpan(header.Length, 12);
        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(key, 16);
        aes.Encrypt(nonce, plaintext, output.AsSpan(header.Length + 28), output.AsSpan(header.Length + 12, 16), header);
        return output;
    }
}
