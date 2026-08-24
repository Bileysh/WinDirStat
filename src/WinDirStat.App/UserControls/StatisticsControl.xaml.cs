using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinDirStat.ViewModels;

namespace WinDirStat_App.UserControls;

public sealed partial class StatisticsControl : UserControl
{
    public MainPageViewModel ViewModel
    {
        get => (MainPageViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(MainPageViewModel),
            typeof(StatisticsControl),
            new PropertyMetadata(null));

    public StatisticsControl()
    {
        InitializeComponent();
    }

    private Visibility GetNoDataVisibility(int count, bool isScanning) =>
        count == 0 && !isScanning ? Visibility.Visible : Visibility.Collapsed;
}