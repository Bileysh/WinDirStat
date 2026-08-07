namespace WinDirStat.Core.Interfaces;

public interface IFolderPickerService
{
    Task<string?> PickFolderAsync();
}