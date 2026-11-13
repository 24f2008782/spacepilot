using CDClean.Core.Disk;
using Xunit;

/// <summary>Export functions on a hand-made tree (no disk access).</summary>
public class DiskExportTests
{
    private static DiskTree Sample()
    {
        var t = new DiskTree(@"C:\");
        var d = t.Add(0, "docs", NodeKind.Dir, 0, 0, DateTime.MinValue);
        t.Add(d, "say \"hi\".txt", NodeKind.File, 10, 4096, new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        t.Add(0, "locked", NodeKind.Unreadable, 0, 0, DateTime.MinValue, error: "denied, sorry");
        t.Rollup();
        return t;
    }

    [Fact] // one header line plus one row per node; quotes and commas in paths and errors are escaped
    public void Csv_has_one_row_per_node_and_escapes_text()
    {
        using var w = new StringWriter();
        DiskExport.WriteCsv(Sample(), w);
        var lines = w.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(5, lines.Length);                                   // header + root + docs + file + locked
        Assert.StartsWith("path,kind,", lines[0]);
        Assert.Contains("\"C:\\docs\\say \"\"hi\"\".txt\"", lines[3]);
        Assert.Contains("\"denied, sorry\"", lines[4]);
    }

    [Fact] // the summary carries totals, the size model and the ledger lines
    public void Summary_json_has_totals_and_size_model()
    {
        var t = Sample();
        var json = DiskExport.SummaryJson(t, Ledger.Build(t, 1 << 20, wholeDrive: false), DateTime.UtcNow);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal(t.TotalAllocated, root.GetProperty("totalAllocated").GetInt64());
        Assert.Equal(1, root.GetProperty("files").GetInt32());
        Assert.Equal(1, root.GetProperty("unreadable").GetInt32());
        Assert.Contains("On-disk", root.GetProperty("sizeModel").GetString());
        Assert.True(root.GetProperty("ledger").GetArrayLength() >= 4);
    }
}
