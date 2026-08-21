using System.Globalization;
using WinDirStat.Core.Interfaces;
using Microsoft.Windows.Globalization;
using Microsoft.Windows.ApplicationModel.Resources;

namespace WinDirStat_App.Services;

public class WinUiLocalizationService : ILocalizationService
{
    private readonly ResourceLoader _resourceLoader = new();

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

        CurrentLanguage = cultureCode;
    }

    public string GetString(string key)
    {
        var resourceManager = new ResourceManager();
        var resourceContext = resourceManager.CreateResourceContext();
        resourceContext.QualifierValues["Language"] = CurrentLanguage;
        
        return resourceManager.MainResourceMap.GetValue($"Resources/{key}", resourceContext).ValueAsString;
    }
    
}