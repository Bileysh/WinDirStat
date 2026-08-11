using WinDirStat.Core.Classification;
using WinDirStat.Core.Entities;

namespace WinDirStat.ViewModels;

public class TreeMapRectViewModel(TreeMapRect rect)
{
    public double X { get; } = rect.X;
    public double Y { get; } = rect.Y;
    public double Width { get; } = rect.Width;
    public double Height { get; } = rect.Height;
    public string Name { get; } = rect.Node.Name;
    public FileCategory Category { get; } = FileCategoryClassifier.Classify(rect.Node.Extension);
}