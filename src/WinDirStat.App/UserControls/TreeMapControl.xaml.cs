using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
            new PropertyMetadata(null));

    public TreeMapControl() => InitializeComponent();
    
    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TreeMapControl control && e.NewValue is MainPageViewModel vm)
        {
            if (control.RootGrid.ActualWidth > 0 && control.RootGrid.ActualHeight > 0)
            {
                vm.UpdateTreeMapSize(control.RootGrid.ActualWidth, control.RootGrid.ActualHeight);
            }
        }
    }
    
    private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e) =>
        ViewModel?.UpdateTreeMapSize(e.NewSize.Width, e.NewSize.Height);

    private Visibility GetNoDataVisibility(int count, bool isScanning) =>
        count == 0 && !isScanning ? Visibility.Visible : Visibility.Collapsed;
}