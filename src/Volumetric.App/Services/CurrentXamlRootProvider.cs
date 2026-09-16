using Microsoft.UI.Xaml;

namespace Volumetric_App.Services;

public interface ICurrentXamlRootProvider
{
    XamlRoot? XamlRoot { get; set; }
}

public class CurrentXamlRootProvider : ICurrentXamlRootProvider
{
    public XamlRoot? XamlRoot { get; set; }
}
