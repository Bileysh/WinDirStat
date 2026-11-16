using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Volumetric.Services;

public static class ScanResultCipher
{
    public const int KeySize = 32;
    public const int DefaultChunkSize = 1024 * 1024;
    internal const int NonceSize = 12;
    internal const int NoncePrefixSize = 8;
    internal const int TagSize = 16;
    internal const int MaxChunkSize = 64 * 1024 * 1024;

    internal static ReadOnlySpan<byte> LegacyHeader => "VOLENC01"u8;
    internal static ReadOnlySpan<byte> ChunkedHeader => "VOLENC02"u8;

    public static int HeaderLength => ChunkedHeader.Length;

    public static byte[] GenerateKey() => RandomNumberGenerator.GetBytes(KeySize);

    public static bool IsEncrypted(ReadOnlySpan<byte> data) =>
        data.StartsWith(ChunkedHeader) || data.StartsWith(LegacyHeader);

    public static ScanResultEncryptingStream CreateEncryptingStream(Stream output, byte[] key,
        int chunkSize = DefaultChunkSize) => new(output, key, chunkSize);

    public static Stream OpenDecryptingStream(Stream input, byte[] key)
    {
        var header = new byte[HeaderLength];
        input.ReadExactly(header);

        if (header.AsSpan().SequenceEqual(ChunkedHeader))
        {
            return new ScanResultDecryptingStream(input, key);
        }

        if (header.AsSpan().SequenceEqual(LegacyHeader))
        {
            return new MemoryStream(DecryptLegacy(input, key));
        }

        throw new CryptographicException("The data is not an encrypted Volumetric scan result.");
    }

    internal static void FillNonce(Span<byte> nonce, ReadOnlySpan<byte> prefix, uint chunkIndex)
    {
        prefix.CopyTo(nonce);
        BinaryPrimitives.WriteUInt32BigEndian(nonce[NoncePrefixSize..], chunkIndex);
    }

    internal static void FillAssociatedData(Span<byte> associatedData, bool isFinal)
    {
        ChunkedHeader.CopyTo(associatedData);
        associatedData[^1] = isFinal ? (byte)1 : (byte)0;
    }

    private static byte[] DecryptLegacy(Stream input, byte[] key)
    {
        using var rest = new MemoryStream();
        input.CopyTo(rest);
        var data = rest.GetBuffer().AsSpan(0, (int)rest.Length);

        if (data.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("The encrypted scan result is truncated.");
        }

        var plaintext = new byte[data.Length - NonceSize - TagSize];
        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(data[..NonceSize], data[(NonceSize + TagSize)..], data.Slice(NonceSize, TagSize), plaintext,
            LegacyHeader);
        return plaintext;
    }
}

public sealed class ScanResultEncryptingStream : Stream
{
    private readonly Stream _output;
    private readonly AesGcm _aes;
    private readonly byte[] _noncePrefix = RandomNumberGenerator.GetBytes(ScanResultCipher.NoncePrefixSize);
    private readonly byte[] _plaintext;
    private readonly byte[] _ciphertext;
    private int _buffered;
    private uint _chunkIndex;
    private bool _completed;

    internal ScanResultEncryptingStream(Stream output, byte[] key, int chunkSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chunkSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(chunkSize, ScanResultCipher.MaxChunkSize);

        _output = output;
        _aes = new AesGcm(key, ScanResultCipher.TagSize);
        _plaintext = new byte[chunkSize];
        _ciphertext = new byte[chunkSize];

        _output.Write(ScanResultCipher.ChunkedHeader);
        _output.Write(_noncePrefix);
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => !_completed;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_completed, this);

        while (!buffer.IsEmpty)
        {
            if (_buffered == _plaintext.Length)
            {
                WriteChunk(isFinal: false);
            }

            var count = Math.Min(buffer.Length, _plaintext.Length - _buffered);
            buffer[..count].CopyTo(_plaintext.AsSpan(_buffered));
            _buffered += count;
            buffer = buffer[count..];
        }
    }

    public void Complete()
    {
        ObjectDisposedException.ThrowIf(_completed, this);

        WriteChunk(isFinal: true);
        _output.Flush();
        _completed = true;
    }

    private void WriteChunk(bool isFinal)
    {
        Span<byte> nonce = stackalloc byte[ScanResultCipher.NonceSize];
        Span<byte> associatedData = stackalloc byte[ScanResultCipher.HeaderLength + 1];
        Span<byte> tag = stackalloc byte[ScanResultCipher.TagSize];
        Span<byte> recordHeader = stackalloc byte[5];

        ScanResultCipher.FillNonce(nonce, _noncePrefix, _chunkIndex++);
        ScanResultCipher.FillAssociatedData(associatedData, isFinal);
        _aes.Encrypt(nonce, _plaintext.AsSpan(0, _buffered), _ciphertext.AsSpan(0, _buffered), tag, associatedData);

        recordHeader[0] = isFinal ? (byte)1 : (byte)0;
        BinaryPrimitives.WriteInt32LittleEndian(recordHeader[1..], _buffered);
        _output.Write(recordHeader);
        _output.Write(_ciphertext.AsSpan(0, _buffered));
        _output.Write(tag);
        _buffered = 0;
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _aes.Dispose();
            _completed = true;
        }

        base.Dispose(disposing);
    }
}

public sealed class ScanResultDecryptingStream : Stream
{
    private readonly Stream _input;
    private readonly AesGcm _aes;
    private readonly byte[] _noncePrefix = new byte[ScanResultCipher.NoncePrefixSize];
    private byte[] _plaintext = [];
    private byte[] _ciphertext = [];
    private int _available;
    private int _position;
    private uint _chunkIndex;
    private bool _finalChunkRead;

    internal ScanResultDecryptingStream(Stream input, byte[] key)
    {
        _input = input;
        _aes = new AesGcm(key, ScanResultCipher.TagSize);
        ReadExactlyOrThrow(_noncePrefix);
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        while (_position == _available)
        {
            if (_finalChunkRead)
            {
                if (_input.ReadByte() != -1)
                {
                    throw new CryptographicException("The encrypted scan result has data after its final chunk.");
                }

                return 0;
            }

            ReadNextChunk();
        }

        var count = Math.Min(buffer.Length, _available - _position);
        _plaintext.AsSpan(_position, count).CopyTo(buffer);
        _position += count;
        return count;
    }

    private void ReadNextChunk()
    {
        Span<byte> recordHeader = stackalloc byte[5];
        ReadExactlyOrThrow(recordHeader);

        var isFinal = recordHeader[0] switch
        {
            0 => false,
            1 => true,
            _ => throw new CryptographicException("The encrypted scan result is corrupted.")
        };
        var length = BinaryPrimitives.ReadInt32LittleEndian(recordHeader[1..]);
        if (length < 0 || length > ScanResultCipher.MaxChunkSize)
        {
            throw new CryptographicException("The encrypted scan result is corrupted.");
        }

        if (_ciphertext.Length < length)
        {
            _ciphertext = new byte[length];
            _plaintext = new byte[length];
        }

        Span<byte> tag = stackalloc byte[ScanResultCipher.TagSize];
        Span<byte> nonce = stackalloc byte[ScanResultCipher.NonceSize];
        Span<byte> associatedData = stackalloc byte[ScanResultCipher.HeaderLength + 1];

        ReadExactlyOrThrow(_ciphertext.AsSpan(0, length));
        ReadExactlyOrThrow(tag);

        ScanResultCipher.FillNonce(nonce, _noncePrefix, _chunkIndex++);
        ScanResultCipher.FillAssociatedData(associatedData, isFinal);
        _aes.Decrypt(nonce, _ciphertext.AsSpan(0, length), tag, _plaintext.AsSpan(0, length), associatedData);

        _available = length;
        _position = 0;
        _finalChunkRead = isFinal;
    }

    private void ReadExactlyOrThrow(Span<byte> buffer)
    {
        try
        {
            _input.ReadExactly(buffer);
        }
        catch (EndOfStreamException ex)
        {
            throw new CryptographicException("The encrypted scan result is truncated.", ex);
        }
    }

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _aes.Dispose();
        }

        base.Dispose(disposing);
    }
}
