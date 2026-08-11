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
    
    private void RefreshStatistics()
    {
        if (_scannedRoot is null) return;
        TypeStatistics = new ObservableCollection<FileTypeStatisticsViewModel>(
            (GroupByCategory ? FileStatisticsAggregator.ByCategory(_scannedRoot)
                : FileStatisticsAggregator.ByExtension(_scannedRoot))
            .Select(s => new FileTypeStatisticsViewModel(s)));}
    
    [RelayCommand]
    private async Task OpenFolderAsync()
    {
        var path = await _folderPickerService.PickFolderAsync();
        if (path is null) return;

        IsScanning = true;
        try
        {
            var rootNode = await Task.Run(() => _diskScanService.Scan(path));
            _scannedRoot = rootNode;
            RootNodes = [new NodeViewModel(rootNode)];
            RefreshStatistics();
        }
        finally
        {
            IsScanning = false;
        }
    }
}