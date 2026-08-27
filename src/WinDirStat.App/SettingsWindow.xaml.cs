using Microsoft.UI.Xaml.Controls;
using WinDirStat.ViewModels;

namespace WinDirStat_App;

public sealed partial class SettingsWindow
{
    public SettingsViewModel ViewModel { get; }

    public SettingsWindow(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        
        ScanIntervalNumberBox.Value = ViewModel.ScanIntervalMinutes;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        Closed += (_, _) => ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.ScanIntervalMinutes))
        {
            ScanIntervalNumberBox.Value = ViewModel.ScanIntervalMinutes;
        }
    }

    private void ScanIntervalNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (double.IsNaN(args.NewValue)) return;

        var newValue = (uint)Math.Max(15, args.NewValue);
        if (newValue != ViewModel.ScanIntervalMinutes)
        {
            ViewModel.ScanIntervalMinutes = newValue;
        }
    }
}