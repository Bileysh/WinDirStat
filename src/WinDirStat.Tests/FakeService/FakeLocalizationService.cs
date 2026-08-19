using WinDirStat.Core.Interfaces;

namespace WinDirStat.Tests.FakeService;

public class FakeLocalizationService : ILocalizationService
{
    public string CurrentLanguage { get; private set; } = "uk-UA";

    public void SetLanguage(string cultureCode)
    {
        CurrentLanguage = cultureCode;
    }
}