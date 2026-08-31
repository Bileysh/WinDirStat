using WinDirStat.Core.Entities;

namespace WinDirStat.Core.Interfaces;

public interface IElevatedScanHelper
{
 
    bool TryScanElevated(IReadOnlyList<string> paths, out IReadOnlyDictionary<string, FileSystemNode> results);
}
