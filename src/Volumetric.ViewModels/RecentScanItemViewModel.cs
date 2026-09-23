namespace Volumetric.ViewModels;

public class RecentScanItemViewModel
{
    public string FullPath { get; }
    public string DisplayName { get; }

    public RecentScanItemViewModel(string fullPath)
    {
        FullPath = fullPath;

        var trimmed = fullPath.TrimEnd('\\');
        var name = Path.GetFileName(trimmed);
        DisplayName = string.IsNullOrEmpty(name) ? fullPath : name;
    }
}
