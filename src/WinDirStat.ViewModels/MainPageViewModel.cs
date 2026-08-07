using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinDirStat.Core.Interfaces;

namespace WinDirStat.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private readonly IDiskScanService _diskScanService;

    public MainPageViewModel(IDiskScanService diskScanService)
    {
        _diskScanService = diskScanService;
    }


    [RelayCommand]
    private void ScanDisk()
    {
        var scan = _diskScanService.Scan(
            "C:\\Users\\Білеуш Антон"); // TODO: temporary manual scan trigger, replaced with FolderPicker in PR #3

        Debug.WriteLine(
            $"Scan completed. Root node: {scan.Name}, SizeLogical: {scan.SizeLogical}, SizePhysical: {scan.SizePhysical}, LastModified: {scan.LastModified}, Children count: {scan.Children.Count}, FullPath: {scan.FullPath}, IsDirectory: {scan.IsDirectory}, Extension: {scan.Extension}");
    }
}