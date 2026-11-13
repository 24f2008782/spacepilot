using CDClean.Core.Disk;
using Xunit;

/// <summary>A file held open by another process (as pagefile.sys is) must still be counted, not shown as unreadable.</summary>
public class PagefileTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "cdclean-locked-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void Locked_file_is_counted_from_metadata()
    {
        Directory.CreateDirectory(_dir);
        var p = Path.Combine(_dir, "pagefile-like.sys");
        using (var held = new FileStream(p, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
        {
            held.Write(new byte[300_000]);
            held.Flush(true);
            var tree = DiskScanner.Scan(_dir);
            var idx = Enumerable.Range(0, tree.Nodes.Count).Single(i => tree.Nodes[i].Name == "pagefile-like.sys");
            var node = tree.Nodes[idx];
            Assert.Equal(NodeKind.File, node.Kind);
            Assert.Null(tree.ErrorOf(idx));
            Assert.Equal(300_000, node.OwnLogical);
            Assert.True(node.OwnAllocated >= 300_000);
        }
    }
}
