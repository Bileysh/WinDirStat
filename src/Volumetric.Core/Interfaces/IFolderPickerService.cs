namespace Volumetric.Core.Interfaces;

public interface IFolderPickerService
{
    Task<string?> PickFolderAsync();
}
