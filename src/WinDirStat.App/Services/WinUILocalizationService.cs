using Windows.Globalization;
using WinDirStat.Core.Interfaces;

namespace WinDirStat_App.Services;

public class WinUiLocalizationService : ILocalizationService
{
    public string CurrentLanguage => ApplicationLanguages.PrimaryLanguageOverride;

    public void SetLanguage(string cultureCode)
    {
        ApplicationLanguages.PrimaryLanguageOverride = cultureCode;
    }
}