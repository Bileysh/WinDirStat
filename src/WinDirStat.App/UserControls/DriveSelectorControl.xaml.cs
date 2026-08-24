using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WinDirStat.ViewModels;

namespace WinDirStat_App.UserControls;

public sealed partial class DriveSelectorControl : UserControl
{
    public MainPageViewModel ViewModel
    {
        get => (MainPageViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(MainPageViewModel), typeof(DriveSelectorControl),
            new PropertyMetadata(null));

    public DriveSelectorControl() => InitializeComponent();

    private void OnDriveTapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: DriveItemViewModel drive })
        {
            ViewModel?.SelectDriveCommand.Execute(drive);
            e.Handled = true;
        }
    }
}