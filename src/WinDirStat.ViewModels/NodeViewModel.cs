using CommunityToolkit.Mvvm.ComponentModel;
using WinDirStat.Core.Classification;
using WinDirStat.Core.Entities;

namespace WinDirStat.ViewModels;

public partial class NodeViewModel: ObservableObject
{
    private readonly FileSystemNode _node;
    private readonly long _parentSizeLogical;
    private List<NodeViewModel>? _children;
    
    public NodeViewModel(FileSystemNode node, long parentSizeLogical = 0)
    {
        _node = node;
        _parentSizeLogical = parentSizeLogical;
    }
    
    public string Name => _node.Name;
    public bool IsDirectory => _node.IsDirectory;
    public DateTime LastModified => _node.LastModified.ToLocalTime();
    
    public string SizeLogicalFormatted => SizeFormatter.Format(_node.SizeLogical);
    public string SizePhysicalFormatted => SizeFormatter.Format(_node.SizePhysical);
    
    public string PercentOfParentFormatted  => _parentSizeLogical > 0 
        ? $"{(double)_node.SizeLogical / _parentSizeLogical * 100:F1}%"
        : "100%";
    
    public int ChildFileCount => _node.Children.Count(c => !c.IsDirectory);
    public int ChildDirectoryCount => _node.Children.Count(c => c.IsDirectory);

    public IReadOnlyList<NodeViewModel> Children =>
        _children ??= _node.Children
            .Select(c => new NodeViewModel(c, _node.SizeLogical))
            .ToList();
    
    public string ChildSummaryFormatted => IsDirectory
        ? $"{ChildFileCount} файлів, {ChildDirectoryCount} папок"
        : string.Empty;
}