using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinDirStat.Core.Classification;
using WinDirStat.Core.Entities;
using WinDirStat.Core.Interfaces;

namespace WinDirStat.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private readonly IDiskScanService _diskScanService;
    private readonly IFolderPickerService _folderPickerService;
    private FileSystemNode? _scannedRoot;

    public MainPageViewModel(IDiskScanService diskScanService, IFolderPickerService folderPickerService)
    {
        _diskScanService = diskScanService;
        _folderPickerService = folderPickerService;
    }
    
    [ObservableProperty] 
    private ObservableCollection<NodeViewModel> _rootNodes = [];

    [ObservableProperty] 
    private bool _isScanning;

    [ObservableProperty]
    private ObservableCollection<FileTypeStatisticsViewModel> _typeStatistics = [];

    [ObservableProperty]
    private bool _groupByCategory;

    partial void OnGroupByCategoryChanged(bool value) => RefreshStatistics();
    
    [ObservableProperty] 
    private ObservableCollection<TreeMapRectViewModel> _treeMapRects = [];

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
        if (_scannedRoot is null || _treeMapWidth <= 0 || _treeMapHeight <= 0) return;
    
        var rects = SquarifiedTreeMapLayout.Compute(_scannedRoot, 0, 0, _treeMapWidth, _treeMapHeight);
        var viewModels = rects.Select(r => new TreeMapRectViewModel(r));
        var viewModelsList = viewModels.ToList();
        TreeMapRects = new ObservableCollection<TreeMapRectViewModel>(viewModelsList);
    }
    
    private void RefreshStatistics()
    {
        if (_scannedRoot is null) return;

        var stats = GroupByCategory
            ? FileStatisticsAggregator.ByCategory(_scannedRoot)
            : FileStatisticsAggregator.ByExtension(_scannedRoot);

        var viewModels = stats.Select(s => new FileTypeStatisticsViewModel(s));
        var viewModelsList = viewModels.ToList();
        TypeStatistics = new ObservableCollection<FileTypeStatisticsViewModel>(viewModelsList);
    }
    
    [RelayCommand]
    private async Task OpenFolderAsync()
    {
        // TODO: (Elevated Support) Замінити FolderPicker на класичний IFileOpenDialog для підтримки режиму Адміністратора. 
        // Поточний пікер конфліктує з правами Windows і видає помилку при спробі відкрити вікно.
        var path = await _folderPickerService.PickFolderAsync();
        if (path is null) return;

        IsScanning = true;
        try
        {
            var rootNode = await Task.Run(() => _diskScanService.Scan(path));
            _scannedRoot = rootNode;
            RootNodes = [new NodeViewModel(rootNode)];
            RefreshStatistics();
            RefreshTreeMap();
        }
        finally
        {
            IsScanning = false;
        }
    }
    
}