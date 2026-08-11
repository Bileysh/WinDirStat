using Windows.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using WinDirStat.Core.Entities;

namespace WinDirStat_App.Converters;

public class FileCategoryToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        new SolidColorBrush(ColorFor((FileCategory)value));

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();

    private static Color ColorFor(FileCategory c) => c switch
    {
        FileCategory.Documents => Colors.CornflowerBlue,
        FileCategory.Videos => Colors.OrangeRed,
        FileCategory.Audio => Colors.MediumPurple,
        FileCategory.Images => Colors.SeaGreen,
        FileCategory.Archives => Colors.Goldenrod,
        FileCategory.Executables => Colors.Crimson,
        FileCategory.Development => Colors.LightSeaGreen, 
        FileCategory.VirtualDisks => Colors.SlateGray,  
        FileCategory.System => Colors.DimGray,
        _ => Colors.Gray
    };
}