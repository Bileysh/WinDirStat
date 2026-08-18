using WinDirStat.Core.Classification;
using WinDirStat.Core.Entities;

namespace WinDirStat.ViewModels;

public class FileTypeStatisticsViewModel(FileTypeStatisticsEntry statisticsEntry)
{
    public string Label => statisticsEntry.Label;
    public string TotalSizeFormatted => SizeFormatter.Format(statisticsEntry.TotalSize);
    public string PercentFormatted => $"{statisticsEntry.PercentOfTotal:F1}%";
    public FileCategory Category => statisticsEntry.Category;

    public string IconGlyph => Category switch
    {
        FileCategory.Images => "\uEB9F",       
        FileCategory.Videos => "\uE714",       
        FileCategory.Audio => "\uE8D6",        
        FileCategory.Documents => "\uE8A5",    
        FileCategory.Archives => "\uE188",     
        FileCategory.Executables => "\uE7B8", 
        FileCategory.Development => "\uE943",  
        FileCategory.VirtualDisks => "\uEDA2", 
        FileCategory.System => "\uE770",       
        _ => "\uE7C3"                          
    };
}