using Microsoft.UI.Xaml;

namespace WinDirStat_App;

public sealed partial class MainWindow : Window
{
    public MainWindow(MainPage mainPage)
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");

        RootFrame.Content = mainPage;
    }
}
