using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Volumetric.Core.Interfaces;
using Volumetric.Services;

namespace Volumetric_App.Services;

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
                "[WinUiDialogService] XamlRoot not ready for this window yet — dialog skipped.");
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
