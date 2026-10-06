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
        MaxDepth = FileSystemNodeJsonContext.MaxTreeJsonDepth,
        PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate
    };

    public async Task WriteAsync(Stream stream, FileSystemNode rootNode)
    {
        stream.SetLength(0);

        if (!securitySettings.EncryptScanResults)
        {
            await LargeStack.RunAsync(() =>
            {
                JsonSerializer.Serialize(stream, rootNode, JsonOptions);
                return true;
            });
            return;
        }

        var key = keyStore.GetOrCreateKey();
        await LargeStack.RunAsync(() =>
        {
            using var encrypting = ScanResultCipher.CreateEncryptingStream(stream, key);
            JsonSerializer.Serialize(encrypting, rootNode, JsonOptions);
            encrypting.Complete();
            return true;
        });
    }

    public async Task<FileSystemNode?> ReadAsync(Stream stream)
    {
        var header = new byte[ScanResultCipher.HeaderLength];
        var headerLength = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);
        stream.Seek(0, SeekOrigin.Begin);

        if (!ScanResultCipher.IsEncrypted(header.AsSpan(0, headerLength)))
        {
            return await LargeStack.RunAsync(() => LinkParents(JsonSerializer.Deserialize<FileSystemNode>(stream, JsonOptions)));
        }

        var key = keyStore.TryGetKey() ?? throw new ScanResultKeyUnavailableException();

        return await LargeStack.RunAsync(() =>
        {
            using var decrypting = ScanResultCipher.OpenDecryptingStream(stream, key);
            var rootNode = JsonSerializer.Deserialize<FileSystemNode>(decrypting, JsonOptions);
            decrypting.CopyTo(Stream.Null);
            return LinkParents(rootNode);
        });
    }

    private static FileSystemNode? LinkParents(FileSystemNode? rootNode)
    {
        rootNode?.EstablishParentLinksRecursively();
        return rootNode;
    }
}
