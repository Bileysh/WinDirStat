using WinDirStat.Core.Entities;
using WinDirStat.ViewModels;

namespace WinDirStat.Tests.Tests;

public class TreeMapRectViewModelTests
{
    [Fact]
    public void ShowPropertiesCommand_WhenNodeHasNoPath_DoesNotThrow()
    {
        var node = new FileSystemNode { Name = "unnamed", RootFullPathOverride = string.Empty };
        var rect = new TreeMapRect(node, 0, 0, 100, 100);
        var vm = new TreeMapRectViewModel(rect);

        var exception = Record.Exception(() => vm.ShowPropertiesCommand.Execute(null));

        Assert.Null(exception);
    }
}