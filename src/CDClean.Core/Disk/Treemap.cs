namespace CDClean.Core.Disk;

public readonly record struct TileRect(double X, double Y, double W, double H)
{
    public double Area => W * H;
}

/// <summary>
/// Squarified treemap (Bruls, Huizing and van Wijk, 2000). Written from the paper, not from any product's code.
/// Tiles fill the bounds exactly; each tile's area is proportional to its value. Zero or negative values get no tile.
/// </summary>
public static class Treemap
{
    /// <summary>Returns one tile per positive value, as (index into values, tile). Input order is not preserved.</summary>
    public static List<(int Index, TileRect Rect)> Layout(IReadOnlyList<long> values, TileRect bounds)
    {
        var result = new List<(int, TileRect)>();
        var order = Enumerable.Range(0, values.Count).Where(i => values[i] > 0)
            .OrderByDescending(i => values[i]).ToList();
        var total = order.Sum(i => (double)values[i]);
        if (order.Count == 0 || total <= 0 || bounds.W <= 0 || bounds.H <= 0) return result;

        var scale = bounds.W * bounds.H / total;
        var areas = order.Select(i => values[i] * scale).ToArray();
        var r = bounds;
        var start = 0;
        while (start < areas.Length)
        {
            var side = Math.Min(r.W, r.H);
            var end = start + 1;
            // Keep adding items while the worst aspect ratio does not get worse.
            while (end < areas.Length && Worst(areas, start, end + 1, side) <= Worst(areas, start, end, side)) end++;
            r = LayRow(areas, order, start, end, r, result);
            start = end;
        }
        return result;
    }

    private static double Worst(double[] areas, int start, int end, double side)
    {
        double sum = 0, min = double.MaxValue, max = 0;
        for (var i = start; i < end; i++)
        {
            sum += areas[i];
            min = Math.Min(min, areas[i]);
            max = Math.Max(max, areas[i]);
        }
        var s2 = side * side;
        return Math.Max(s2 * max / (sum * sum), sum * sum / (s2 * min));
    }

    /// <summary>Places items [start, end) along one edge and returns the space that is left.</summary>
    private static TileRect LayRow(double[] areas, List<int> order, int start, int end, TileRect r,
        List<(int, TileRect)> result)
    {
        double sum = 0;
        for (var i = start; i < end; i++) sum += areas[i];
        if (r.W >= r.H)
        {
            // Lay the row as a column on the left edge.
            var colW = sum / r.H;
            var y = r.Y;
            for (var i = start; i < end; i++)
            {
                var h = areas[i] / colW;
                result.Add((order[i], new TileRect(r.X, y, colW, h)));
                y += h;
            }
            return new TileRect(r.X + colW, r.Y, r.W - colW, r.H);
        }
        else
        {
            // Lay the row across the top edge.
            var rowH = sum / r.W;
            var x = r.X;
            for (var i = start; i < end; i++)
            {
                var w = areas[i] / rowH;
                result.Add((order[i], new TileRect(x, r.Y, w, rowH)));
                x += w;
            }
            return new TileRect(r.X, r.Y + rowH, r.W, r.H - rowH);
        }
    }
}
