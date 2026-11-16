using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Volumetric.Core.Interfaces;
using Volumetric.Services;

namespace Volumetric_App.Services;

public class WinUiDialogService : IDialogService
{
    private readonly ICurrentXamlRootProvider _xamlRootProvider;
    private readonly IAppLogger _logger;

    public WinUiDialogService(ICurrentXamlRootProvider xamlRootProvider, IAppLogger logger)
    {
        _xamlRootProvider = xamlRootProvider;
        _logger = logger;
    }

    public async Task ShowMessageAsync(string title, string message, string closeButtonText = "OK")
    {
        var xamlRoot = _xamlRootProvider.XamlRoot;

        if (xamlRoot is null)
        {
            _logger.Warning("[WinUiDialogService] XamlRoot not ready for this window yet — dialog skipped.");
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
