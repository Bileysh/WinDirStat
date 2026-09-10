namespace WinDirStat.Core.Interfaces;

public interface IFileExplorerService
{
    void OpenInExplorer(string fullPath, bool isDirectory);
    void ShowProperties(string fullPath);
}
