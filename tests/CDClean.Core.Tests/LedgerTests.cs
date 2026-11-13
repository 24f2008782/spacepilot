using CDClean.Core.Disk;
using Xunit;

/// <summary>Ledger arithmetic on a hand-made tree (no disk access).</summary>
public class LedgerTests
{
    private static DiskTree Sample()
    {
        var t = new DiskTree(@"C:\");
        t.Add(0, "a.bin", NodeKind.File, 1000, 4096, DateTime.UtcNow);
        t.Add(0, "pagefile.sys", NodeKind.File, 4 << 20, 4 << 20, DateTime.UtcNow, system: true);
        t.Add(0, "cloud.docx", NodeKind.File, 500, 0, DateTime.UtcNow, cloud: true);
        t.Add(0, "locked", NodeKind.Unreadable, 0, 0, DateTime.MinValue, error: "denied");
        t.AddHardLinkSaved(4096);
        t.Rollup();
        return t;
    }

    private static long Bytes(List<LedgerLine> lines, string prefix) => lines.Single(l => l.Name.StartsWith(prefix)).Bytes;

    [Fact] // the named buckets add up to the drive's used space
    public void Named_buckets_add_up_to_used_space()
    {
        var used = 20L << 20;
        var lines = Ledger.Build(Sample(), used, wholeDrive: true);
        var sum = Bytes(lines, "Files and folders") + Bytes(lines, "System files") + Bytes(lines, "Not attributed");
        Assert.Equal(used, sum);
    }

    [Fact] // savings from hard links and cloud placeholders are shown but never added to the total
    public void Information_lines_are_not_in_the_total()
    {
        var lines = Ledger.Build(Sample(), 20L << 20, wholeDrive: true);
        Assert.False(lines.Single(l => l.Name.StartsWith("Hard-link")).InTotal);
        Assert.False(lines.Single(l => l.Name.StartsWith("Cloud")).InTotal);
        Assert.Equal(4096, Bytes(lines, "Hard-link"));
        Assert.Equal(500, Bytes(lines, "Cloud"));
    }

    [Fact] // a scan larger than the drive's used space is said so, not hidden
    public void Negative_remainder_is_explained()
    {
        var lines = Ledger.Build(Sample(), 1000, wholeDrive: true);
        var rest = lines.Single(l => l.Name.StartsWith("Not attributed"));
        Assert.True(rest.Bytes < 0);
        Assert.Contains("more than the drive reports", rest.Note);
    }

    [Fact] // unreadable items are counted in their own line
    public void Unreadable_items_are_counted()
    {
        var lines = Ledger.Build(Sample(), 20L << 20, wholeDrive: true);
        Assert.Contains("1 item(s) could not be read", lines.Single(l => l.Name.StartsWith("Unreadable")).Note);
    }

    [Fact] // a folder scan says the rest is outside the folder, not "metadata"
    public void Folder_scan_names_the_remainder_correctly()
    {
        var lines = Ledger.Build(Sample(), 20L << 20, wholeDrive: false);
        Assert.Contains("outside the scanned folder", lines.Single(l => l.Name.StartsWith("Not attributed")).Note);
    }
}
