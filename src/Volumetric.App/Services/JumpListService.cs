using Serilog;
using Volumetric.Core.Interfaces;
using Windows.UI.StartScreen;

namespace Volumetric_App.Services;

public sealed class JumpListService(IScanStateService scanStateService) : IRecentScansService
{
    private const int MaxRecentEntries = 5;

    public void Initialize()
    {
        scanStateService.StateChanged += (sender, result) =>
        {
            if (result is not null)
            {
                _ = UpdateAsync(result.RootPath);
            }
        };
    }

    public async Task<IReadOnlyList<string>> GetRecentPathsAsync()
    {
        try
        {
            if (!JumpList.IsSupported())
            {
                return [];
            }

            var jumpList = await JumpList.LoadCurrentAsync();
            return jumpList.Items
                .Select(item => item.Arguments)
                .Where(path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                .Take(MaxRecentEntries)
                .ToList();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "JumpListService: failed to read recent scans");
            return [];
        }
    }

    private static async Task UpdateAsync(string scannedPath)
    {
        try
        {
            if (!JumpList.IsSupported())
            {
                return;
            }

            var jumpList = await JumpList.LoadCurrentAsync();
            jumpList.SystemGroupKind = JumpListSystemGroupKind.None;

            var remainingPaths = jumpList.Items
                .Select(item => item.Arguments)
                .Where(path => !string.IsNullOrWhiteSpace(path) &&
                               !string.Equals(path, scannedPath, StringComparison.OrdinalIgnoreCase))
                .Take(MaxRecentEntries - 1)
                .ToList();

            jumpList.Items.Clear();

            foreach (var path in remainingPaths.Prepend(scannedPath))
            {
                var jumpItem = JumpListItem.CreateWithArguments(path, path.TrimEnd('\\'));
                jumpItem.Description = path;
                jumpItem.GroupName = "Recent scans";
                jumpItem.Logo = new Uri("ms-appx:///Assets/AppIcon.ico");
                jumpList.Items.Add(jumpItem);
            }

            await jumpList.SaveAsync();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "JumpListService: failed to update jump list for '{ScannedPath}'", scannedPath);
        }
    }
}
