namespace Volumetric.Core.Interfaces;

public interface IDialogService
{
    Task ShowMessageAsync(string title, string message, string closeButtonText = "OK");
}
