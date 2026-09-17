using Volumetric.Core.Entities;

namespace Volumetric.Core.Interfaces;

public interface IScanResultFileService
{
    Task<string?> ExportAsync(FileSystemNode rootNode, string suggestedFileName, IntPtr ownerHwnd);
    Task<(FileSystemNode RootNode, string FileName)?> ImportAsync(IntPtr ownerHwnd);
    Task<FileSystemNode?> ImportFromPathAsync(string filePath);}