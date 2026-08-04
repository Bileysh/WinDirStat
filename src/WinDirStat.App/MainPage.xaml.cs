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
    }
}
