using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinDirStat.Core.Classification;
using WinDirStat.Core.Entities;
using WinDirStat.Core.Interfaces;

namespace WinDirStat.ViewModels;

public partial class MainPageViewModel : ObservableObject, IDisposable
{
    private readonly IDiskScanService _diskScanService;
    private readonly IFolderPickerService _folderPickerService;
    private readonly IScanStateService _scanStateService;
    private readonly IWindowManagerService _windowManagerService;
    private readonly IDialogService _dialogService;
    private readonly ILocalizationService _localizationService;
    private readonly IThemeService _themeService;
    private readonly INotificationService _notificationService;
    private readonly Stack<FileSystemNode> _treeMapHistory = new();

    private CancellationTokenSource? _scanCts;

    public MainPageViewModel(IDiskScanService diskScanService, IFolderPickerService folderPickerService,
        IScanStateService scanStateService, IWindowManagerService windowManagerService, IDialogService dialogService,
        ILocalizationService localizationService, IThemeService themeService, INotificationService notificationService)
    {
        _diskScanService = diskScanService;
        _folderPickerService = folderPickerService;
        _scanStateService = scanStateService;
        _windowManagerService = windowManagerService;
        _dialogService = dialogService;
        _localizationService = localizationService;
        _themeService = themeService;
        _notificationService = notificationService;

        _scanStateService.StateChanged += OnStateChanged;

        if (_scanStateService.CurrentResult is not null)
            OnStateChanged(this, _scanStateService.CurrentResult);
    }

    [ObservableProperty] private ObservableCollection<NodeViewModel> _rootNodes = [];

    [ObservableProperty] private bool _isScanning;

    [ObservableProperty] private ObservableCollection<FileTypeStatisticsViewModel> _typeStatistics = [];

    [ObservableProperty] private ObservableCollection<TreeMapRectViewModel> _treeMapRects = [];

    [ObservableProperty] private bool _groupByCategory;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTreeMapNavigated))]
    [NotifyPropertyChangedFor(nameof(TreeMapRelativePath))]
    [NotifyPropertyChangedFor(nameof(TreeMapAbsoluteRootPath))]
    private FileSystemNode? _currentTreeMapRoot;

    public bool IsTreeMapNavigated => _treeMapHistory.Count > 0;

    partial void OnGroupByCategoryChanged(bool value) => RefreshStatistics();

    private void OnStateChanged(object? sender, ScanResult? result)
    {
        if (result is null) return;

        RootNodes = [new NodeViewModel(result.RootNode)];

        _treeMapHistory.Clear();
        CurrentTreeMapRoot = result.RootNode;
        OnPropertyChanged(nameof(IsTreeMapNavigated));

        RefreshStatistics();
        RefreshTreeMap();
    }

    private void RefreshStatistics()
    {
        var currentResult = _scanStateService.CurrentResult;
        if (currentResult is null) return;

        var stats = GroupByCategory
            ? currentResult.StatisticsByCategory
            : currentResult.StatisticsByExtension;

        var viewModels = stats.Select(s => new FileTypeStatisticsViewModel(s, GroupByCategory));
        TypeStatistics = new ObservableCollection<FileTypeStatisticsViewModel>(viewModels.ToList());
    }

    [RelayCommand]
    private async Task OpenFolderAsync()
    {
        var path = await _folderPickerService.PickFolderAsync();
        if (path is null) return;

        CancelScan();
        _scanCts = new CancellationTokenSource();

        IsScanning = true;
        RootNodes.Clear();
        TypeStatistics.Clear();
        TreeMapRects.Clear();

        try
        {
            var scanResult = await _diskScanService.ScanAsync(path, _scanCts.Token);
            _scanStateService.SetResult(scanResult);

            var fileCount = 0;
            var folderCount = 0;
            CountNodes(scanResult.RootNode, ref fileCount, ref folderCount);

            var title = _localizationService.GetString("ScanCompleteTitle");
            var msg = string.Format(_localizationService.GetString("ScanCompleteMessageFormat"),
                scanResult.ScanDuration.TotalSeconds, fileCount, folderCount);

            _notificationService.ShowNotification(title, msg);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            IsScanning = false;
            _scanCts?.Dispose();
            _scanCts = null;
        }
    }

    private void CountNodes(FileSystemNode node, ref int fileCount, ref int folderCount)
    {
        if (node.IsDirectory) folderCount++;
        else fileCount++;

        foreach (var child in node.Children)
        {
            CountNodes(child, ref fileCount, ref folderCount);
        }
    }

    [RelayCommand]
    public void CancelScan()
    {
        if (_scanCts != null && !_scanCts.IsCancellationRequested)
        {
            _scanCts.Cancel();
        }
    }

    private double _treeMapWidth = 600;
    private double _treeMapHeight = 200;

    public void UpdateTreeMapSize(double width, double height)
    {
        _treeMapWidth = width;
        _treeMapHeight = height;
        RefreshTreeMap();
    }

    private void RefreshTreeMap()
    {
        if (CurrentTreeMapRoot is null || _treeMapWidth <= 0 || _treeMapHeight <= 0) return;

        var rects = SquarifiedTreeMapLayout.Compute(CurrentTreeMapRoot, 0, 0, _treeMapWidth, _treeMapHeight);
        var viewModels = rects.Select(r => new TreeMapRectViewModel(r));
        TreeMapRects = new ObservableCollection<TreeMapRectViewModel>(viewModels.ToList());
    }

    public void Dispose()
    {
        _scanStateService.StateChanged -= OnStateChanged;
        CancelScan();
        GC.SuppressFinalize(this);
    }

    [RelayCommand]
    private void OpenInNewWindow()
    {
        _windowManagerService.OpenMainWindow();
    }

    [RelayCommand]
    private void OpenStatisticsWindow()
    {
        _windowManagerService.OpenStatisticsWindow();
    }

    [RelayCommand]
    private void OpenTreeViewWindow()
    {
        _windowManagerService.OpenTreeViewWindow();
    }

    [RelayCommand]
    private void OpenTreeMapWindow()
    {
        _windowManagerService.OpenTreeMapWindow();
    }

    [RelayCommand]
    public void DrillDownTreeMap(TreeMapRectViewModel? clickedRect)
    {
        if (clickedRect?.Node != null && clickedRect.Node.Children.Any())
        {
            if (CurrentTreeMapRoot != null)
            {
                _treeMapHistory.Push(CurrentTreeMapRoot);
            }

            CurrentTreeMapRoot = clickedRect.Node;
            OnPropertyChanged(nameof(IsTreeMapNavigated));
            RefreshTreeMap();
        }
    }

    [RelayCommand]
    private void NavigateUpTreeMap()
    {
        if (_treeMapHistory.Count > 0)
        {
            CurrentTreeMapRoot = _treeMapHistory.Pop();
            OnPropertyChanged(nameof(IsTreeMapNavigated));
            RefreshTreeMap();
        }
    }

    [RelayCommand]
    private void ChangeLanguage(string cultureCode)
    {
        if (_localizationService.CurrentLanguage == cultureCode) return;

        _localizationService.SetLanguage(cultureCode);

        _windowManagerService.ReloadMainWindowContent();
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        _themeService.ToggleTheme();
    }

    [RelayCommand]
    private async Task ShowAboutAsync()
    {
        var title = _localizationService.GetString("AboutTitle");
        var message = _localizationService.GetString("AboutMessage");
        await _dialogService.ShowMessageAsync(title, message);
    }
    
    public string TreeMapAbsoluteRootPath => _scanStateService.CurrentResult?.RootPath ?? string.Empty;

    public string TreeMapRelativePath
    {
        get
        {
            var root = TreeMapAbsoluteRootPath;
            var current = CurrentTreeMapRoot?.FullPath ?? string.Empty;
            if (string.IsNullOrEmpty(root) || !current.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return string.Empty;
            return current[root.Length..];
            
        }
    }

}