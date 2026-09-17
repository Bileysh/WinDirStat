using Volumetric.ViewModels;

namespace Volumetric_App;

public sealed partial class SettingsWindow : Microsoft.UI.Xaml.Window
{
    public SettingsViewModel ViewModel { get; }

    public SettingsWindow(SettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        ViewModel.WindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
    }
}