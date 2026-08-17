using WinDirStat.Core.Entities;

namespace WinDirStat.Core.Classification;

public static class FileCategoryClassifier
{
    private static readonly Dictionary<string, FileCategory> ExtensionMap = BuildMap();

    public static FileCategory Classify(string extension) =>
        string.IsNullOrWhiteSpace(extension)
            ? FileCategory.Other
            : ExtensionMap.GetValueOrDefault(extension, FileCategory.Other);

    private static Dictionary<string, FileCategory> BuildMap()
    {
        var map = new Dictionary<string, FileCategory>(StringComparer.OrdinalIgnoreCase);

        void Add(FileCategory category, params string[] extensions)
        {
            foreach (var ext in extensions) map[ext] = category;
        }

        Add(FileCategory.Development, ".cs", ".cpp", ".h", ".hpp", ".js", ".ts", ".json", ".xml", ".csproj", ".sln", ".ipch", ".pdb", ".obj", ".bin", ".vsix");
        Add(FileCategory.VirtualDisks, ".vhd", ".vhdx", ".vmdk", ".vdi", ".ova");
        Add(FileCategory.System, ".dll", ".sys", ".ini", ".log", ".tmp", ".cache", ".nvph", ".dat", ".db", ".sqlite", ".bak");        
        Add(FileCategory.Documents, ".doc", ".docx", ".pdf", ".txt", ".xls", ".xlsx", ".rtf", ".ppt", ".pptx", ".csv");
        Add(FileCategory.Videos, ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm");
        Add(FileCategory.Audio, ".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a");
        Add(FileCategory.Images, ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg", ".webp", ".tiff", ".ico");
        Add(FileCategory.Archives, ".zip", ".rar", ".7z", ".tar", ".gz", ".iso", ".nupkg");       
        Add(FileCategory.Executables, ".exe", ".msi", ".bat", ".cmd", ".sh", ".apk", ".app", ".jar");
        return map;
    }
}