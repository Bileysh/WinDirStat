using Volumetric.Core.Entities;

namespace Volumetric.ViewModels;

internal sealed class SearchFilter
{
    private readonly HashSet<FileSystemNode> _visibleNodes = [];

    public SearchFilter(FileSystemNode root, string query)
    {
        Collect(root, query);
    }

    public bool IsVisible(FileSystemNode node) => _visibleNodes.Contains(node);

    private bool Collect(FileSystemNode node, string query)
    {
        var anyChildVisible = false;
        foreach (var child in node.Children)
        {
            if (Collect(child, query))
            {
                anyChildVisible = true;
            }
        }

        if (!anyChildVisible && !node.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        _visibleNodes.Add(node);
        return true;
    }
}
