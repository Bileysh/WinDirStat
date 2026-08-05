using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    [ObservableProperty] private bool _isScanning;

    [RelayCommand]
    private async Task OpenFolderAsync()
    {
        var scan = _diskScanService.Scan(
            "C:\\Users\\Білеуш Антон"); // TODO: temporary manual scan trigger, replaced with FolderPicker in PR #3

        Debug.WriteLine(
            $"Scan completed. Root node: {scan.Name}, SizeLogical: {scan.SizeLogical}, SizePhysical: {scan.SizePhysical}, LastModified: {scan.LastModified}, Children count: {scan.Children.Count}, FullPath: {scan.FullPath}, IsDirectory: {scan.IsDirectory}, Extension: {scan.Extension}");
        var path = await _folderPickerService.PickFolderAsync();
        if (path is null) return;

        IsScanning = true;
        try
        {
            var rootNode = await Task.Run(() => _diskScanService.Scan(path));
            _scannedRoot = rootNode;
            RootNodes = [new NodeViewModel(rootNode)];
        }
        finally
        {
            IsScanning = false;
        }
    }
}