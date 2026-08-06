using WinDirStat.Core.Classification;
using WinDirStat.Core.Entities;

namespace WinDirStat.ViewModels;

public class FileTypeStatViewModel(FileTypeStat stat)
{
    public string Label => stat.Label;
    public string TotalSizeFormatted => SizeFormatter.Format(stat.TotalSize);
    public string PercentFormatted => $"{stat.PercentOfTotal:F1}%";
}