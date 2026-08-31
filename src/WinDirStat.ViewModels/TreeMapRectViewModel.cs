using System.Diagnostics;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.Input;
using WinDirStat.Core.Classification;
using WinDirStat.Core.Entities;
using WinDirStat.Core.Interfaces;

namespace WinDirStat.ViewModels;

public partial class TreeMapRectViewModel
{
    private readonly INotificationService? _notificationService;
    private readonly ILocalizationService? _localizationService;

    public FileSystemNode Node { get; }
    public double X { get; }
    public double Y { get; }
    public double Width { get; }
    public double Height { get; }
    public string Name { get; }
    public FileCategory Category { get; }
    public string ToolTipText { get; }
    public bool IsTitleVisible { get; }
    public string SizeFormatted { get; }
    public bool IsSizeVisible { get; }
    public bool IsFolder { get; }

    public TreeMapRectViewModel(TreeMapRect rect, INotificationService? notificationService = null,
        ILocalizationService? localizationService = null)
    {
        _notificationService = notificationService;
        _localizationService = localizationService;
        Node = rect.Node;
        X = rect.X;
        Y = rect.Y;
        Width = rect.Width;
        Height = rect.Height;
        Name = rect.Node.Name;

        IsFolder = rect.Node.IsDirectory;
        Category = IsFolder
            ? FileCategory.Folder
            : FileCategoryClassifier.Classify(rect.Node.Extension);

        SizeFormatted = SizeFormatter.Format(rect.Node.SizeLogical);
        ToolTipText = $"{rect.Node.Name}\n{SizeFormatted}";

        IsTitleVisible = Width > TreeMapConstants.MinWidthForTitle && Height > TreeMapConstants.MinHeightForTitle;
        IsSizeVisible = !IsFolder && Width > TreeMapConstants.MinWidthForSize &&
                        Height > TreeMapConstants.MinHeightForSize;
    }

    [RelayCommand]
    private void OpenInExplorer()
    {
        if (string.IsNullOrEmpty(Node.FullPath)) return;

        try
        {
            if (IsFolder)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Node.FullPath,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{Node.FullPath}\"",
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TreeMapRectViewModel] OpenInExplorer failed for '{Node.FullPath}': {ex}");
            _notificationService?.ShowNotification(
                GetLocalizedOrFallback("OpenInExplorerFailedTitle", "Failed to open Explorer"),
                $"'{Node.Name}': {ex.Message}");
        }
    }

    [RelayCommand]
    private void ShowProperties()
    {
        if (string.IsNullOrEmpty(Node.FullPath)) return;

        try
        {
            var succeeded = ShellInterop.SHObjectProperties(
                IntPtr.Zero, ShellInterop.SHOP_FILEPATH, Node.FullPath, null);

            if (!succeeded)
            {
                var error = Marshal.GetLastWin32Error();
                Debug.WriteLine(
                    $"[TreeMapRectViewModel] SHObjectProperties returned false for '{Node.FullPath}' " +
                    $"(Win32 error {error}).");
                _notificationService?.ShowNotification(
                    GetLocalizedOrFallback("ShowPropertiesFailedTitle", "Failed to open properties"), Node.Name);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TreeMapRectViewModel] ShowProperties failed for '{Node.FullPath}': {ex}");
            _notificationService?.ShowNotification(
                GetLocalizedOrFallback("ShowPropertiesFailedTitle", "Failed to open properties"),
                $"'{Node.Name}': {ex.Message}");
        }
    }
    private string GetLocalizedOrFallback(string key, string fallback) =>
        _localizationService?.GetString(key) ?? fallback;

    private static class ShellInterop
    {
        public const uint SHOP_FILEPATH = 0x2;

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SHObjectProperties(
            IntPtr hwnd, uint shopObjectType, string pszObjectName, string? pszPropertyPage);
    }
}