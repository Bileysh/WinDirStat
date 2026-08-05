using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WinDirStat.ViewModels;

/// <summary>
/// Sample ViewModel using CommunityToolkit.Mvvm partial property syntax.
/// Uses <see cref="ObservableProperty"/> for change notification and
/// <see cref="RelayCommand"/> for command binding.
/// </summary>
public partial class MainPageViewModel : ObservableObject
{
    [ObservableProperty]
    private string _greeting = "Welcome to WinDirStat";

    [ObservableProperty]
    private int _counter;

    [RelayCommand]
    private void IncrementCounter() => Counter++;

    [RelayCommand]
    private void DecrementCounter() => Counter--;
}