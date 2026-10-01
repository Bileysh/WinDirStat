using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Volumetric.Core.Entities;
using Volumetric.Core.Interfaces;

namespace Volumetric_App.Services;

public sealed class ScanResultFileService : IScanResultFileService
{
    private const string Extension = ".volscan";

    private readonly IAppLogger _logger;
    private readonly ScanResultSerializer _serializer;

    public ScanResultFileService(IAppLogger logger, ScanResultSerializer serializer)
    {
        _logger = logger;
        _serializer = serializer;
    }

    public async Task<string?> ExportAsync(FileSystemNode rootNode, string suggestedFileName, IntPtr ownerHwnd)
    {
        var picker = new FileSavePicker();
        picker.FileTypeChoices.Add("Volumetric scan", [Extension]);
        picker.SuggestedFileName = suggestedFileName;
        WinRT.Interop.InitializeWithWindow.Initialize(picker, ownerHwnd);

        var file = await picker.PickSaveFileAsync();
        if (file is null)
        {
            return null;
        }

        using var stream = await file.OpenStreamForWriteAsync();
        await _serializer.WriteAsync(stream, rootNode);

        return file.Name;
    }

    public async Task<(FileSystemNode RootNode, string FileName)?> ImportAsync(IntPtr ownerHwnd)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(Extension);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, ownerHwnd);

        var file = await picker.PickSingleFileAsync();
        if (file is null)
        {
            return null;
        }

        using var stream = await file.OpenStreamForReadAsync();
        var rootNode = await _serializer.ReadAsync(stream);

        return rootNode is null ? null : (rootNode, file.Name);
    }

    public async Task<FileSystemNode?> ImportFromPathAsync(string filePath)
    {
        try
        {
            await using var stream = File.OpenRead(filePath);
            return await _serializer.ReadAsync(stream);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "[ScanResultFileService] ImportFromPathAsync failed for '{FilePath}'", filePath);
            return null;
        }
    }
}
