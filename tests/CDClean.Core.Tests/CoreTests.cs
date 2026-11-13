using System.Security.Cryptography;
using CDClean.Core;
using Xunit;

public class CoreTests : IDisposable
{
    private readonly string _tmp = Path.Combine(Path.GetTempPath(), "cdclean-test-" + Guid.NewGuid().ToString("N"));
    public CoreTests() => Directory.CreateDirectory(_tmp);
    public void Dispose() { try { Directory.Delete(_tmp, true); } catch { } }

    [Fact]
    public void Parses_rules_and_rejects_bad_action()
    {
        var rules = Rules.Parse("[[rule]]\nname = \"a\"\npath = 'C:\\x'\ninclude = [\"Cache\"]\ndepth = 2\nolder_than_days = 3\nsafe = false\n");
        Assert.Single(rules);
        Assert.Equal("delete", rules[0].Action);
        Assert.Equal(2, rules[0].Depth);
        Assert.Equal(3, rules[0].OlderThanDays);
        Assert.False(rules[0].Safe);
        Assert.Throws<FormatException>(() => Rules.Parse("[[rule]]\nname = \"a\"\npath = 'C:\\x'\naction = \"nuke\"\n"));
    }

    [Fact]
    public void Normalizes_CR_only_and_CRLF_line_endings()
    {
        Assert.Equal("a\nb\nc", Rules.NormalizeNewlines("a\r\nb\rc"));
        var rules = Rules.Parse("[[rule]]\rname = \"a\"\rpath = 'C:\\x'\r");
        Assert.Single(rules);
        Assert.Equal("a", rules[0].Name);
    }

    [Fact]
    public void Shipped_rules_file_parses()
    {
        var file = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "rules.toml"));
        Assert.NotEmpty(Rules.Load(file));
    }

    [Fact]
    public void Protects_browser_login_files_and_claude_data()
    {
        Assert.True(Protected.IsProtected(@"C:\x\Default\Cookies"));
        Assert.True(Protected.IsProtected(@"C:\x\Default\Login Data"));
        Assert.True(Protected.IsProtected(@"C:\Users\TestUser\AppData\Local\Claude-Data\a.txt"));
        Assert.False(Protected.IsProtected(@"C:\x\Cache\data_1"));
    }

    [Fact]
    public void Archive_then_restore_is_byte_identical()
    {
        var src = Path.Combine(_tmp, "src", "sub");
        Directory.CreateDirectory(src);
        var file = Path.Combine(src, "b.bin");
        File.WriteAllBytes(file, RandomNumberGenerator.GetBytes(300_000));
        var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));
        var backups = Path.Combine(_tmp, "backups");

        var rule = new Rule("t", Path.Combine(_tmp, "src"), Array.Empty<string>(), null, null, "archive", true);
        var summary = Cleaner.Run(new[] { Scanner.Scan(rule) }, _ => true, false, backups);
        Assert.Equal(1, summary.Archived);
        Assert.False(File.Exists(file));

        var run = Assert.Single(Backups.List(backups));
        var (restored, skipped) = Backups.Restore(backups, run.Id);
        Assert.Equal((1, 0), (restored, skipped));
        Assert.Equal(hash, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))));
        Assert.True(Backups.List(backups)[0].Restored);
        Assert.Single(History.Read(backups));
    }

    [Fact]
    public void Dry_run_changes_nothing()
    {
        var f = Path.Combine(_tmp, "keep.txt");
        File.WriteAllText(f, "x");
        var rule = new Rule("t", _tmp, Array.Empty<string>(), null, null, "delete", true);
        var s = Cleaner.Run(new[] { Scanner.Scan(rule) }, _ => true, true, Path.Combine(_tmp, "bk"));
        Assert.Equal(1, s.Files);
        Assert.True(File.Exists(f));
    }

    [Fact]
    public void Age_filter_skips_recent_files()
    {
        var old = Path.Combine(_tmp, "old.txt");
        var recent = Path.Combine(_tmp, "new.txt");
        File.WriteAllText(old, "o");
        File.WriteAllText(recent, "n");
        File.SetLastWriteTime(old, DateTime.Now.AddDays(-10));
        var rule = new Rule("t", _tmp, Array.Empty<string>(), null, 5, "delete", true);
        var items = Scanner.Scan(rule).Items;
        Assert.Single(items);
        Assert.Equal(old, items[0].Path);
    }

    [Fact]
    public void Include_rule_only_takes_named_folders()
    {
        Directory.CreateDirectory(Path.Combine(_tmp, "Default", "Cache"));
        File.WriteAllText(Path.Combine(_tmp, "Default", "Cache", "c1"), "c");
        File.WriteAllText(Path.Combine(_tmp, "Default", "Cookies"), "secret");
        var rule = new Rule("t", _tmp, new[] { "Cache" }, 2, null, "delete", true);
        var r = Scanner.Scan(rule);
        Assert.Single(r.Items);
        Assert.EndsWith("c1", r.Items[0].Path);
    }

    [Fact]
    public void Prune_removes_only_old_runs()
    {
        var root = Path.Combine(_tmp, "bk");
        var old = DateTime.Today.AddDays(-40).ToString("yyyy-MM-dd") + "_100000";
        var fresh = DateTime.Today.ToString("yyyy-MM-dd") + "_100000";
        Directory.CreateDirectory(Path.Combine(root, old));
        Directory.CreateDirectory(Path.Combine(root, fresh));
        Directory.CreateDirectory(Path.Combine(root, "not-a-run"));
        Assert.Equal(new[] { old }, Backups.Prune(root, 30, dryRun: false));
        Assert.True(Directory.Exists(Path.Combine(root, fresh)));
        Assert.True(Directory.Exists(Path.Combine(root, "not-a-run")));
        Assert.Throws<ArgumentException>(() => Backups.Delete(root, "..\\x"));
    }

    [Fact]
    public void Leftover_matching_ignores_punctuation_and_updater()
    {
        var apps = new[] { Leftovers.Norm("OpenWhispr 1.11.0") };
        Assert.True(Leftovers.Matches("open-whispr-updater", apps));
        Assert.False(Leftovers.Matches("SomethingElse", apps));
    }

    [Fact]
    public void Organizer_plans_moves_and_undo_restores()
    {
        File.WriteAllText(Path.Combine(_tmp, "a.jpg"), "1");
        File.WriteAllText(Path.Combine(_tmp, "b.pdf"), "2");
        File.WriteAllText(Path.Combine(_tmp, "c.weird"), "3");
        var plan = Organizer.Plan(_tmp);
        Assert.Contains(plan, m => m.Category == "Images");
        Assert.Contains(plan, m => m.Category == "Documents");
        Assert.Contains(plan, m => m.Category == "Other");
        var logs = Path.Combine(_tmp, "logs");
        var (moved, failed) = Organizer.Apply(plan, logs);
        Assert.Equal(3, moved);
        Assert.Empty(failed);
        Assert.True(File.Exists(Path.Combine(_tmp, "Images", "a.jpg")));
        Assert.Equal(3, Organizer.Undo(Organizer.LatestLog(logs)!));
        Assert.True(File.Exists(Path.Combine(_tmp, "a.jpg")));
    }

    [Fact]
    public void Duplicates_found_by_content()
    {
        File.WriteAllText(Path.Combine(_tmp, "x1.txt"), "same content here");
        File.WriteAllText(Path.Combine(_tmp, "x2.txt"), "same content here");
        File.WriteAllText(Path.Combine(_tmp, "y.txt"), "diff content here");
        var g = Assert.Single(Duplicates.Find(_tmp, 1));
        Assert.Equal(2, g.Paths.Count);
    }
}
