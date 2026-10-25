namespace Volumetric.Core.Interfaces;

public interface IRecentScansService
{
    Task<IReadOnlyList<string>> GetRecentPathsAsync();
}
