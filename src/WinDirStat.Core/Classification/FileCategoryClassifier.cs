using WinDirStat.Core.Entities;

namespace WinDirStat.Core.Classification;

public static class FileCategoryClassifier
{
    public static FileCategory Classify(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return FileCategory.Other;

        return extension.ToLowerInvariant() switch
        {
            ".cs" or ".cpp" or ".h" or ".hpp" or ".js"
                or ".ts" or ".json" or ".xml" or
                ".csproj" or ".sln" or ".ipch" or ".pdb"
                or ".obj" or ".bin" or ".vsix" => FileCategory.Development,

            ".vhd" or ".vhdx" or ".vmdk"
                or ".vdi" or ".ova" => FileCategory.VirtualDisks,

            ".dll" or ".sys" or ".ini" or
                ".log" or ".tmp" or ".cache" or ".nvph" => FileCategory.System,

            ".doc" or ".docx" or ".pdf" or
                ".txt" or ".xls" or ".xlsx" or
                ".rtf" or ".ppt" or ".pptx" or ".csv" => FileCategory.Documents,

            ".mp4" or ".mkv" or ".avi" or
                ".mov" or ".wmv" or ".flv" or ".webm" => FileCategory.Videos,

            ".mp3" or ".wav" or ".flac" or
                ".aac" or ".ogg" or ".m4a" => FileCategory.Audio,

            ".jpg" or ".jpeg" or ".png" or
                ".gif" or ".bmp" or ".svg" or
                ".webp" or ".tiff" or ".ico" => FileCategory.Images,

            ".zip" or ".rar" or ".7z" or
                ".tar" or ".gz" or ".iso" => FileCategory.Archives,

            ".exe" or ".msi" or ".bat" or
                ".cmd" or ".sh" or ".apk" or ".app" => FileCategory.Executables,

            _ => FileCategory.Other
        };
    }
}