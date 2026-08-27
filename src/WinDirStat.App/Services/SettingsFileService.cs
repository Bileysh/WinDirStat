using Windows.Storage;
using Windows.Storage.Pickers;
using WinDirStat.Core.Interfaces;

namespace WinDirStat_App.Services;

public sealed class SettingsFileService : ISettingsFileService
{
    public async Task<string?> ExportAsync(string json, string suggestedFileName)
    {
        var picker = new FileSavePicker();
        picker.FileTypeChoices.Add("JSON", [".json"]);
        picker.SuggestedFileName = suggestedFileName;
        WinRT.Interop.InitializeWithWindow.Initialize(picker, GetWindowHandle());

        var file = await picker.PickSaveFileAsync();
        if (file is null) return null;

        await FileIO.WriteTextAsync(file, json);
        return file.Name;
    }

    public async Task<(string Json, string FileName)?> ImportAsync()
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".json");
        WinRT.Interop.InitializeWithWindow.Initialize(picker, GetWindowHandle());

        var file = await picker.PickSingleFileAsync();
        if (file is null) return null;

        var json = await FileIO.ReadTextAsync(file);
        return (json, file.Name);
    }

    private static IntPtr GetWindowHandle() =>
        WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
}