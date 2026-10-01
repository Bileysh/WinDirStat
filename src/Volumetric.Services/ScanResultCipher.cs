using System.Security.Cryptography;

namespace Volumetric.Services;

public static class ScanResultCipher
{
    public const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private static ReadOnlySpan<byte> Header => "VOLENC01"u8;
    private static int PrefixSize => Header.Length + NonceSize + TagSize;

    public static int HeaderLength => Header.Length;

    public static byte[] GenerateKey() => RandomNumberGenerator.GetBytes(KeySize);

    public static bool IsEncrypted(ReadOnlySpan<byte> data) => data.StartsWith(Header);

    public static byte[] Encrypt(ReadOnlySpan<byte> plaintext, byte[] key)
    {
        var output = new byte[PrefixSize + plaintext.Length];
        var span = output.AsSpan();

        Header.CopyTo(span);
        var nonce = span.Slice(Header.Length, NonceSize);
        var tag = span.Slice(Header.Length + NonceSize, TagSize);
        var ciphertext = span[PrefixSize..];

        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, Header);

        return output;
    }

    public static byte[] Decrypt(ReadOnlySpan<byte> data, byte[] key)
    {
        if (!IsEncrypted(data) || data.Length < PrefixSize)
        {
            throw new CryptographicException("The data is not an encrypted Volumetric scan result.");
        }

        var nonce = data.Slice(Header.Length, NonceSize);
        var tag = data.Slice(Header.Length + NonceSize, TagSize);
        var ciphertext = data[PrefixSize..];
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext, Header);

        return plaintext;
    }
}
