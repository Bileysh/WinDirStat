namespace WinDirStat.Core.Interfaces;

public interface IDialogService
{
    Task ShowMessageAsync(string title, string message, string closeButtonText = "OK");
}