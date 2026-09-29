using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Volumetric.ViewModels;

namespace Volumetric_App.UserControls;

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

    private void OnSelectCustomFolderClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.OpenFolderCommand.CanExecute(null) == true)
        {
            ViewModel.OpenFolderCommand.Execute(null);
        }
    }

    private void OnRefreshDrivesClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.RefreshDrivesCommand.CanExecute(null) == true)
        {
            ViewModel.RefreshDrivesCommand.Execute(null);
        }
    }

    private void OnDriveClicked(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: DriveItemViewModel drive })
        {
            ViewModel?.SelectDriveCommand.Execute(drive);
        }
    }

    private void OnRecentScanClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: RecentScanItemViewModel recentScan })
        {
            ViewModel?.OpenRecentScanCommand.Execute(recentScan);
        }
    }
}
