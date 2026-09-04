namespace WinDirStat.Core.Interfaces;

public interface ISettingsFileService
{
    Task<string?> ExportAsync(string json, string suggestedFileName, IntPtr ownerHwnd);

    Task<(string Json, string FileName)?> ImportAsync(IntPtr ownerHwnd);
}