namespace WinDirStat.Core.Interfaces;

public interface ILocalizationService
{
    string CurrentLanguage { get; }
    void SetLanguage(string cultureCode);
    string GetString(string key); 
}