using System;
using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Volumetric.Core.Classification;
using Volumetric.Core.Entities;
using Volumetric.Core.Interfaces;

namespace Volumetric.ViewModels;

public partial class MainPageViewModel : ObservableObject, IDisposable, IMainPageViewModel
{
    private readonly IDiskScanService _diskScanService;
    private readonly IFolderPickerService _folderPickerService;
    private readonly IScanStateService _scanStateService;
    private readonly IWindowManagerService _windowManagerService;
    private readonly IDialogService _dialogService;
    private readonly ILocalizationService _localizationService;
    private readonly IThemeService _themeService;
    private readonly INotificationService _notificationService;
    private readonly IDriveInfoService _driveInfoService;
    private readonly Stack<FileSystemNode> _treeMapHistory = new();
    private readonly IClipboardService _clipboardService;
    private readonly IFileExplorerService _fileExplorerService;
    private readonly IBackgroundScanSettingsService _backgroundScanSettingsService;
    private readonly IScanResultFileService _scanResultFileService;
    private readonly IWindowHandleProvider _windowHandleProvider;
    private readonly IAppLogger _appLogger;
    private readonly IRecentScansService _recentScansService;

    private CancellationTokenSource? _scanCts;
    private string? _lastScanPath;

    public MainPageViewModel(IDiskScanService diskScanService, IFolderPickerService folderPickerService,
        IScanStateService scanStateService, IWindowManagerService windowManagerService, IDialogService dialogService,
        ILocalizationService localizationService, IThemeService themeService, INotificationService notificationService,
        IDriveInfoService driveInfoService, IClipboardService clipboardService,
        IFileExplorerService fileExplorerService, IBackgroundScanSettingsService backgroundScanSettingsService,
        IScanResultFileService scanResultFileService, IWindowHandleProvider windowHandleProvider,
        IAppLogger appLogger, IRecentScansService recentScansService)
    {
        _diskScanService = diskScanService;
        _folderPickerService = folderPickerService;
        _scanStateService = scanStateService;
        _windowManagerService = windowManagerService;
        _dialogService = dialogService;
        _localizationService = localizationService;
        _themeService = themeService;
        _notificationService = notificationService;
        _driveInfoService = driveInfoService;
        _clipboardService = clipboardService;
        _fileExplorerService = fileExplorerService;
        _backgroundScanSettingsService = backgroundScanSettingsService;
        _scanResultFileService = scanResultFileService;
        _windowHandleProvider = windowHandleProvider;
        _appLogger = appLogger;
        _recentScansService = recentScansService;

        _scanStateService.StateChanged += OnStateChanged;

        LoadAvailableDrives();
        _ = LoadRecentScansAsync();

        if (_scanStateService.CurrentResult is not null)
        {
            OnStateChanged(this, _scanStateService.CurrentResult);
        }
    }

    [ObservableProperty]
    public partial ObservableCollection<NodeViewModel> RootNodes { get; set; } = [];

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    partial void OnSearchTextChanged(string value)
    {
        foreach (var root in RootNodes) root.ApplySearchFilter(value);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowDriveSelector))]
    [NotifyPropertyChangedFor(nameof(IsNoDataVisible))]
    public partial bool IsScanning { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowDriveSelector))]
    public partial bool HasScanResult { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScanProgressText))]
    public partial long ScanFilesCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScanProgressText))]
    public partial long ScanFoldersCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScanProgressText))]
    public partial string ScanCurrentPath { get; set; } = string.Empty;

    public string ScanProgressText => ScanFilesCount == 0 && ScanFoldersCount == 0
        ? string.Empty
        : string.Format(_localizationService.GetString(ResourceKeys.ScanProgressFormat), ScanFilesCount, ScanFoldersCount);

    [ObservableProperty]
    public partial ObservableCollection<DriveItemViewModel> AvailableDrives { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRecentScans))]
    public partial ObservableCollection<RecentScanItemViewModel> RecentScans { get; set; } = [];

    public bool HasRecentScans => RecentScans.Count > 0;

    public bool ShowDriveSelector => !IsScanning && !HasScanResult;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNoDataVisible))]
    public partial ObservableCollection<FileTypeStatisticsViewModel> TypeStatistics { get; set; } = [];

    public bool IsNoDataVisible => TypeStatistics.Count == 0 && !IsScanning;

    [ObservableProperty]
    public partial ObservableCollection<TreeMapRectViewModel> TreeMapRects { get; set; } = [];

    [ObservableProperty]
    public partial bool GroupByCategory { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTreeMapNavigated))]
    [NotifyPropertyChangedFor(nameof(TreeMapRelativePath))]
    [NotifyPropertyChangedFor(nameof(TreeMapAbsoluteRootPath))]
    public partial FileSystemNode? CurrentTreeMapRoot { get; set; }

    public bool IsTreeMapNavigated => _treeMapHistory.Count > 0;

    partial void OnGroupByCategoryChanged(bool value) => RefreshStatistics();

    partial void OnIsScanningChanged(bool value)
    {
        RescanCommand.NotifyCanExecuteChanged();
        RescanElevatedCommand.NotifyCanExecuteChanged();
    }

    partial void OnHasScanResultChanged(bool value)
    {
        ExportScanResultsCommand.NotifyCanExecuteChanged();
        OpenScanReportCommand.NotifyCanExecuteChanged();
    }

    private void OnStateChanged(object? sender, ScanResult? result)
    {
        if (result is null) return;

        RootNodes =
        [
            new NodeViewModel(result.RootNode, localizationService: _localizationService,
                notificationService: _notificationService, clipboardService: _clipboardService,
                fileExplorerService: _fileExplorerService, appLogger: _appLogger)
        ];
        HasScanResult = true;
        SearchText = string.Empty;

        _treeMapHistory.Clear();
        CurrentTreeMapRoot = result.RootNode;
        OnPropertyChanged(nameof(IsTreeMapNavigated));

        RefreshStatistics();
        RefreshTreeMap();
        AddToRecentScans(result.RootPath);
    }

    private const int MaxRecentScans = 5;

    private void AddToRecentScans(string path)
    {
        var existing = RecentScans.FirstOrDefault(r =>
            string.Equals(r.FullPath, path, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            RecentScans.Remove(existing);
        }

        RecentScans.Insert(0, new RecentScanItemViewModel(path));
        while (RecentScans.Count > MaxRecentScans)
        {
            RecentScans.RemoveAt(RecentScans.Count - 1);
        }
    }

    private void LoadAvailableDrives()
    {
        var drives = _driveInfoService.GetDrives().Select(d => new DriveItemViewModel(d));
        AvailableDrives = new ObservableCollection<DriveItemViewModel>(drives);
    }

    private async Task LoadRecentScansAsync()
    {
        try
        {
            var paths = await _recentScansService.GetRecentPathsAsync();
            RecentScans = new ObservableCollection<RecentScanItemViewModel>(
                paths.Select(path => new RecentScanItemViewModel(path)));
        }
        catch (Exception ex)
        {
            _appLogger.Warning(ex, "[MainPageViewModel] Failed to load recent scans");
        }
    }

    [RelayCommand]
    private void RefreshDrives() => LoadAvailableDrives();

    [RelayCommand]
    private async Task OpenRecentScanAsync(RecentScanItemViewModel? recentScan)
    {
        if (recentScan is null) return;

        await ScanPathAsync(recentScan.FullPath);
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

        await ScanPathAsync(path);
    }

    [RelayCommand]
    private async Task SelectDriveAsync(DriveItemViewModel? drive)
    {
        if (drive is null) return;

        await ScanPathAsync(drive.RootPath);
    }

    [RelayCommand(CanExecute = nameof(CanRescan))]
    private async Task RescanAsync()
    {
        if (_lastScanPath is not null)
        {
            await ScanPathAsync(_lastScanPath);
        }
    }

    [RelayCommand(CanExecute = nameof(CanRescan))]
    private async Task RescanElevatedAsync()
    {
        if (_lastScanPath is not null)
        {
            await ScanPathAsync(_lastScanPath, useElevatedFallbackForAccessDenied: true);
        }
    }

    private bool CanRescan() => !IsScanning && _lastScanPath is not null;

    public async Task ScanPathAsync(string? path, bool useElevatedFallbackForAccessDenied = false)
    {
        CancelScan();
        _scanCts = new CancellationTokenSource();
        _lastScanPath = path;
        RescanCommand.NotifyCanExecuteChanged();
        RescanElevatedCommand.NotifyCanExecuteChanged();

        IsScanning = true;
        RootNodes.Clear();
        TypeStatistics.Clear();
        TreeMapRects.Clear();
        ScanFilesCount = 0;
        ScanFoldersCount = 0;
        ScanCurrentPath = string.Empty;

        var progress = new Progress<ScanProgress>(p =>
        {
            ScanFilesCount = p.FilesScanned;
            ScanFoldersCount = p.FoldersScanned;
            ScanCurrentPath = p.CurrentPath;
        });

        try
        {
            var scanResult = await _diskScanService.ScanAsync(
                path, _scanCts.Token, useElevatedFallbackForAccessDenied, progress,
                _backgroundScanSettingsService.AccountForHardLinks);
            _scanStateService.SetResult(scanResult);

            var fileCount = 0;
            var folderCount = 0;
            CountNodes(scanResult.RootNode, ref fileCount, ref folderCount);

            var title = _localizationService.GetString(ResourceKeys.ScanCompleteTitle);
            var msg = string.Format(_localizationService.GetString(ResourceKeys.ScanCompleteMessageFormat),
                scanResult.ScanDuration.TotalSeconds, fileCount, folderCount);

            _notificationService.ShowNotification(title, msg, path);
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
        if (CurrentTreeMapRoot is null) return;
        if (_treeMapWidth <= 0) return;
        if (_treeMapHeight <= 0) return;

        var rects = SquarifiedTreeMapLayout.Compute(CurrentTreeMapRoot, 0, 0, _treeMapWidth, _treeMapHeight);
        var viewModels = rects.Select(r =>
            new TreeMapRectViewModel(r, _notificationService, _localizationService, _fileExplorerService, _appLogger));
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
        _windowManagerService.OpenStatisticsWindow(this);
    }

    [RelayCommand]
    private void OpenTreeViewWindow()
    {
        _windowManagerService.OpenTreeViewWindow(this);
    }

    [RelayCommand]
    private void OpenTreeMapWindow()
    {
        _windowManagerService.OpenTreeMapWindow(this);
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
    private void OpenSettings()
    {
        _windowManagerService.OpenSettingsWindow();
    }

    [RelayCommand]
    private void Exit()
    {
        _windowManagerService.ExitApplication();
    }

    [RelayCommand]
    private async Task ShowAboutAsync()
    {
        var title = _localizationService.GetString(ResourceKeys.AboutTitle);
        var message = _localizationService.GetString(ResourceKeys.AboutMessage);
        await _dialogService.ShowMessageAsync(title, message);
    }

    [RelayCommand(CanExecute = nameof(HasScanResult))]
    private async Task ExportScanResultsAsync()
    {
        var result = _scanStateService.CurrentResult;
        if (result is null) return;

        try
        {
            var suggestedName = Path.GetFileName(result.RootPath.TrimEnd(Path.DirectorySeparatorChar));
            var fileName = await _scanResultFileService.ExportAsync(
                result.RootNode, string.IsNullOrEmpty(suggestedName) ? "scan" : suggestedName,
                _windowHandleProvider.Hwnd);

            if (fileName is not null)
            {
                var title = _localizationService.GetString(ResourceKeys.ExportCompleteTitle);
                _notificationService.ShowNotification(title, fileName);
            }
        }
        catch (Exception ex)
        {
            var title = _localizationService.GetString(ResourceKeys.ExportErrorTitle);
            await _dialogService.ShowMessageAsync(title, ex.Message);
        }
    }

    [RelayCommand(CanExecute = nameof(HasScanResult))]
    private void OpenScanReport()
    {
        var result = _scanStateService.CurrentResult;
        if (result is not null)
        {
            _windowManagerService.OpenScanReportWindow(result.RootNode);
        }
    }

    [RelayCommand]
    private async Task ImportScanResultsAsync()
    {
        var imported = await _scanResultFileService.ImportAsync(_windowHandleProvider.Hwnd);
        if (imported is null) return;

        LoadImportedResult(imported.Value.RootNode);
    }

    public void LoadImportedResult(FileSystemNode rootNode)
    {
        var (statsByExtension, statsByCategory) = FileStatisticsAggregator.ComputeAll(rootNode);
        var result = new ScanResult(rootNode.RootFullPathOverride ?? rootNode.Name, rootNode,
            statsByCategory, statsByExtension, TimeSpan.Zero);
        _scanStateService.SetResult(result);

        _lastScanPath = null;
        RescanCommand.NotifyCanExecuteChanged();
        RescanElevatedCommand.NotifyCanExecuteChanged();
    }

    public async Task HandleDroppedFilesAsync(IReadOnlyList<string> filePaths)
    {
        var scanPath = filePaths.FirstOrDefault(p =>
            p.EndsWith(".volscan", StringComparison.OrdinalIgnoreCase) ||
            p.EndsWith(".wdsscan", StringComparison.OrdinalIgnoreCase));

        if (scanPath is null) return;

        var rootNode = await _scanResultFileService.ImportFromPathAsync(scanPath);
        if (rootNode is not null)
        {
            LoadImportedResult(rootNode);
        }
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
