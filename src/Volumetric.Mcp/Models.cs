namespace Volumetric.Mcp;

public sealed record ScanStartedInfo(string ScanId, string RootPath, string State);

public sealed record ScanStatusInfo(
    string ScanId,
    string RootPath,
    string State,
    double ElapsedSeconds,
    long FilesScanned,
    long FoldersScanned,
    string? CurrentPath,
    string? Error);

public sealed record SizeEntry(string Path, bool IsDirectory, long SizeBytes, string Size);

public sealed record TypeStatsEntry(string Label, long SizeBytes, string Size, int FileCount, double PercentOfTotal);

public sealed record ScanResultsInfo(
    string ScanId,
    string RootPath,
    long TotalSizeBytes,
    string TotalSize,
    double ScanDurationSeconds,
    IReadOnlyList<TypeStatsEntry> ByCategory,
    IReadOnlyList<TypeStatsEntry> ByExtension,
    IReadOnlyList<SizeEntry> LargestRootChildren,
    IReadOnlyList<SizeEntry> LargestFiles);
