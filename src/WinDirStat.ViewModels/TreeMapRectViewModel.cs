using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using WinDirStat.Core.Classification;
using WinDirStat.Core.Entities;
using WinDirStat.Core.Interfaces;

namespace WinDirStat.ViewModels;

public partial class TreeMapRectViewModel
{
    private readonly INotificationService? _notificationService;
    private readonly ILocalizationService? _localizationService;
    private readonly IFileExplorerService? _fileExplorerService;

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

    public TreeMapRectViewModel(TreeMapRect rect, INotificationService? notificationService = null,
        ILocalizationService? localizationService = null, IFileExplorerService? fileExplorerService = null)
    {
        _notificationService = notificationService;
        _localizationService = localizationService;
        _fileExplorerService = fileExplorerService;
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
        IsSizeVisible = !IsFolder && Width > TreeMapConstants.MinWidthForSize &&
                        Height > TreeMapConstants.MinHeightForSize;
    }
    
    [RelayCommand]
    private void OpenInExplorer()
    {
        if (string.IsNullOrEmpty(Node.FullPath)) return;

        try
        {
            _fileExplorerService?.OpenInExplorer(Node.FullPath, IsFolder);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TreeMapRectViewModel] OpenInExplorer failed for '{Node.FullPath}': {ex}");
            _notificationService?.ShowNotification(
                GetLocalizedOrFallback("OpenInExplorerFailedTitle", "Failed to open Explorer"),
                $"'{Node.Name}': {ex.Message}");
        }
    }

    [RelayCommand]
    private void ShowProperties()
    {
        if (string.IsNullOrEmpty(Node.FullPath)) return;

        try
        {
            _fileExplorerService?.ShowProperties(Node.FullPath);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TreeMapRectViewModel] ShowProperties failed for '{Node.FullPath}': {ex}");
            _notificationService?.ShowNotification(
                GetLocalizedOrFallback("ShowPropertiesFailedTitle", "Failed to open properties"),
                $"'{Node.Name}': {ex.Message}");
        }
    }

    private string GetLocalizedOrFallback(string key, string fallback) =>
        _localizationService?.GetString(key) ?? fallback;
}