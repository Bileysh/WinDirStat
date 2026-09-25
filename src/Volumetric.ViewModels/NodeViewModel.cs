using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Volumetric.Core.Classification;
using Volumetric.Core.Entities;
using Volumetric.Core.Interfaces;

namespace Volumetric.ViewModels;

public partial class NodeViewModel : ObservableObject
{
    private readonly FileSystemNode _node;
    private readonly long _parentSizeLogical;
    private readonly ILocalizationService? _localizationService;
    private readonly INotificationService? _notificationService;
    private readonly IClipboardService? _clipboardService;
    private readonly IFileExplorerService? _fileExplorerService;
    private readonly IAppLogger? _appLogger;
    private List<NodeViewModel>? _children;
    private SearchFilter? _searchFilter;

    public NodeViewModel(FileSystemNode node, long parentSizeLogical = 0,
        ILocalizationService? localizationService = null, INotificationService? notificationService = null,
        IClipboardService? clipboardService = null, IFileExplorerService? fileExplorerService = null,
        IAppLogger? appLogger = null)
    {
        _node = node;
        _parentSizeLogical = parentSizeLogical;
        _localizationService = localizationService;
        _notificationService = notificationService;
        _clipboardService = clipboardService;
        _fileExplorerService = fileExplorerService;
        _appLogger = appLogger;
    }

    public string Name => _node.Name;
    public bool IsDirectory => _node.IsDirectory;
    public DateTime LastModified => _node.LastModified.ToLocalTime();

    public string PercentOfParentFormatted => _parentSizeLogical > 0
        ? $"{(double)_node.SizeLogical / _parentSizeLogical * 100:F1}%"
        : "100%";

    public int ChildFileCount => _node.Children.Count(c => !c.IsDirectory);
    public int ChildDirectoryCount => _node.Children.Count(c => c.IsDirectory);

    public IReadOnlyList<NodeViewModel> Children => _children ??= CreateChildren();

    internal bool AreChildrenMaterialized => _children is not null;

    [ObservableProperty] public partial bool IsSearchMatch { get; set; } = true;

    public bool ApplySearchFilter(string? query)
    {
        ApplySearchFilter(string.IsNullOrWhiteSpace(query) ? null : new SearchFilter(_node, query));
        return IsSearchMatch;
    }

    private void ApplySearchFilter(SearchFilter? filter)
    {
        _searchFilter = filter;
        IsSearchMatch = filter?.IsVisible(_node) ?? true;

        if (_children is null)
        {
            return;
        }

        foreach (var child in _children)
        {
            child.ApplySearchFilter(filter);
        }
    }

    private List<NodeViewModel> CreateChildren() =>
        _node.Children
            .Select(c => new NodeViewModel(c, _node.SizeLogical, _localizationService, _notificationService,
                _clipboardService, _fileExplorerService, _appLogger)
            {
                _searchFilter = _searchFilter,
                IsSearchMatch = _searchFilter?.IsVisible(c) ?? true
            })
            .ToList();

    public string ChildSummaryFormatted => IsDirectory
        ? $"{ChildFileCount} {_localizationService?.GetString(ResourceKeys.FilesText)}, {ChildDirectoryCount} {_localizationService?.GetString(ResourceKeys.FoldersText)}"
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

    public string AccessibleName
    {
        get
        {
            var parts = new List<string> { Name };

            if (HasStatusIcon && !string.IsNullOrEmpty(StatusTooltip))
            {
                parts.Add(StatusTooltip);
            }

            parts.Add(SizeLogicalFormatted);
            parts.Add(PercentOfParentFormatted);

            if (IsDirectory && !string.IsNullOrEmpty(ChildSummaryFormatted))
            {
                parts.Add(ChildSummaryFormatted);
            }

            return string.Join(", ", parts);
        }
    }

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
        ? _localizationService?.GetString(ResourceKeys.HardLinkTooltipText) ?? string.Empty
        : _node.Status switch
        {
            ScanStatus.AccessDenied => _node.ErrorMessage ??
                                       _localizationService?.GetString(ResourceKeys.AccessDeniedText) ?? string.Empty,
            ScanStatus.Error => _node.ErrorMessage ?? _localizationService?.GetString(ResourceKeys.ScanErrorText) ?? string.Empty,
            ScanStatus.ReparsePoint => _localizationService?.GetString(ResourceKeys.ReparsePointText) ?? string.Empty,
            _ => string.Empty
        };

    [RelayCommand]
    private void OpenInExplorer()
    {
        if (string.IsNullOrEmpty(_node.FullPath))
        {
            return;
        }

        try
        {
            _fileExplorerService?.OpenInExplorer(_node.FullPath, IsDirectory);
        }
        catch (Exception ex)
        {
            _appLogger?.Warning(ex, "[NodeViewModel] OpenInExplorer failed for '{FullPath}'", _node.FullPath);
            _notificationService?.ShowNotification(
                GetLocalizedOrFallback(ResourceKeys.OpenInExplorerFailedTitle, "Failed to open Explorer"),
                $"'{Name}': {ex.Message}");
        }
    }

    [RelayCommand]
    private void ShowProperties()
    {
        if (string.IsNullOrEmpty(_node.FullPath))
        {
            return;
        }

        try
        {
            _fileExplorerService?.ShowProperties(_node.FullPath);
        }
        catch (Exception ex)
        {
            _appLogger?.Warning(ex, "[NodeViewModel] ShowProperties failed for '{FullPath}'", _node.FullPath);
            _notificationService?.ShowNotification(
                GetLocalizedOrFallback(ResourceKeys.ShowPropertiesFailedTitle, "Failed to open properties"),
                $"'{Name}': {ex.Message}");
        }
    }

    private string GetLocalizedOrFallback(string key, string fallback) =>
        _localizationService?.GetString(key) ?? fallback;

    [RelayCommand]
    private void CopyPath()
    {
        if (!string.IsNullOrEmpty(_node.FullPath))
        {
            _clipboardService?.CopyText(_node.FullPath);
        }
    }

    [RelayCommand]
    private void CopyName()
    {
        if (!string.IsNullOrEmpty(Name))
        {
            _clipboardService?.CopyText(Name);
        }
    }
}
