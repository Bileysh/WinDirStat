using Microsoft.UI.Xaml;

namespace WinDirStat_App.Services;

public static class WindowManagerConstants
{
    public const int MicaMinBuildNumber = 22000;

    public const double TitleBarRowHeight = 40;
    public const double TitleTextFontSize = 12;
    public const double TitleTextOpacity = 0.6;

    public const int WindowOffsetX = 50;
    public const int WindowOffsetY = 50;

    public const int StatisticsWindowWidth = 400;
    public const int StatisticsWindowHeight = 600;
    public const int TreeViewWindowWidth = 700;
    public const int TreeViewWindowHeight = 500;
    public const int TreeMapWindowWidth = 800;
    public const int TreeMapWindowHeight = 500;

    public static readonly Thickness TitleTextMargin = new(16, 0, 0, 0);
    public static readonly Thickness ContentMargin = new(16, 0, 16, 16);
}