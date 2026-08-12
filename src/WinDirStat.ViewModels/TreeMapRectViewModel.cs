using WinDirStat.Core.Classification;
using WinDirStat.Core.Entities;

namespace WinDirStat.ViewModels;

public class TreeMapRectViewModel
{
    public double X { get; }
    public double Y { get; }
    public double Width { get; }
    public double Height { get; }
    public string Name { get; }
    public FileCategory Category { get; }
    
    public string ToolTipText { get; }
    public bool IsTitleVisible { get; } 
    
    public string SizeFormatted { get; }
    public bool IsSizeVisible { get; }

    public TreeMapRectViewModel(TreeMapRect rect)
    {
        X = rect.X;
        Y = rect.Y;
        Width = rect.Width;
        Height = rect.Height;
        Name = rect.Node.Name;
        Category = FileCategoryClassifier.Classify(rect.Node.Extension);

        SizeFormatted = SizeFormatter.Format(rect.Node.SizeLogical);
        ToolTipText = $"{rect.Node.Name}\n{SizeFormatted}";

        IsTitleVisible = Width > 40 && Height > 20;
        IsSizeVisible = Width > 60 && Height > 40;
    }
}