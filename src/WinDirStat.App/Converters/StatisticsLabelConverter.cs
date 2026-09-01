using System;
using Microsoft.UI.Xaml.Data;
using Microsoft.Windows.ApplicationModel.Resources;
using WinDirStat.Core.Entities;
using WinDirStat.ViewModels;

namespace WinDirStat_App.Converters;

public class StatisticsLabelConverter : IValueConverter
{
    private ResourceLoader? _resourceLoader;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is FileTypeStatisticsViewModel vm)
        {
            if (vm.IsCategoryGroup)
            {
                _resourceLoader ??= new ResourceLoader();

                return vm.Category switch
                {
                    FileCategory.Documents => _resourceLoader.GetString("Category_Documents"),
                    FileCategory.Videos => _resourceLoader.GetString("Category_Videos"),
                    FileCategory.Audio => _resourceLoader.GetString("Category_Audio"),
                    FileCategory.Images => _resourceLoader.GetString("Category_Images"),
                    FileCategory.Archives => _resourceLoader.GetString("Category_Archives"),
                    FileCategory.Executables => _resourceLoader.GetString("Category_Executables"),
                    FileCategory.Development => _resourceLoader.GetString("Category_Development"),
                    FileCategory.VirtualDisks => _resourceLoader.GetString("Category_VirtualDisks"),
                    FileCategory.System => _resourceLoader.GetString("Category_System"),
                    FileCategory.Folder => _resourceLoader.GetString("Category_Folder"),
                    FileCategory.Other => _resourceLoader.GetString("Category_Other"),
                    _ => vm.Category.ToString()
                };
            }

            return string.IsNullOrEmpty(vm.Label)
                ? (_resourceLoader ??= new ResourceLoader()).GetString("NoExtensionLabel")
                : vm.Label;
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}