using System.Globalization;
using WinDirStat.Core.Interfaces;
using Microsoft.Windows.Globalization;
using Microsoft.Windows.ApplicationModel.Resources;

namespace WinDirStat_App.Services;

public class WinUiLocalizationService : ILocalizationService
{
    private readonly ResourceManager _resourceManager = new();

    public string CurrentLanguage
    {
        get => ApplicationLanguages.PrimaryLanguageOverride;
        set => ApplicationLanguages.PrimaryLanguageOverride = value;
    }

    public void SetLanguage(string cultureCode)
    {
        ApplicationLanguages.PrimaryLanguageOverride = cultureCode;

        var culture = new CultureInfo(cultureCode);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    public string GetString(string key)
    {
        var resourceContext = _resourceManager.CreateResourceContext();
        resourceContext.QualifierValues["Language"] = CurrentLanguage;

        return _resourceManager.MainResourceMap.GetValue($"Resources/{key}", resourceContext).ValueAsString;
    }
}