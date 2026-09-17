using Volumetric.Core.Entities;

namespace Volumetric.Core.Interfaces;

public interface IElevatedScanHelper
{
 
    bool TryScanElevated(IReadOnlyList<string> paths, out IReadOnlyDictionary<string, FileSystemNode> results);
}
