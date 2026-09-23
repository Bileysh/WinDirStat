using Windows.ApplicationModel.DataTransfer;
using Volumetric.Core.Interfaces;

namespace Volumetric_App.Services;

public class ClipboardService : IClipboardService
{
    public void CopyText(string text)
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(text);
        Clipboard.SetContent(dataPackage);
    }
}
