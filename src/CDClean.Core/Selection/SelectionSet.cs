using CDClean.Core.Native;

namespace CDClean.Core.Selection;

/// <summary>One row the selection can refer to. Visible is an output of ApplyView.</summary>
public sealed class SelRecord
{
    public required FileKey Key { get; init; }
    public long Bytes { get; set; }
    public bool Exists { get; set; } = true;
    public bool Visible { get; set; }
    public string Path { get; init; } = "";
    /// <summary>The UI row this record belongs to, for filters that need more than the path.</summary>
    public object? Tag { get; init; }
}

public sealed record SelStats(
    int VisibleCount, int SelectedVisible, int SelectedHidden, int SelectedTotal, int Stale,
    long SelectedVisibleBytes, long SelectedHiddenBytes, long SelectedTotalBytes);

/// <summary>
/// Selection keyed by file identity, never by row index, so it survives filtering, sorting and rescans.
/// Port of dw_sel from the DiskWise prototype. A null filter means "show everything".
/// </summary>
public sealed class SelectionSet
{
    private readonly Dictionary<FileKey, long> _sel = new();

    public int Count => _sel.Count;
    public long TotalBytes => _sel.Values.Sum();
    public bool Contains(FileKey k) => _sel.ContainsKey(k);
    public IEnumerable<FileKey> Keys => _sel.Keys;

    /// <summary>Returns true if the key was new. Re-adding refreshes the size.</summary>
    public bool Add(FileKey k, long bytes)
    {
        var isNew = !_sel.ContainsKey(k);
        _sel[k] = bytes;
        return isNew;
    }

    public bool Remove(FileKey k) => _sel.Remove(k);
    public void Clear() => _sel.Clear();

    private static bool Visible(SelRecord r, Func<SelRecord, bool>? filter) => r.Exists && (filter == null || filter(r));

    public SelStats ApplyView(IList<SelRecord> recs, Func<SelRecord, bool>? filter)
    {
        int vis = 0, selVis = 0, selHid = 0;
        long visBytes = 0, hidBytes = 0;
        foreach (var r in recs)
        {
            r.Visible = Visible(r, filter);
            if (r.Visible) vis++;
            if (!_sel.ContainsKey(r.Key)) continue;
            if (r.Visible) { selVis++; visBytes += r.Bytes; }
            else { selHid++; if (r.Exists) hidBytes += r.Bytes; }
        }
        return new SelStats(vis, selVis, selHid, Count, 0, visBytes, hidBytes, TotalBytes);
    }

    public int SelectVisible(IEnumerable<SelRecord> recs) =>
        recs.Count(r => r.Visible && r.Exists && Add(r.Key, r.Bytes));

    /// <summary>Selects every record matching the filter, including ones not on screen.</summary>
    public int SelectAllMatching(IEnumerable<SelRecord> recs, Func<SelRecord, bool>? filter) =>
        recs.Count(r => Visible(r, filter) && Add(r.Key, r.Bytes));

    public int DeselectVisible(IEnumerable<SelRecord> recs) => recs.Count(r => r.Visible && Remove(r.Key));

    /// <summary>Inverts only rows the user can see. Hidden selections are untouched.</summary>
    public SelStats InvertVisible(IList<SelRecord> recs, Func<SelRecord, bool>? filter)
    {
        foreach (var r in recs)
        {
            r.Visible = Visible(r, filter);
            if (!r.Visible) continue;
            if (!Remove(r.Key)) Add(r.Key, r.Bytes);
        }
        return ApplyView(recs, filter);
    }

    /// <summary>
    /// After a rescan: refresh sizes, and count selections whose file is gone as stale.
    /// Stale selections are kept, not dropped, and must block a destructive job until reviewed.
    /// </summary>
    public SelStats Reconcile(IList<SelRecord> recs, Func<SelRecord, bool>? filter)
    {
        var present = new HashSet<FileKey>();
        foreach (var r in recs)
        {
            if (!r.Exists) continue;
            present.Add(r.Key);
            if (_sel.ContainsKey(r.Key)) _sel[r.Key] = r.Bytes;
        }
        var st = ApplyView(recs, filter);
        return st with { Stale = _sel.Keys.Count(k => !present.Contains(k)) };
    }
}
