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
    
    private readonly Stack<FileSystemNode> _treeMapHistory = new();

    private CancellationTokenSource? _scanCts;

    public MainPageViewModel(IDiskScanService diskScanService, IFolderPickerService folderPickerService,
        IScanStateService scanStateService, IWindowManagerService windowManagerService, IDialogService dialogService,
        ILocalizationService localizationService, IThemeService themeService)
    {
        _diskScanService = diskScanService;
        _folderPickerService = folderPickerService;
        _scanStateService = scanStateService;
        _windowManagerService = windowManagerService;
        _dialogService = dialogService;
        _localizationService = localizationService;
        _themeService = themeService;

        _scanStateService.StateChanged += OnStateChanged;

        if (_scanStateService.CurrentResult is not null)
            OnStateChanged(this, _scanStateService.CurrentResult);
    }

    [ObservableProperty] private ObservableCollection<NodeViewModel> _rootNodes = [];

    [ObservableProperty] private bool _isScanning;

    [ObservableProperty] private ObservableCollection<FileTypeStatisticsViewModel> _typeStatistics = [];

    [ObservableProperty] private ObservableCollection<TreeMapRectViewModel> _treeMapRects = [];

    [ObservableProperty] private bool _groupByCategory;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsTreeMapNavigated))]
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
        // TODO: (Elevated Support) Замінити FolderPicker на класичний IFileOpenDialog для підтримки режиму Адміністратора. 
        // Поточний пікер конфліктує з правами Windows і видає помилку при спробі відкрити вікно.
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
    private async Task ChangeLanguageAsync(string cultureCode)
    {
        if (_localizationService.CurrentLanguage == cultureCode) return;

        _localizationService.SetLanguage(cultureCode);

        var title = cultureCode == "uk-UA" ? "Зміна мови" : "Language Changed";
        var message = cultureCode == "uk-UA"
            ? "Мову успішно змінено. Будь ласка, перезапустіть програму, щоб зміни набули чинності."
            : "Language changed successfully. Please restart the application to apply the changes.";

        await _dialogService.ShowMessageAsync(title, message);
    }
    
    [RelayCommand]
    private void ToggleTheme()
    {
        _themeService.ToggleTheme();
    }
}