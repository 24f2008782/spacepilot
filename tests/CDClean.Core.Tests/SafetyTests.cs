using CDClean.Core.Native;
using CDClean.Core.Safety;
using CDClean.Core.Selection;
using Xunit;

/// <summary>Port of the 39 checks in docs/diskwise/prototype/tests.c.</summary>
public class SelectionAndGateTests
{
    private static FileKey K(ulong id) => new(7, 0, id);
    private static SelRecord R(ulong id, long bytes, string path) => new() { Key = K(id), Bytes = bytes, Path = path };
    private static bool Large(SelRecord r) => r.Bytes > 1000;
    private static bool Narrow(SelRecord r) => r.Path.Contains("old");

    [Fact]
    public void Selection_persists_across_filters()
    {
        var s = new SelectionSet();
        var recs = new List<SelRecord>
        {
            R(1, 5000, @"C:\Users\a\Downloads\old-setup.exe"), R(2, 900, @"C:\Users\a\Docs\note.txt"),
            R(3, 7000, @"C:\Users\a\Downloads\old-video.mp4"), R(4, 20000, @"C:\Games\archive\big.bin"),
            R(5, 100, @"C:\Users\a\Docs\old-small.txt"),
        };
        var st = s.ApplyView(recs, Large);
        Assert.Equal(3, st.VisibleCount);
        s.SelectVisible(recs);
        Assert.Equal(3, s.Count);

        st = s.ApplyView(recs, Narrow); // "search within results"
        Assert.Equal(3, st.VisibleCount);
        Assert.Equal(2, st.SelectedVisible);
        Assert.Equal(1, st.SelectedHidden);
        Assert.Equal(20000, st.SelectedHiddenBytes);
        Assert.Equal(3, st.SelectedTotal);
        Assert.Equal(32000, st.SelectedTotalBytes);

        st = s.ApplyView(recs, null);
        Assert.Equal(5, st.VisibleCount);
        Assert.Equal(3, st.SelectedVisible);
        Assert.Equal(0, st.SelectedHidden);
    }

    [Fact]
    public void Bulk_operations_respect_the_view()
    {
        var s = new SelectionSet();
        var recs = new List<SelRecord>
        {
            R(10, 2000, @"C:\a\old1.bin"), R(11, 2000, @"C:\a\old2.bin"), R(12, 2000, @"C:\a\new1.bin"),
            R(13, 2000, @"C:\a\new2.bin"), R(14, 500, @"C:\a\old3.bin"), R(15, 500, @"C:\a\new3.bin"),
        };
        s.ApplyView(recs, Large);
        Assert.Equal(4, s.SelectAllMatching(recs, Large));

        var st = s.InvertVisible(recs, Narrow);
        Assert.Equal(3, st.VisibleCount);
        Assert.Equal(3, s.Count);
        Assert.True(s.Contains(K(12)) && s.Contains(K(13)));
        Assert.True(s.Contains(K(14)));

        Assert.Equal(1, s.DeselectVisible(recs));
        s.Clear();
        Assert.Equal(0, s.Count);
        Assert.Equal(0, s.TotalBytes);
    }

    [Fact]
    public void Rescan_marks_vanished_selection_stale_and_keeps_it()
    {
        var s = new SelectionSet();
        var recs = Enumerable.Range(20, 4).Select(i => R((ulong)i, 1000, $@"C:\a\{i}")).ToList();
        s.ApplyView(recs, null);
        s.SelectVisible(recs);
        Assert.Equal(4, s.Count);

        recs[1].Exists = false;
        recs[2].Bytes = 5000;
        var st = s.Reconcile(recs, null);
        Assert.Equal(1, st.Stale);
        Assert.Equal(4, st.SelectedTotal);
        Assert.Equal(1, st.SelectedHidden);
        Assert.Equal(8000, s.TotalBytes);
    }

    private static readonly Policy Pol = new(
        new[] { @"C:\Windows\", @"C:\Program Files\WindowsApps\", @"C:\System Volume Information\" },
        SizeWarnBytes: 10 << 20, MaxRisk: RiskClass.Review);

    private static GateInput G(ulong id, string path, ItemFlags f = ItemFlags.None, RiskClass r = RiskClass.Safe, bool exists = true) =>
        new(path, exists, K(id), 4096, f, r);

    [Theory]
    [InlineData(ItemFlags.Locked, Reason.Locked)]
    [InlineData(ItemFlags.HardLink, Reason.HardLink)]
    [InlineData(ItemFlags.Cloud, Reason.Cloud)]
    [InlineData(ItemFlags.RecentlyUsed, Reason.RecentUse)]
    public void Gate_warns(ItemFlags flag, Reason expected)
    {
        var g = G(35, @"C:\Users\a\AppData\Local\T\x.bin", flag);
        var (v, rs) = Gate.Evaluate(g, g.Key, Pol);
        Assert.Equal(Verdict.Warn, v);
        Assert.Equal(expected, rs[0]);
    }

    [Fact]
    public void Gate_blocks()
    {
        var ok = G(30, @"C:\Users\a\Downloads\x.tmp");
        Assert.Equal(Verdict.Allow, Gate.Evaluate(ok, ok.Key, Pol).Verdict);

        void Blocks(GateInput g, FileKey expected, Reason r)
        {
            var (v, rs) = Gate.Evaluate(g, expected, Pol);
            Assert.Equal(Verdict.Block, v);
            Assert.Equal(r, rs[0]);
        }
        Blocks(G(31, @"C:\Users\a\Downloads\x.tmp"), K(99), Reason.IdentityChanged);
        Blocks(G(32, @"C:\Windows\System32\drivers\x.sys"), K(32), Reason.ProtectedPath);
        Blocks(G(33, @"C:\Users\a\link", ItemFlags.Reparse), K(33), Reason.Reparse);
        Blocks(G(34, @"C:\Users\a\pagefile.sys", ItemFlags.System), K(34), Reason.SystemFile);
        Blocks(G(39, @"C:\Users\a\AppData\Roaming\App\state.db", r: RiskClass.High), K(39), Reason.RiskExceeded);
        Blocks(G(40, @"C:\Users\a\temp2.tmp", exists: false), K(40), Reason.Gone);
        Assert.Equal(Verdict.Block, Gate.Evaluate(null, K(1), Pol).Verdict);
    }

    [Fact]
    public void Protected_prefix_does_not_match_a_sibling_folder()
    {
        Assert.True(Pol.IsProtected(@"C:\Windows"));
        Assert.True(Pol.IsProtected(@"C:\windows\x\..\notepad.exe"));
        Assert.False(Pol.IsProtected(@"C:\WindowsOld\a.txt"));
    }

    [Fact]
    public void Two_hundred_thousand_selections_stay_exact()
    {
        const int N = 200_000;
        var s = new SelectionSet();
        var recs = Enumerable.Range(0, N).Select(i => R(1000 + (ulong)i, 100, @"C:\bulk\f.bin")).ToList();
        foreach (var r in recs) s.Add(r.Key, r.Bytes);
        Assert.Equal(N, s.Count);
        Assert.Equal(N * 100L, s.TotalBytes);
        for (var i = 0; i < N; i += 2) s.Remove(recs[i].Key);
        Assert.Equal(N / 2, s.Count);
        Assert.True(s.Contains(K(1001)) && !s.Contains(K(1000)));
        var st = s.ApplyView(recs, null);
        Assert.Equal(N / 2, st.SelectedVisible);
        Assert.Equal(0, st.SelectedHidden);
    }

    [Fact] // report §4.11.3 #5: the commands shown in the review must be the ones the elevated helper runs
    public void Admin_review_commands_match_the_elevated_helper()
    {
        var src = File.ReadAllText(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "CDClean.Elevated", "Program.cs")));
        foreach (var t in CDClean.Core.AdminTasks.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(t.Commands), $"{t.Id} has no commands listed");
            Assert.Contains($"case \"{t.Id}\"", src);
            foreach (var line in t.Commands.Split('\n').Select(l => l.Trim()).Where(l => l.Contains(".exe ") && !l.Contains("oemNN")))
            {
                var exe = line[..(line.IndexOf(".exe ") + 4)];
                var args = line[(exe.Length + 1)..].Trim();
                Assert.True(src.Contains($"\"{exe}\", \"{args}\""), $"{t.Id}: review shows '{line}' but the helper does not run exactly that");
            }
            if (t.Class == CDClean.Core.ConfirmClass.Irreversible) Assert.False(string.IsNullOrEmpty(t.Token));
        }
    }

    [Fact]
    public void Cloud_placeholder_attribute_is_detected()
    {
        var f = FileFacts.Missing("x") with { Exists = true, Attributes = (FileAttributes)0x00400000 };
        Assert.True(f.IsCloudPlaceholder);
        Assert.False((f with { Attributes = FileAttributes.Normal }).IsCloudPlaceholder);
    }
}
