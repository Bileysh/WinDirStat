using Microsoft.UI.Xaml.Controls;
using WinDirStat.ViewModels;
using WinDirStat_App.Services;

namespace WinDirStat_App;

public sealed partial class MainPage : Page
{
    public MainPageViewModel ViewModel { get; }

    public MainPage(MainPageViewModel viewModel, ICurrentXamlRootProvider xamlRootProvider)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Unloaded += (_, _) => ViewModel.Dispose();
        Loaded += (_, _) => xamlRootProvider.XamlRoot = XamlRoot;
    }

    private void TreeMapContainer_SizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
    {
        if (e.NewSize.Width > 0 && e.NewSize.Height > 0)
        {
            ViewModel.UpdateTreeMapSize(e.NewSize.Width, e.NewSize.Height);
        }
    }

    public Microsoft.UI.Xaml.Visibility GetNoDataVisibility(int count, bool isScanning)
    {
        return (count == 0 && !isScanning)
            ? Microsoft.UI.Xaml.Visibility.Visible
            : Microsoft.UI.Xaml.Visibility.Collapsed;
    }

    private void OpenFolderAccelerator_Invoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender,
        Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        if (ViewModel.OpenFolderCommand.CanExecute(null))
        {
            ViewModel.OpenFolderCommand.Execute(null);
        }
    }

    private void RescanAccelerator_Invoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender,
        Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        if (ViewModel.RescanCommand.CanExecute(null))
        {
            ViewModel.RescanCommand.Execute(null);
        }
    }

    private void CancelScanAccelerator_Invoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender,
        Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        if (ViewModel.CancelScanCommand.CanExecute(null))
        {
            ViewModel.CancelScanCommand.Execute(null);
        }
    }
}