using System.Text.Json;
using System.Text.Json.Serialization;
using Volumetric.Core.Entities;
using Volumetric.Core.Interfaces;
using Volumetric.Services;

namespace Volumetric_App.Services;

public sealed class ScanResultSerializer(ISecuritySettingsService securitySettings, IScanResultKeyStore keyStore)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        TypeInfoResolver = FileSystemNodeJsonContext.Default,
        MaxDepth = 256,
        PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate
    };

    public async Task WriteAsync(Stream stream, FileSystemNode rootNode)
    {
        stream.SetLength(0);

        if (!securitySettings.EncryptScanResults)
        {
            await JsonSerializer.SerializeAsync(stream, rootNode, JsonOptions);
            return;
        }

        var json = JsonSerializer.SerializeToUtf8Bytes(rootNode, JsonOptions);
        await stream.WriteAsync(ScanResultCipher.Encrypt(json, keyStore.GetOrCreateKey()));
    }

    public async Task<FileSystemNode?> ReadAsync(Stream stream)
    {
        var header = new byte[ScanResultCipher.HeaderLength];
        var headerLength = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);
        stream.Seek(0, SeekOrigin.Begin);

        var rootNode = ScanResultCipher.IsEncrypted(header.AsSpan(0, headerLength))
            ? await ReadEncryptedAsync(stream)
            : await JsonSerializer.DeserializeAsync<FileSystemNode>(stream, JsonOptions);

        rootNode?.EstablishParentLinksRecursively();
        return rootNode;
    }

    private async Task<FileSystemNode?> ReadEncryptedAsync(Stream stream)
    {
        var key = keyStore.TryGetKey() ?? throw new ScanResultKeyUnavailableException();

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        var json = ScanResultCipher.Decrypt(buffer.GetBuffer().AsSpan(0, (int)buffer.Length), key);

        return JsonSerializer.Deserialize<FileSystemNode>(json, JsonOptions);
    }
}
