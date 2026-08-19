using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using WinDirStat.Core.Interfaces;
using WinDirStat.Services;

namespace WinDirStat_App.Services;

public class WinUIDialogService : IDialogService
{
    public async Task ShowMessageAsync(string title, string message, string closeButtonText = "OK")
    {
        var window = App.MainWindow; 

        if (window?.Content?.XamlRoot == null) return;

        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = closeButtonText,
            XamlRoot = window.Content.XamlRoot
        };

        await dialog.ShowAsync();
    }
}