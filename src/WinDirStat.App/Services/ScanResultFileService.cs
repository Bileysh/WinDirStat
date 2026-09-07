using System.Text.Json;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinDirStat.Core.Entities;
using WinDirStat.Core.Interfaces;

namespace WinDirStat_App.Services;

public sealed class ScanResultFileService : IScanResultFileService
{
    private const string Extension = ".wdsscan";

    public async Task<string?> ExportAsync(FileSystemNode rootNode, string suggestedFileName, IntPtr ownerHwnd)
    {
        var picker = new FileSavePicker();
        picker.FileTypeChoices.Add("WinDirStat scan", [Extension]);
        picker.SuggestedFileName = suggestedFileName;
        WinRT.Interop.InitializeWithWindow.Initialize(picker, ownerHwnd);

        var file = await picker.PickSaveFileAsync();
        if (file is null) return null;

        var json = JsonSerializer.Serialize(rootNode, FileSystemNodeJsonContext.Default.FileSystemNode);
        await FileIO.WriteTextAsync(file, json);
        return file.Name;
    }

    public async Task<(FileSystemNode RootNode, string FileName)?> ImportAsync(IntPtr ownerHwnd)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(Extension);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, ownerHwnd);

        var file = await picker.PickSingleFileAsync();
        if (file is null) return null;

        var json = await FileIO.ReadTextAsync(file);
        var rootNode = Deserialize(json);
        return rootNode is null ? null : (rootNode, file.Name);
    }

    public FileSystemNode? ImportFromPath(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            return Deserialize(json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ScanResultFileService] ImportFromPath('{filePath}') failed: {ex}");
            return null;
        }
    }

    private static FileSystemNode? Deserialize(string json)
    {
        var rootNode = JsonSerializer.Deserialize(json, FileSystemNodeJsonContext.Default.FileSystemNode);
        rootNode?.EstablishParentLinksRecursively();
        return rootNode;
    }
}