using Volumetric.Core.Interfaces;

namespace Volumetric.Tests.FakeService;

public class FakeLocalizationService : ILocalizationService
{
    public string CurrentLanguage { get; private set; } = "uk-UA";

    public void SetLanguage(string cultureCode)
    {
        CurrentLanguage = cultureCode;
    }

    public string GetString(string key) => key;
}
