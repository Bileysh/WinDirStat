using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using WinDirStat.Core.Interfaces;
using WinDirStat.Services;

namespace WinDirStat_App.Services;

public class WinUiDialogService : IDialogService
{
    private readonly ICurrentXamlRootProvider _xamlRootProvider;

    public WinUiDialogService(ICurrentXamlRootProvider xamlRootProvider)
    {
        _xamlRootProvider = xamlRootProvider;
    }

    public async Task ShowMessageAsync(string title, string message, string closeButtonText = "OK")
    {
        var xamlRoot = _xamlRootProvider.XamlRoot;

        if (xamlRoot is null)
        {
            System.Diagnostics.Debug.WriteLine(
                "[WinUiDialogService] XamlRoot ще не готовий для цього вікна — діалог пропущено.");
            return;
        }

        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = closeButtonText,
            XamlRoot = xamlRoot
        };

        await dialog.ShowAsync();
    }
}
