using System.Diagnostics;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinDirStat.Core.Classification;
using WinDirStat.Core.Entities;
using WinDirStat.Core.Interfaces;

namespace WinDirStat.ViewModels;

public partial class NodeViewModel : ObservableObject
{
    private readonly FileSystemNode _node;
    private readonly long _parentSizeLogical;
    private readonly ILocalizationService? _localizationService;
    private readonly INotificationService? _notificationService;
    private readonly IClipboardService? _clipboardService;
    private List<NodeViewModel>? _children;

    public NodeViewModel(FileSystemNode node, long parentSizeLogical = 0,
        ILocalizationService? localizationService = null, INotificationService? notificationService = null, IClipboardService? clipboardService = null)
    {
        _node = node;
        _parentSizeLogical = parentSizeLogical;
        _localizationService = localizationService;
        _notificationService = notificationService;
        _clipboardService = clipboardService;
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
            .Select(c => new NodeViewModel(c, _node.SizeLogical, _localizationService, _notificationService, _clipboardService ))
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

    [RelayCommand]
    private void OpenInExplorer()
    {
        if (string.IsNullOrEmpty(_node.FullPath)) return;

        try
        {
            if (IsDirectory)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _node.FullPath,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{_node.FullPath}\"",
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NodeViewModel] OpenInExplorer failed for '{_node.FullPath}': {ex}");
            _notificationService?.ShowNotification(
                GetLocalizedOrFallback("OpenInExplorerFailedTitle", "Failed to open Explorer"),
                $"'{Name}': {ex.Message}");
        }
    }

    [RelayCommand]
    private void ShowProperties()
    {
        if (string.IsNullOrEmpty(_node.FullPath)) return;

        try
        {
            var succeeded = ShellInterop.SHObjectProperties(
                IntPtr.Zero, ShellInterop.SHOP_FILEPATH, _node.FullPath, null);

            if (!succeeded)
            {
                var error = Marshal.GetLastWin32Error();
                Debug.WriteLine(
                    $"[NodeViewModel] SHObjectProperties returned false for '{_node.FullPath}' " +
                    $"(Win32 error {error}).");
                _notificationService?.ShowNotification(
                    GetLocalizedOrFallback("ShowPropertiesFailedTitle", "Failed to open properties"), Name);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NodeViewModel] ShowProperties failed for '{_node.FullPath}': {ex}");
            _notificationService?.ShowNotification(
                GetLocalizedOrFallback("ShowPropertiesFailedTitle", "Failed to open properties"),
                $"'{Name}': {ex.Message}");
        }
    }

    private string GetLocalizedOrFallback(string key, string fallback) =>
        _localizationService?.GetString(key) ?? fallback;

    private static class ShellInterop
    {
        public const uint SHOP_FILEPATH = 0x2;

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SHObjectProperties(
            IntPtr hwnd, uint shopObjectType, string pszObjectName, string? pszPropertyPage);
    }
    
    [RelayCommand]
    private void CopyPath()
    {
        if (!string.IsNullOrEmpty(_node.FullPath))
            _clipboardService?.CopyText(_node.FullPath);
    }

    [RelayCommand]
    private void CopyName()
    {
        if (!string.IsNullOrEmpty(Name))
            _clipboardService?.CopyText(Name);
    }
}