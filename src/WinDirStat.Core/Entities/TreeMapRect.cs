namespace WinDirStat.Core.Entities;

public record TreeMapRect(
    FileSystemNode Node,
    double X,
    double Y,
    double Width,
    double Height);