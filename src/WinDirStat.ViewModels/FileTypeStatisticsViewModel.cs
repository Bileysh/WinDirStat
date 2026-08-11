using WinDirStat.Core.Classification;
using WinDirStat.Core.Entities;

namespace WinDirStat.ViewModels;

public class FileTypeStatisticsViewModel(FileTypeStatisticsEntry statisticsEntry)
{
    public string Label => statisticsEntry.Label;
    public string TotalSizeFormatted => SizeFormatter.Format(statisticsEntry.TotalSize);
    public string PercentFormatted => $"{statisticsEntry.PercentOfTotal:F1}%";
}