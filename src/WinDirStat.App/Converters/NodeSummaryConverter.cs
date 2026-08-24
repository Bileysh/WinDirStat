using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using WinDirStat.Core.Interfaces;
using WinDirStat.ViewModels;

namespace WinDirStat_App.Converters;

public class NodeSummaryConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is NodeViewModel node && node.IsDirectory)
        {
            var localizationService = ((App)Application.Current).Services.GetRequiredService<ILocalizationService>();
            var filesText = localizationService.GetString("FilesText");
            var foldersText = localizationService.GetString("FoldersText");

            return $"{node.ChildFileCount} {filesText}, {node.ChildDirectoryCount} {foldersText}";
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}