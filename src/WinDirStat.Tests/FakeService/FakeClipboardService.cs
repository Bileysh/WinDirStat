using WinDirStat.Core.Interfaces;

namespace WinDirStat.Tests.FakeService;

public class FakeClipboardService : IClipboardService
{
    public void CopyText(string text)
    {
    }
}