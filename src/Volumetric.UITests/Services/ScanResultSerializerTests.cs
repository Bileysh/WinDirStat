using System.Text;
using Volumetric_App.Services;
using Volumetric.Core.Entities;
using Volumetric.Core.Interfaces;
using Volumetric.Services;

namespace Volumetric.UITests.Services;

public class ScanResultSerializerTests
{
    [Fact]
    public async Task WriteThenRead_EncryptionDisabled_RoundTripsAsPlainJson()
    {
        var serializer = new ScanResultSerializer(new FakeSecuritySettings(false), new InMemoryKeyStore());
        using var stream = new MemoryStream();

        await serializer.WriteAsync(stream, CreateTree());

        Assert.False(ScanResultCipher.IsEncrypted(stream.ToArray()));
        Assert.Contains("secret-report.docx", Encoding.UTF8.GetString(stream.ToArray()));
        AssertTree(await ReadBackAsync(serializer, stream));
    }

    [Fact]
    public async Task WriteThenRead_EncryptionEnabled_RoundTripsAndHidesFileNames()
    {
        var serializer = new ScanResultSerializer(new FakeSecuritySettings(true), new InMemoryKeyStore());
        using var stream = new MemoryStream();

        await serializer.WriteAsync(stream, CreateTree());

        Assert.True(ScanResultCipher.IsEncrypted(stream.ToArray()));
        Assert.DoesNotContain("secret-report.docx", Encoding.UTF8.GetString(stream.ToArray()));
        AssertTree(await ReadBackAsync(serializer, stream));
    }

    [Fact]
    public async Task Read_PlainFileWhileEncryptionEnabled_StillImports()
    {
        using var stream = new MemoryStream();
        await new ScanResultSerializer(new FakeSecuritySettings(false), new InMemoryKeyStore())
            .WriteAsync(stream, CreateTree());

        var reader = new ScanResultSerializer(new FakeSecuritySettings(true), new InMemoryKeyStore());

        AssertTree(await ReadBackAsync(reader, stream));
    }

    [Fact]
    public async Task Read_EncryptedFileWithoutKey_ThrowsKeyUnavailable()
    {
        using var stream = new MemoryStream();
        await new ScanResultSerializer(new FakeSecuritySettings(true), new InMemoryKeyStore())
            .WriteAsync(stream, CreateTree());

        var reader = new ScanResultSerializer(new FakeSecuritySettings(true), new InMemoryKeyStore());
        stream.Position = 0;

        await Assert.ThrowsAsync<ScanResultKeyUnavailableException>(() => reader.ReadAsync(stream));
    }

    [Fact]
    public async Task Write_ReusesTheSameKeyAcrossExports()
    {
        var keyStore = new InMemoryKeyStore();
        var serializer = new ScanResultSerializer(new FakeSecuritySettings(true), keyStore);

        await serializer.WriteAsync(new MemoryStream(), CreateTree());
        var firstKey = keyStore.TryGetKey();
        await serializer.WriteAsync(new MemoryStream(), CreateTree());

        Assert.NotNull(firstKey);
        Assert.Equal(firstKey, keyStore.TryGetKey());
    }

    private static async Task<FileSystemNode?> ReadBackAsync(ScanResultSerializer serializer, MemoryStream stream)
    {
        stream.Position = 0;
        return await serializer.ReadAsync(stream);
    }

    private static FileSystemNode CreateTree()
    {
        var root = new FileSystemNode { Name = "C:\\", IsDirectory = true, SizeLogical = 42 };
        root.AddChild(new FileSystemNode
        {
            Name = "secret-report.docx", IsDirectory = false, Extension = ".docx", SizeLogical = 42
        });
        return root;
    }

    private static void AssertTree(FileSystemNode? root)
    {
        Assert.NotNull(root);
        Assert.Equal("C:\\", root.Name);
        var child = Assert.Single(root.Children);
        Assert.Equal("secret-report.docx", child.Name);
        Assert.Equal(42, child.SizeLogical);
        Assert.Same(root, child.Parent);
    }

    private sealed class FakeSecuritySettings(bool encrypt) : ISecuritySettingsService
    {
        public bool EncryptScanResults { get; set; } = encrypt;
        public bool RequireWindowsHelloOnLaunch { get; set; }
    }

    private sealed class InMemoryKeyStore : IScanResultKeyStore
    {
        private byte[]? _key;

        public byte[] GetOrCreateKey() => _key ??= ScanResultCipher.GenerateKey();

        public byte[]? TryGetKey() => _key;
    }
}
