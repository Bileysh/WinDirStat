using Microsoft.UI.Xaml.Data;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using WinDirStat.ViewModels;

namespace WinDirStat_App.Converters;

public class NodeSummaryConverter : IValueConverter
{
    private readonly ResourceLoader _resourceLoader = new();

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is NodeViewModel node && node.IsDirectory)
        {
            var filesText = _resourceLoader.GetString("FilesText");
            var foldersText = _resourceLoader.GetString("FoldersText");
            
            return $"{node.ChildFileCount} {filesText}, {node.ChildDirectoryCount} {foldersText}";
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}