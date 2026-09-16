using Volumetric.Core.Interfaces;
using Volumetric.WinRT;

namespace Volumetric_App.Services;

public sealed class BackgroundScanTestRunner : IBackgroundScanTestRunner
{
    public void RunNow() => BackgroundScanTask.RunScanAndNotify();
}