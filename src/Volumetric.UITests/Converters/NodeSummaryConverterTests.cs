using Volumetric.Core.Entities;
using Volumetric.ViewModels;
using Volumetric_App.Converters;

namespace Volumetric.UITests.Converters;

public class NodeSummaryConverterTests
{
    private readonly NodeSummaryConverter _converter = new();

    [Fact]
    public void Convert_NonNodeViewModelValue_ReturnsEmptyString()
    {
        var result = _converter.Convert("not a node", typeof(string), null!, string.Empty);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Convert_FileNode_ReturnsEmptyStringWithoutTouchingLocalization()
    {
        var fileNode = new FileSystemNode { Name = "readme.txt", IsDirectory = false };
        var vm = new NodeViewModel(fileNode);

        var result = _converter.Convert(vm, typeof(string), null!, string.Empty);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void ConvertBack_Throws()
    {
        Assert.Throws<NotImplementedException>(() =>
            _converter.ConvertBack(string.Empty, typeof(NodeViewModel), null!, string.Empty));
    }
}
