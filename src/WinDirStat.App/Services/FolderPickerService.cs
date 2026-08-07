using Windows.Storage.Pickers;
using WinDirStat.Core.Interfaces;
using WinRT.Interop;

namespace WinDirStat_App.Services;

public class FolderPickerService: IFolderPickerService
{
    public async Task<string?> PickFolderAsync()
    {
        var picker = new FolderPicker();
        var hwnd = WindowNative.GetWindowHandle(App.MainWindow); 
        InitializeWithWindow.Initialize(picker, hwnd);
        picker.FileTypeFilter.Add("*");

        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }
}