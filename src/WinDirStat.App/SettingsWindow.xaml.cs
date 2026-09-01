using WinDirStat.ViewModels;

namespace WinDirStat_App;

public sealed partial class SettingsWindow : Microsoft.UI.Xaml.Window
{
    public SettingsViewModel ViewModel { get; }

    public SettingsWindow(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}