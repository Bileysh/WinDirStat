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

    public MainPageViewModel(IDiskScanService diskScanService, IFolderPickerService folderPickerService,
        IScanStateService scanStateService)
    {
        _diskScanService = diskScanService;
        _folderPickerService = folderPickerService;
        _scanStateService = scanStateService;

        _scanStateService.StateChanged += OnStateChanged;
    }

    [ObservableProperty] 
    private ObservableCollection<NodeViewModel> _rootNodes = [];

    [ObservableProperty] 
    private bool _isScanning;

    [ObservableProperty]
    private ObservableCollection<FileTypeStatisticsViewModel> _typeStatistics = [];
    
    [ObservableProperty] 
    private ObservableCollection<TreeMapRectViewModel> _treeMapRects = [];

    [ObservableProperty] 
    private bool _groupByCategory;

    partial void OnGroupByCategoryChanged(bool value) => RefreshStatistics();

    private void OnStateChanged(object? sender, ScanResult? result)
    {
        if (result is null) return;

        RootNodes = [new NodeViewModel(result.RootNode)];
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

        var viewModels = stats.Select(s => new FileTypeStatisticsViewModel(s));
        TypeStatistics = new ObservableCollection<FileTypeStatisticsViewModel>(viewModels.ToList());
    }

    [RelayCommand]
    private async Task OpenFolderAsync()
    {
        // TODO: (Elevated Support) Замінити FolderPicker на класичний IFileOpenDialog для підтримки режиму Адміністратора. 
        // Поточний пікер конфліктує з правами Windows і видає помилку при спробі відкрити вікно.
        var path = await _folderPickerService.PickFolderAsync();
        if (path is null) return;

        IsScanning = true;
        RootNodes.Clear();
        TypeStatistics.Clear();
        TreeMapRects.Clear();
        
        try
        {
            var scanResult = await Task.Run(() => _diskScanService.Scan(path));
            _scanStateService.SetResult(scanResult);
        }
        finally
        {
            IsScanning = false;
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
        var currentResult = _scanStateService.CurrentResult;
        if (currentResult is null || _treeMapWidth <= 0 || _treeMapHeight <= 0) return;

        var rects = SquarifiedTreeMapLayout.Compute(currentResult.RootNode, 0, 0, _treeMapWidth, _treeMapHeight);
        var viewModels = rects.Select(r => new TreeMapRectViewModel(r));
        TreeMapRects = new ObservableCollection<TreeMapRectViewModel>(viewModels.ToList());
    }
    
    public void Dispose()
    {
        _scanStateService.StateChanged -= OnStateChanged;
        GC.SuppressFinalize(this);
    }
}