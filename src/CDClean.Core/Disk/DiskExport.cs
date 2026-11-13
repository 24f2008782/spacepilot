using System.Text;
using System.Text.Json;

namespace CDClean.Core.Disk;

/// <summary>Plain-text exports of a disk scan. Pure functions of the tree, so they can be tested without the UI.</summary>
public static class DiskExport
{
    public static string Csv(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";

    /// <summary>One row per node: path, kind, folder totals, modified time (UTC, ISO 8601) and error text.</summary>
    public static void WriteCsv(DiskTree t, string path)
    {
        using var w = new StreamWriter(path, false, new UTF8Encoding(true));
        WriteCsv(t, w);
    }

    public static void WriteCsv(DiskTree t, TextWriter w)
    {
        w.WriteLine("path,kind,allocated_bytes,logical_bytes,files,modified_utc,error");
        for (var i = 0; i < t.Nodes.Count; i++)
        {
            var n = t.Nodes[i];
            var modified = n.Modified == DateTime.MinValue ? "" : n.Modified.ToString("o");
            w.WriteLine($"{Csv(t.PathOf(i))},{n.Kind},{n.InclusiveAllocated},{n.InclusiveLogical},{n.InclusiveFiles},{modified},{Csv(t.ErrorOf(i) ?? "")}");
        }
    }

    /// <summary>Totals, ledger, and the largest files and folders. The size model is written into the file.</summary>
    public static string SummaryJson(DiskTree t, IEnumerable<LedgerLine> ledger, DateTime scannedAt)
    {
        var files = Enumerable.Range(0, t.Nodes.Count).Where(i => t.Nodes[i].Kind == NodeKind.File)
            .OrderByDescending(i => t.Nodes[i].OwnAllocated).Take(200)
            .Select(i => new { path = t.PathOf(i), bytes = t.Nodes[i].OwnAllocated }).ToList();
        var folders = Enumerable.Range(0, t.Nodes.Count).Where(i => t.Nodes[i].Kind == NodeKind.Dir)
            .OrderByDescending(i => t.Nodes[i].InclusiveAllocated).Take(200)
            .Select(i => new { path = t.PathOf(i), bytes = t.Nodes[i].InclusiveAllocated }).ToList();
        var summary = new
        {
            root = t.RootPath,
            scannedAt,
            sizeModel = "On-disk (allocated) bytes. Hard links counted once. Cloud placeholders 0. Junctions and symlinks not followed.",
            totalAllocated = t.TotalAllocated,
            totalLogical = t.TotalLogical,
            files = t.FileCount,
            unreadable = t.UnreadableCount,
            ledger = ledger.Select(l => new { l.Name, l.Bytes, l.Note, l.InTotal }).ToList(),
            largestFiles = files,
            largestFolders = folders,
        };
        return JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
    }
}
