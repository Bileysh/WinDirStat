namespace WinDirStat.Core.Entities;

public sealed record ScanProgress(string CurrentPath, long FilesScanned, long FoldersScanned);