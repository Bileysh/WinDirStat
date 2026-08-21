using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using WinDirStat.Core.Classification;
using WinDirStat.Core.Entities;

namespace WinDirStat.ViewModels;

public partial class TreeMapRectViewModel
{
    public FileSystemNode Node { get; }
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
    public bool IsFolder { get; }
    public TreeMapRectViewModel(TreeMapRect rect)
    {
        Node = rect.Node;
        X = rect.X;
        Y = rect.Y;
        Width = rect.Width;
        Height = rect.Height;
        Name = rect.Node.Name;
        
        IsFolder = rect.Node.IsDirectory;
        Category = IsFolder 
            ? FileCategory.Folder 
            : FileCategoryClassifier.Classify(rect.Node.Extension);
        
        SizeFormatted = SizeFormatter.Format(rect.Node.SizeLogical);
        ToolTipText = $"{rect.Node.Name}\n{SizeFormatted}";

        IsTitleVisible = Width > TreeMapConstants.MinWidthForTitle && Height > TreeMapConstants.MinHeightForTitle;
        IsSizeVisible = !IsFolder && Width > TreeMapConstants.MinWidthForSize && Height > TreeMapConstants.MinHeightForSize;
    }
    
    [RelayCommand]
    private void OpenInExplorer()
    {
        if (string.IsNullOrEmpty(Node.FullPath)) return;

        try
        {
            if (IsFolder)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Node.FullPath,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{Node.FullPath}\"",
                    UseShellExecute = true
                });
            }
        }
        catch
        {
        }
    }
}