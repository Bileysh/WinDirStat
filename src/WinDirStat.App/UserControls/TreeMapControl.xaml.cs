using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WinDirStat.ViewModels;

namespace WinDirStat_App.UserControls;

public sealed partial class TreeMapControl : UserControl
{
    public MainPageViewModel ViewModel
    {
        get => (MainPageViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(MainPageViewModel), typeof(TreeMapControl),
            new PropertyMetadata(null, OnViewModelChanged));

    public TreeMapControl() => InitializeComponent();

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TreeMapControl control && e.NewValue is MainPageViewModel vm)
        {
            if (control.MapContainer.ActualWidth > 0 && control.MapContainer.ActualHeight > 0)
            {
                vm.UpdateTreeMapSize(control.MapContainer.ActualWidth, control.MapContainer.ActualHeight);
            }
        }
    }

    private void MapContainer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width > 0 && e.NewSize.Height > 0)
        {
            ViewModel.UpdateTreeMapSize(e.NewSize.Width, e.NewSize.Height);
        }
    }

    private Visibility GetNoDataVisibility(int count, bool isScanning) =>
        count == 0 && !isScanning ? Visibility.Visible : Visibility.Collapsed;

    private void OnRectTapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is TreeMapRectViewModel rectVm)
        {
            ViewModel?.DrillDownTreeMapCommand.Execute(rectVm);
            e.Handled = true;
        }
    }
}