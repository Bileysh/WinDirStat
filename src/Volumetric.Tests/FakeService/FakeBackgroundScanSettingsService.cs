using Volumetric.Core.Interfaces;

namespace Volumetric.Tests.FakeService;

public class FakeBackgroundScanSettingsService : IBackgroundScanSettingsService
{
    public uint ScanIntervalMinutes { get; set; }
    public double LowFreeSpaceThresholdPercent { get; set; }
    public bool AccountForHardLinks { get; set; }
}
