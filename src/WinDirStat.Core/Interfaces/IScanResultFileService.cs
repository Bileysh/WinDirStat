using WinDirStat.Core.Entities;

namespace WinDirStat.Core.Interfaces;

public interface IScanResultFileService
{
    Task<string?> ExportAsync(FileSystemNode rootNode, string suggestedFileName, IntPtr ownerHwnd);
    Task<(FileSystemNode RootNode, string FileName)?> ImportAsync(IntPtr ownerHwnd);
    FileSystemNode? ImportFromPath(string filePath);
}