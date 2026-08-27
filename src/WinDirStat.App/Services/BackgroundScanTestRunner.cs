using WinDirStat.Core.Interfaces;
using WinDirStat.WinRT;

namespace WinDirStat_App.Services;

public sealed class BackgroundScanTestRunner : IBackgroundScanTestRunner
{
    public void RunNow() => BackgroundScanTask.RunScanAndNotify();
}