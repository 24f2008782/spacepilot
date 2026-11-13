using System.ComponentModel;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using CDClean.Core.Disk;
using CDClean.Core.Native;
using Xunit;

/// <summary>Batch 2.1 tests (playbook: tier2-plan.md §3). Synthetic trees are built only under E:\Temp.</summary>
public sealed class DiskTests : IDisposable
{
    private static readonly string Base = Directory.Exists(@"E:\") ? @"E:\Temp\cdclean-disk-test" : Path.Combine(Path.GetTempPath(), "cdclean-disk-test");
    private readonly string _root = Path.Combine(Base, Guid.NewGuid().ToString("N"));

    public DiskTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try
        {
            foreach (var f in Directory.EnumerateFiles(_root, "*", new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true }))
                File.SetAttributes(f, FileAttributes.Normal);
            Directory.Delete(_root, true);
        }
        catch { }
    }

    private string File_(string rel, int bytes)
    {
        var p = Path.Combine(_root, rel);
        Directory.CreateDirectory(Path.GetDirectoryName(p)!);
        File.WriteAllBytes(p, new byte[bytes]);
        return p;
    }

    [Fact] // SC-01 analogue: total equals the sum of allocated sizes of the files, computed independently
    public void Total_matches_independent_sum()
    {
        var files = new[] { File_("a.bin", 3000), File_("b.bin", 1 << 20), File_("sub\\c.bin", 5000), File_("sub\\deep\\d.bin", 17) };
        var expected = files.Sum(f => Files.Read(f).Allocated);
        var tree = DiskScanner.Scan(_root);
        Assert.Equal(expected, tree.TotalAllocated);
        Assert.Equal(files.Length, tree.FileCount);
        Assert.Equal(files.Sum(f => new FileInfo(f).Length), tree.TotalLogical);
    }

    [Fact] // SC-04: a file with a second hard-link name is counted once
    public void Hard_link_counts_once()
    {
        var a = File_("x\\a.bin", 1 << 20);
        Assert.True(CreateHardLinkW(Path.Combine(_root, "x\\b.bin"), a, IntPtr.Zero));
        var tree = DiskScanner.Scan(_root);
        Assert.Equal(Files.Read(a).Allocated, tree.TotalAllocated);
        Assert.Equal(1, tree.Nodes.Count(n => n.RepeatHardLink));
    }

    [Fact] // SC-03 / DE-09: a junction is recorded as a link and its target is not counted
    public void Junction_is_not_followed()
    {
        var outside = Path.Combine(Base, "outside-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outside);
        try
        {
            File.WriteAllBytes(Path.Combine(outside, "big.bin"), new byte[2 << 20]);
            var link = Path.Combine(_root, "j");
            Process.Start(new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{link}\" \"{outside}\"") { CreateNoWindow = true, UseShellExecute = false })!.WaitForExit();
            Assert.True(Directory.Exists(link));
            File_("local.bin", 100);

            var tree = DiskScanner.Scan(_root);
            var j = tree.Nodes.Single(n => n.Name == "j");
            Assert.Equal(NodeKind.Link, j.Kind);
            Assert.Empty(tree.Children(Array.IndexOf(tree.Nodes.ToArray(), j)));
            Assert.True(tree.TotalAllocated < (1 << 20), "the 2 MB file behind the junction must not be counted");
        }
        finally { try { Directory.Delete(outside, true); } catch { } }
    }

    [Fact] // SC-07: an unreadable folder is kept as a node with its error, not dropped
    public void Unreadable_folder_is_reported()
    {
        File_("open\\ok.bin", 10);
        File_("locked\\secret.bin", 10);
        var locked = Path.Combine(_root, "locked");
        var me = WindowsIdentity.GetCurrent().User!;
        var di = new DirectoryInfo(locked);
        var rule = new FileSystemAccessRule(me, FileSystemRights.ListDirectory, AccessControlType.Deny);
        var acl = di.GetAccessControl(); acl.AddAccessRule(rule); di.SetAccessControl(acl);
        try
        {
            var tree = DiskScanner.Scan(_root);
            var idx = Enumerable.Range(0, tree.Nodes.Count).Single(i => tree.Nodes[i].Name == "locked");
            Assert.False(string.IsNullOrEmpty(tree.ErrorOf(idx)));
            Assert.Equal(1, tree.FileCount); // only open\ok.bin
        }
        finally { acl.RemoveAccessRule(rule); di.SetAccessControl(acl); }
    }

    [Fact] // Deep trees use an explicit stack, so depth does not overflow
    public void Deep_tree_scans()
    {
        var rel = string.Join('\\', Enumerable.Repeat("d", 60));
        File_(rel + "\\leaf.bin", 4096);
        var tree = DiskScanner.Scan(_root);
        Assert.Equal(1, tree.FileCount);
    }

    [Fact] // Rollup: folder totals and newest time are right; paths rebuild
    public void Rollup_and_paths()
    {
        File_("p\\q\\r.bin", 4096);
        var tree = DiskScanner.Scan(_root);
        var r = Enumerable.Range(0, tree.Nodes.Count).Single(i => tree.Nodes[i].Name == "r.bin");
        Assert.Equal(Path.Combine(_root, "p", "q", "r.bin"), tree.PathOf(r));
        var p = Enumerable.Range(0, tree.Nodes.Count).Single(i => tree.Nodes[i].Name == "p");
        Assert.Equal(tree.Nodes[r].OwnAllocated, tree.Nodes[p].InclusiveAllocated);
        Assert.Equal(tree.Nodes[r].Modified, tree.Nodes[p].Modified);
    }

    [Fact] // Treemap: tiles fill the bounds, do not overlap, and are proportional to their values
    public void Treemap_tiles_exactly_and_proportionally()
    {
        var values = new long[] { 6, 6, 4, 3, 2, 2, 1, 0, -5 };
        var bounds = new TileRect(0, 0, 600, 400);
        var tiles = Treemap.Layout(values, bounds);
        Assert.Equal(7, tiles.Count);                       // zero and negative values get no tile
        var total = values.Where(v => v > 0).Sum();
        var area = 600.0 * 400.0;
        Assert.Equal(area, tiles.Sum(t => t.Rect.Area), 6);
        foreach (var (i, r) in tiles)
        {
            Assert.Equal(values[i] * area / total, r.Area, 6);
            Assert.True(r.X >= -1e-9 && r.Y >= -1e-9 && r.X + r.W <= 600 + 1e-9 && r.Y + r.H <= 400 + 1e-9);
        }
        for (var a = 0; a < tiles.Count; a++)
            for (var b = a + 1; b < tiles.Count; b++)
            {
                var p = tiles[a].Rect; var q = tiles[b].Rect;
                var ox = Math.Max(0, Math.Min(p.X + p.W, q.X + q.W) - Math.Max(p.X, q.X));
                var oy = Math.Max(0, Math.Min(p.Y + p.H, q.Y + q.H) - Math.Max(p.Y, q.Y));
                Assert.True(ox * oy < 1e-6, "tiles must not overlap");
            }
    }

    [Fact]
    public void Treemap_empty_and_degenerate_inputs()
    {
        Assert.Empty(Treemap.Layout(new long[] { 0, 0 }, new TileRect(0, 0, 10, 10)));
        Assert.Empty(Treemap.Layout(new long[] { 5 }, new TileRect(0, 0, 0, 10)));
        var one = Treemap.Layout(new long[] { 7 }, new TileRect(0, 0, 10, 10));
        Assert.Equal(100, one.Single().Rect.Area, 6);
    }

    [Fact] // USN parser: one version 2 record with a non-ASCII name
    public void Usn_record_parses()
    {
        var name = "résumé.txt";
        var nameBytes = Encoding.Unicode.GetBytes(name);
        var recLen = 60 + nameBytes.Length;
        recLen = (recLen + 7) & ~7;
        var buf = new byte[8 + recLen];
        BitConverter.GetBytes(0x1234UL).CopyTo(buf, 0);
        var o = 8;
        BitConverter.GetBytes((uint)recLen).CopyTo(buf, o);
        BitConverter.GetBytes((ushort)2).CopyTo(buf, o + 4);
        BitConverter.GetBytes(77UL).CopyTo(buf, o + 8);     // file reference
        BitConverter.GetBytes(5UL).CopyTo(buf, o + 16);     // parent reference
        BitConverter.GetBytes(0x20u).CopyTo(buf, o + 52);   // archive
        BitConverter.GetBytes((ushort)nameBytes.Length).CopyTo(buf, o + 56);
        BitConverter.GetBytes((ushort)60).CopyTo(buf, o + 58);
        nameBytes.CopyTo(buf, o + 60);

        var list = new List<UsnEntry>();
        var next = UsnReader.ParseBuffer(buf, 8 + recLen, list);
        Assert.Equal(0x1234UL, next);
        var e = Assert.Single(list);
        Assert.Equal(name, e.Name);
        Assert.Equal(77UL, e.Ref);
        Assert.Equal(5UL, e.Parent);
        Assert.False(e.IsDirectory);
    }

    [Fact] // USN tree builder keeps only what is under the chosen folder
    public void Usn_tree_builder_keeps_only_the_folder()
    {
        var all = new List<UsnEntry>
        {
            new(10, 5, "dir", 0x10),
            new(11, 10, "f.txt", 0x20),
            new(20, 99, "elsewhere.txt", 0x20),   // parent 99 is not under the root
        };
        var tree = UsnReader.BuildTree(@"C:\root", 5, all);
        Assert.Equal(3, tree.Nodes.Count);        // root, dir, f.txt
        var f = Enumerable.Range(0, tree.Nodes.Count).Single(i => tree.Nodes[i].Name == "f.txt");
        Assert.Equal(Path.Combine(@"C:\root", "dir", "f.txt"), tree.PathOf(f));
    }

    [Fact] // Live check: enumerates a real volume if the test run is elevated; otherwise confirms the clear "run as administrator" error
    public void Usn_live_volume_enumeration()
    {
        if (IsElevated())
        {
            var entries = UsnReader.EnumerateVolume('C');
            Assert.True(entries.Count > 1000, $"expected many entries, got {entries.Count}");
        }
        else
        {
            var ex = Assert.Throws<Win32Exception>(() => UsnReader.EnumerateVolume('C'));
            Assert.Contains("administrator", ex.Message);
        }
    }

    private static bool IsElevated()
    {
        using var id = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
    }

    [Fact] // system files (pagefile, hiberfil...) are flagged so the ledger can name them
    public void System_file_is_flagged()
    {
        var p = File_("pagefile.sys", 8192);
        File.SetAttributes(p, FileAttributes.System);
        var tree = DiskScanner.Scan(_root);
        Assert.True(tree.Nodes.Single(n => n.Name == "pagefile.sys").SystemFile);
    }

    [Fact] // hard-link savings are reported, and are not part of the total
    public void Hard_link_savings_are_reported()
    {
        var a = File_("hs\\a.bin", 1 << 20);
        Assert.True(CreateHardLinkW(Path.Combine(_root, "hs\\b.bin"), a, IntPtr.Zero));
        var tree = DiskScanner.Scan(_root);
        Assert.Equal(Files.Read(a).Allocated, tree.HardLinkSaved);
        Assert.Equal(Files.Read(a).Allocated, tree.TotalAllocated);
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateHardLinkW(string newLink, string existing, IntPtr sa);
}
