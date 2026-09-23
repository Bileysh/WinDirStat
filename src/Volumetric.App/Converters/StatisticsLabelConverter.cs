using System;
using Microsoft.UI.Xaml.Data;
using Microsoft.Windows.ApplicationModel.Resources;
using Volumetric.Core.Entities;
using Volumetric.Core.Interfaces;
using Volumetric.ViewModels;

namespace Volumetric_App.Converters;

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
                    FileCategory.Documents => _resourceLoader.GetString(ResourceKeys.Category_Documents),
                    FileCategory.Videos => _resourceLoader.GetString(ResourceKeys.Category_Videos),
                    FileCategory.Audio => _resourceLoader.GetString(ResourceKeys.Category_Audio),
                    FileCategory.Images => _resourceLoader.GetString(ResourceKeys.Category_Images),
                    FileCategory.Archives => _resourceLoader.GetString(ResourceKeys.Category_Archives),
                    FileCategory.Executables => _resourceLoader.GetString(ResourceKeys.Category_Executables),
                    FileCategory.Development => _resourceLoader.GetString(ResourceKeys.Category_Development),
                    FileCategory.VirtualDisks => _resourceLoader.GetString(ResourceKeys.Category_VirtualDisks),
                    FileCategory.System => _resourceLoader.GetString(ResourceKeys.Category_System),
                    FileCategory.Folder => _resourceLoader.GetString(ResourceKeys.Category_Folder),
                    FileCategory.Other => _resourceLoader.GetString(ResourceKeys.Category_Other),
                    _ => vm.Category.ToString()
                };
            }

            return string.IsNullOrEmpty(vm.Label)
                ? (_resourceLoader ??= new ResourceLoader()).GetString(ResourceKeys.NoExtensionLabel)
                : vm.Label;
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
