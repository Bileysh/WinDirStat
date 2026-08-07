using WinDirStat.Core.Entities;

namespace WinDirStat.Core.Classification;

public static class FileCategoryClassifier
{
    private static readonly Dictionary<string, FileCategory> ExtensionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [".doc"] = FileCategory.Documents,
        [".docx"] = FileCategory.Documents,
        [".pdf"] = FileCategory.Documents,
        [".txt"] = FileCategory.Documents,
        [".xls"] = FileCategory.Documents,
        [".xlsx"] = FileCategory.Documents,

        [".mp4"] = FileCategory.Videos,
        [".mkv"] = FileCategory.Videos,
        [".avi"] = FileCategory.Videos,
        [".mov"] = FileCategory.Videos,

        [".mp3"] = FileCategory.Audio,
        [".wav"] = FileCategory.Audio,
        [".flac"] = FileCategory.Audio,

        [".jpg"] = FileCategory.Images,
        [".png"] = FileCategory.Images,
        [".gif"] = FileCategory.Images,
        [".bmp"] = FileCategory.Images,

        [".zip"] = FileCategory.Archives,
        [".rar"] = FileCategory.Archives,
        [".7z"] = FileCategory.Archives,

        [".exe"] = FileCategory.Executables,
        [".msi"] = FileCategory.Executables,
        [".dll"] = FileCategory.Executables,
    };

    public static FileCategory Classify(string extension) =>
        ExtensionMap.GetValueOrDefault(extension, FileCategory.Other);
}