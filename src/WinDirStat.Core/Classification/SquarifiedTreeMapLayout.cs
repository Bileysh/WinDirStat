using WinDirStat.Core.Entities;

namespace WinDirStat.Core.Classification;

public static class SquarifiedTreeMapLayout
{
    private record Item(FileSystemNode Node, double Area);

    public static List<TreeMapRect> Compute(FileSystemNode node, double x, double y, double width, double height)
    {
        var childrenData = node.Children
            .Where(c => c.SizeLogical > 0)
            .Select(c => new { Node = c, Weight = Math.Sqrt(c.SizeLogical) })
            .OrderByDescending(c => c.Weight)
            .ToList();

        var result = new List<TreeMapRect>();
        if (childrenData.Count == 0 || width <= 0 || height <= 0) return result;

        var totalWeight = childrenData.Sum(c => c.Weight);
        var scale = (width * height) / totalWeight;

        var mappedItems = childrenData.Select(c => new Item(c.Node, c.Weight * scale));
        var items = new Queue<Item>(mappedItems.ToList());

        var rect = (X: x, Y: y, W: width, H: height);
        var row = new List<Item>();

        while (items.Count > 0)
        {
            var next = items.Peek();
            var sideLength = Math.Min(rect.W, rect.H);

            var rowWithNext = new List<Item>(row) { next };
            
            if (row.Count == 0 || Worst(row, sideLength) >= Worst(rowWithNext, sideLength))
            {
                row.Add(next);
                items.Dequeue();
            }
            else
            {
                rect = LayoutRow(row, rect, result);
                row.Clear();
            }
        }

        if (row.Count > 0)
            LayoutRow(row, rect, result);

        return result;
    }

    private static double Worst(List<Item> row, double sideLength)
    {
        var sum = row.Sum(i => i.Area);
        if (sum == 0) return double.MaxValue; 
        
        var max = row.Max(i => i.Area);
        var min = row.Min(i => i.Area);
        var s2 = sum * sum;
        var w2 = sideLength * sideLength;
        return Math.Max(w2 * max / s2, s2 / (w2 * min));
    }

    private static (double X, double Y, double W, double H) LayoutRow(
        List<Item> row, (double X, double Y, double W, double H) rect, List<TreeMapRect> result)
    {
        var rowArea = row.Sum(i => i.Area);
        if (rowArea == 0) return rect; 

        if (rect.W <= rect.H)
        {
            var rowHeight = rowArea / rect.W;
            var offsetX = rect.X;
            foreach (var item in row)
            {
                var w = item.Area / rowHeight;
                result.Add(new TreeMapRect(item.Node, offsetX, rect.Y, w, rowHeight));
                offsetX += w;
            }

            return (rect.X, rect.Y + rowHeight, rect.W, rect.H - rowHeight);
        }
        else
        {
            var rowWidth = rowArea / rect.H;
            var offsetY = rect.Y;
            foreach (var item in row)
            {
                var h = item.Area / rowWidth;
                result.Add(new TreeMapRect(item.Node, rect.X, offsetY, rowWidth, h));
                offsetY += h;
            }

            return (rect.X + rowWidth, rect.Y, rect.W - rowWidth, rect.H);
        }
    }
}