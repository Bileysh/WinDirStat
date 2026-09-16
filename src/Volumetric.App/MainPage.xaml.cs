using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Serilog;
using Volumetric_App.Services;
using Volumetric.Core.Interfaces;
using Volumetric.ViewModels;

namespace Volumetric_App;

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

    private void Page_DragOver(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    { 
        e.AcceptedOperation = e.DataView.Contains(StandardDataFormats.StorageItems)
            ? DataPackageOperation.Copy
            : DataPackageOperation.None;
    }

    private async void Page_Drop(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            Log.Debug("Page_Drop: dropped data does not contain storage items, ignoring");
            return;
        }

        var deferral = e.GetDeferral();
        try
        {
            var items = await e.DataView.GetStorageItemsAsync();
            var paths = items.OfType<StorageFile>().Select(f => f.Path).ToList();
            Log.Information("Page_Drop: received {Count} file(s): [{Paths}]", paths.Count, string.Join(" | ", paths));

            await ViewModel.HandleDroppedFilesAsync(paths);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Page_Drop: handling dropped files failed");
        }
        finally
        {
            deferral.Complete();
        }
    }
}