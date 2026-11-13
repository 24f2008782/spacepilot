using System.Collections;

namespace CDClean.Core.Disk;

public enum NodeKind : byte { File, Dir, Link, Unreadable }

/// <summary>One item in a scan. Own* values are this item only; Inclusive* values include everything below it.</summary>
public struct DiskNode
{
    public int Parent;              // -1 for the root
    public int FirstChild;          // -1 when there is no child
    public int NextSibling;         // -1 for the last child
    public NodeKind Kind;
    public bool Cloud;              // OneDrive-style placeholder: its data may not be on this disk
    public bool RepeatHardLink;     // second or later name for a file already counted
    public bool SystemFile;         // has the system attribute (pagefile, hiberfil, swapfile...)
    public string Name;
    public long OwnLogical;         // file length
    public long OwnAllocated;       // on-disk bytes; 0 for cloud placeholders and repeated hard links
    public long InclusiveAllocated;
    public long InclusiveLogical;
    public int InclusiveFiles;
    public DateTime Modified;       // newest modification time at or below this item (UTC)
}

/// <summary>
/// Chunked node store. A List&lt;DiskNode&gt; doubles its array when it grows, so a 1M-node scan briefly
/// needs the old and the new array at once. Chunks keep the peak flat, which matters on a 4 GB PC.
/// </summary>
public sealed class NodeStore : IReadOnlyList<DiskNode>
{
    private const int Shift = 15;            // 32,768 nodes per chunk, about 2.4 MB
    private const int Size = 1 << Shift;
    private const int Mask = Size - 1;
    private readonly List<DiskNode[]> _chunks = new();

    public int Count { get; private set; }

    public DiskNode this[int i]
    {
        get => _chunks[i >> Shift][i & Mask];
        set => _chunks[i >> Shift][i & Mask] = value;
    }

    public void Add(DiskNode node)
    {
        if ((Count & Mask) == 0) _chunks.Add(new DiskNode[Size]);
        _chunks[Count >> Shift][Count & Mask] = node;
        Count++;
    }

    public IEnumerator<DiskNode> GetEnumerator()
    {
        for (var i = 0; i < Count; i++) yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// Flat node store (report §9.4.2). Children are always added after their parent, so one backwards pass
/// rolls sizes up with no recursion. A scan is capped at a memory budget so a 4 GB PC stays usable.
/// </summary>
public sealed class DiskTree
{
    // 250 MB on a 4 GB PC, 300 MB on 8 GB or more. About 150 bytes per node (node, name and file ID).
    private static readonly int BudgetMbValue = Budget();
    public static int BudgetMb => BudgetMbValue;
    public static int MaxNodes { get; } = (int)((long)BudgetMbValue << 20) / 150;

    private static int Budget()
    {
        try { return GC.GetGCMemoryInfo().TotalAvailableMemoryBytes < 6L << 30 ? 250 : 300; }
        catch { return 250; }
    }

    public readonly NodeStore Nodes = new();
    private readonly Dictionary<int, string> _errors = new();
    private readonly object _gate = new();
    private long _hardLinkSaved;
    private volatile bool _truncated;

    public DiskTree(string rootPath)
    {
        RootPath = rootPath;
        Add(-1, rootPath, NodeKind.Dir, 0, 0, DateTime.MinValue);
    }

    public string RootPath { get; }

    /// <summary>On-disk bytes of extra hard-link names. Information only: the data is already in the total once.</summary>
    public long HardLinkSaved => Interlocked.Read(ref _hardLinkSaved);

    public void AddHardLinkSaved(long bytes) => Interlocked.Add(ref _hardLinkSaved, bytes);

    /// <summary>True when the memory budget stopped the map. The status line says so; the ledger shows the rest as not attributed.</summary>
    public bool Truncated => _truncated;

    public int Add(int parent, string name, NodeKind kind, long logical, long allocated, DateTime modified,
        bool cloud = false, bool repeat = false, string? error = null, bool system = false)
    {
        lock (_gate)
        {
            if (Nodes.Count >= MaxNodes) { _truncated = true; return -1; }
            var p = parent >= 0 ? Nodes[parent] : default;
            var i = Nodes.Count;
            Nodes.Add(new DiskNode
            {
                Parent = parent, FirstChild = -1, NextSibling = parent >= 0 ? p.FirstChild : -1,
                Kind = kind, Name = name,
                OwnLogical = logical, OwnAllocated = allocated,
                InclusiveLogical = logical, InclusiveAllocated = allocated,
                InclusiveFiles = kind == NodeKind.File ? 1 : 0,
                Modified = modified, Cloud = cloud, RepeatHardLink = repeat, SystemFile = system,
            });
            if (parent >= 0)
            {
                p.FirstChild = i;
                Nodes[parent] = p;
            }
            if (error != null) _errors[i] = error;
            return i;
        }
    }

    public void SetError(int node, string error)
    {
        if (node < 0) return;
        lock (_gate) _errors[node] = error;
    }

    public string? ErrorOf(int node) => _errors.TryGetValue(node, out var e) ? e : null;

    public bool HasChildren(int node) => Nodes[node].FirstChild >= 0;

    public IEnumerable<int> Children(int node)
    {
        for (var c = Nodes[node].FirstChild; c >= 0; c = Nodes[c].NextSibling) yield return c;
    }

    /// <summary>Adds every item's own sizes into its ancestors. Call once after the scan.</summary>
    public void Rollup()
    {
        for (var i = Nodes.Count - 1; i > 0; i--)
        {
            var child = Nodes[i];
            if (child.Parent < 0) continue;
            var p = Nodes[child.Parent];
            p.InclusiveAllocated += child.InclusiveAllocated;
            p.InclusiveLogical += child.InclusiveLogical;
            p.InclusiveFiles += child.InclusiveFiles;
            if (child.Modified > p.Modified) p.Modified = child.Modified;
            Nodes[child.Parent] = p;
        }
    }

    /// <summary>Full path of a node, built from the names above it.</summary>
    public string PathOf(int node)
    {
        var parts = new List<string>();
        for (var i = node; i >= 0; i = Nodes[i].Parent)
            if (Nodes[i].Parent >= 0) parts.Add(Nodes[i].Name);
        parts.Reverse();
        return parts.Count == 0 ? RootPath : Path.Combine(RootPath, Path.Combine(parts.ToArray()));
    }

    public long TotalAllocated => Nodes[0].InclusiveAllocated;
    public long TotalLogical => Nodes[0].InclusiveLogical;
    public int FileCount => Nodes[0].InclusiveFiles;

    public int UnreadableCount
    {
        get
        {
            var n = 0;
            for (var i = 0; i < Nodes.Count; i++)
                if (Nodes[i].Kind == NodeKind.Unreadable || _errors.ContainsKey(i)) n++;
            return n;
        }
    }
}
