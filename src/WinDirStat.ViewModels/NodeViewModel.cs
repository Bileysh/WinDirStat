using CommunityToolkit.Mvvm.ComponentModel;
using WinDirStat.Core.Classification;
using WinDirStat.Core.Entities;
using WinDirStat.Core.Interfaces;

namespace WinDirStat.ViewModels;

public partial class NodeViewModel : ObservableObject
{
    private readonly FileSystemNode _node;
    private readonly long _parentSizeLogical;
    private readonly ILocalizationService? _localizationService;
    private List<NodeViewModel>? _children;

    public NodeViewModel(FileSystemNode node, long parentSizeLogical = 0, ILocalizationService? localizationService = null)
    {
        _node = node;
        _parentSizeLogical = parentSizeLogical;
        _localizationService = localizationService;
    }

    public string Name => _node.Name;
    public bool IsDirectory => _node.IsDirectory;
    public DateTime LastModified => _node.LastModified.ToLocalTime();

    public string PercentOfParentFormatted => _parentSizeLogical > 0
        ? $"{(double)_node.SizeLogical / _parentSizeLogical * 100:F1}%"
        : "100%";

    public int ChildFileCount => _node.Children.Count(c => !c.IsDirectory);
    public int ChildDirectoryCount => _node.Children.Count(c => c.IsDirectory);

    public IReadOnlyList<NodeViewModel> Children =>
        _children ??= _node.Children
            .Select(c => new NodeViewModel(c, _node.SizeLogical, _localizationService))
            .ToList();

    public string ChildSummaryFormatted => IsDirectory
        ? $"{ChildFileCount} {_localizationService?.GetString("FilesText")}, {ChildDirectoryCount} {_localizationService?.GetString("FoldersText")}"
        : string.Empty;

    public string SizeLogicalFormatted => _node.Status switch
    {
        ScanStatus.ReparsePoint => "‹junction›",
        ScanStatus.Ok => SizeFormatter.Format(_node.SizeLogical),
        _ => "—"
    };

    public string SizePhysicalFormatted => _node.Status switch
    {
        ScanStatus.ReparsePoint => "‹junction›",
        ScanStatus.Ok => SizeFormatter.Format(_node.SizePhysical),
        _ => "—"
    };

    public bool IsDuplicateHardLink => _node.IsDuplicateHardLink;

    public string StatusGlyph => IsDuplicateHardLink
        ? "\uE71B"
        : _node.Status switch
        {
            ScanStatus.AccessDenied => "\uE72E",
            ScanStatus.Error => "\uE783",
            ScanStatus.ReparsePoint => "\uE71B",
            _ => string.Empty
        };

    public bool HasStatusIcon => IsDuplicateHardLink || _node.Status != ScanStatus.Ok;

    public string StatusTooltip => IsDuplicateHardLink
        ? _localizationService?.GetString("HardLinkTooltipText") ?? string.Empty
        : _node.Status switch
        {
            ScanStatus.AccessDenied => _node.ErrorMessage ?? _localizationService?.GetString("AccessDeniedText") ?? string.Empty,
            ScanStatus.Error => _node.ErrorMessage ?? _localizationService?.GetString("ScanErrorText") ?? string.Empty,
            ScanStatus.ReparsePoint => _localizationService?.GetString("ReparsePointText") ?? string.Empty,
            _ => string.Empty
        };
}