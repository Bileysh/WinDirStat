using Microsoft.UI.Xaml.Controls;
using WinDirStat.ViewModels;

namespace WinDirStat_App;

public sealed partial class MainPage : Page
{
    public MainPageViewModel ViewModel { get; }

    public MainPage(MainPageViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Unloaded += (_, _) => ViewModel.Dispose();
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
}