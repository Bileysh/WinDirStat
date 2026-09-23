using Volumetric.Core.Interfaces;

namespace Volumetric.Tests.FakeService;

public class FakeRecentScansService : IRecentScansService
{
    public IReadOnlyList<string> PathsToReturn { get; set; } = [];
    public Task<IReadOnlyList<string>> GetRecentPathsAsync() => Task.FromResult(PathsToReturn);
}
