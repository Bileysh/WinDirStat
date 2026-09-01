using Windows.Storage.Pickers;
using WinDirStat.Core.Interfaces;
using WinDirStat.Services;
using WinRT.Interop;

namespace WinDirStat_App.Services;

public class FolderPickerService : IFolderPickerService
{
    private readonly ILocalizationService _localizationService;
    private readonly IWindowHandleProvider _windowHandleProvider;

    public FolderPickerService(ILocalizationService localizationService, IWindowHandleProvider windowHandleProvider)
    {
        _localizationService = localizationService;
        _windowHandleProvider = windowHandleProvider;
    }

    public Task<string?> PickFolderAsync()
    { 
        var hwnd = _windowHandleProvider.Hwnd;

        if (ElevationHelper.IsElevated())
        {
            var title = _localizationService.GetString("FolderPickerTitle");
            return Task.FromResult(ClassicFolderPicker.Show(hwnd, title));
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