using Windows.Storage.Pickers;
using WinDirStat.Core.Interfaces;
using WinDirStat.Services;
using WinRT.Interop;

namespace WinDirStat_App.Services;

public class FolderPickerService : IFolderPickerService
{
    public Task<string?> PickFolderAsync()
    {
        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);

        if (ElevationHelper.IsElevated())
        {
            return Task.FromResult(ClassicFolderPicker.Show(hwnd, "Оберіть папку для сканування"));
        }

        return PickFolderNormalAsync(hwnd);
    }

    private static async Task<string?> PickFolderNormalAsync(IntPtr hwnd)
    {
        var picker = new FolderPicker();
        InitializeWithWindow.Initialize(picker, hwnd);
        picker.FileTypeFilter.Add("*");

        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }
}