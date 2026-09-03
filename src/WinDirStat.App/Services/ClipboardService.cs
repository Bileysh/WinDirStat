using Windows.ApplicationModel.DataTransfer;
using WinDirStat.Core.Interfaces;

namespace WinDirStat_App.Services;

public class ClipboardService : IClipboardService
{
    public void CopyText(string text)
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(text);
        Clipboard.SetContent(dataPackage);
    }
}