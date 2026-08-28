namespace WinDirStat.Core.Interfaces;

public interface ISettingsFileService
{
    Task<string?> ExportAsync(string json, string suggestedFileName);

    Task<(string Json, string FileName)?> ImportAsync();
}