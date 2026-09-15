using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinDirStat.Core.Entities;
using WinDirStat.Core.Interfaces;

namespace WinDirStat_App.Services;

public sealed class ScanResultFileService : IScanResultFileService
{
    private const string Extension = ".wdsscan";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        TypeInfoResolver = FileSystemNodeJsonContext.Default,
        MaxDepth = 256,
        PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate
    };

    public async Task<string?> ExportAsync(FileSystemNode rootNode, string suggestedFileName, IntPtr ownerHwnd)
    {
        var picker = new FileSavePicker();
        picker.FileTypeChoices.Add("Volumetric scan", [Extension]);
        picker.SuggestedFileName = suggestedFileName;
        WinRT.Interop.InitializeWithWindow.Initialize(picker, ownerHwnd);

        var file = await picker.PickSaveFileAsync();
        if (file is null) return null;

        using var stream = await file.OpenStreamForWriteAsync();
        stream.SetLength(0);
        await JsonSerializer.SerializeAsync(stream, rootNode, JsonOptions);

        return file.Name;
    }

    public async Task<(FileSystemNode RootNode, string FileName)?> ImportAsync(IntPtr ownerHwnd)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(Extension);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, ownerHwnd);

        var file = await picker.PickSingleFileAsync();
        if (file is null) return null;

        using var stream = await file.OpenStreamForReadAsync();
        var rootNode = await JsonSerializer.DeserializeAsync<FileSystemNode>(stream, JsonOptions);

        if (rootNode is not null)
        {
            rootNode.EstablishParentLinksRecursively();
            return (rootNode, file.Name);
        }

        return null;
    }

    public async Task<FileSystemNode?> ImportFromPathAsync(string filePath)
    {
        try
        {
            await using var stream = File.OpenRead(filePath);
            var rootNode = await JsonSerializer.DeserializeAsync<FileSystemNode>(stream, JsonOptions);
            rootNode?.EstablishParentLinksRecursively();
            return rootNode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ScanResultFileService] ImportFromPathAsync failed: {ex}");
            return null;
        }
    }
}