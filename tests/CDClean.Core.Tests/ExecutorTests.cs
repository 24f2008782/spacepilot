using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using CDClean.Core;
using CDClean.Core.Native;
using CDClean.Core.Safety;
using Xunit;

/// <summary>
/// Builds throwaway trees on E: (never in real folders) and runs the delete pipeline against them.
/// Test IDs refer to playbook-v2.md §11 (DE = deleting, SC = scanning).
/// </summary>
public sealed class ExecutorTests : IDisposable
{
    private static readonly string Base = Directory.Exists(@"E:\") ? @"E:\Temp\cdclean-tests" : Path.Combine(Path.GetTempPath(), "cdclean-tests");
    private readonly string _root = Path.Combine(Base, Guid.NewGuid().ToString("N"));
    private string Vault => Path.Combine(_root, "vault");
    private string Data => Path.Combine(_root, "data");

    static ExecutorTests() => Audit.Dir = Path.Combine(Base, "audit");

    public ExecutorTests() => Directory.CreateDirectory(Data);

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

    private string Write(string rel, string text = "hello")
    {
        var p = Path.Combine(Data, rel);
        Directory.CreateDirectory(Path.GetDirectoryName(p)!);
        File.WriteAllText(p, text);
        return p;
    }

    private Policy Pol(params string[] extra) => Policy.Default(Vault, extra) with { MaxRisk = RiskClass.Review };
    private JobPlan Plan(Method m, Category c, params string[] paths) => Executor.Plan(paths.Select(p => new Target(p, m, c, "test")), Pol());
    private JobReport Run(JobPlan p, bool warn = true, CancellationToken ct = default) => Executor.Execute(p, new ExecOptions(Vault, warn), null, ct);

    [Fact] // identity survives rename; differs for another file
    public void File_identity_is_stable()
    {
        var a = Write("a.txt");
        var k = Files.Read(a).Key;
        var b = Path.Combine(Data, "b.txt");
        File.Move(a, b);
        Assert.Equal(k, Files.Read(b).Key);
        Assert.NotEqual(k, Files.Read(Write("c.txt")).Key);
        Assert.False(Files.Read(Path.Combine(Data, "nope")).Exists);
    }

    [Fact] // DE-02 + unicode names + alternate data stream + long path: vault, then restore byte-identical
    public void Vault_then_restore_is_hash_identical()
    {
        var deep = string.Join('\\', Enumerable.Repeat("d", 140)); // path well over 260 characters
        var files = new[] { Write("set\\emoji 😀.txt", "x1"), Write("set\\中文 עברית.txt", "x2"), Write("set\\" + deep + "\\deep.txt", "x3") };
        File.WriteAllText(files[0] + ":extra", "stream");
        var hashes = files.Select(CDClean.Core.Vault.Sha256).ToList();

        var report = Run(Plan(Method.Vault, Category.UserFile, Path.Combine(Data, "set")));
        Assert.Equal(3, report.Done);
        Assert.All(files, f => Assert.False(File.Exists(f)));
        Assert.False(Directory.Exists(Path.Combine(Data, "set")), "emptied folders are removed");
        Assert.Contains("SHA-256", File.ReadAllText(Path.Combine(report.VaultDir!, "verify.txt")));

        var (ok, skipped) = Backups.Restore(Vault, report.Id);
        Assert.Equal((3, 0), (ok, skipped));
        Assert.Equal(hashes, files.Select(CDClean.Core.Vault.Sha256).ToList());
    }

    [Fact] // vault scenario 3: restore never overwrites a file that came back, and refuses a damaged backup
    public void Restore_skips_existing_and_rejects_damaged_backup()
    {
        var a = Write("r3\\a.txt", "aaa");
        var b = Write("r3\\b.txt", "bbb");
        var report = Run(Plan(Method.Vault, Category.UserFile, a, b));
        Assert.Equal(2, report.Done);

        File.WriteAllText(a, "user recreated this"); // must survive the restore
        var entryB = File.ReadLines(Path.Combine(report.VaultDir!, "manifest.jsonl"))
            .Select(l => System.Text.Json.JsonSerializer.Deserialize<ManifestEntry>(l)!).Single(e => e.Original == b);
        File.WriteAllText(entryB.Backup, "corrupted");

        Assert.Throws<IOException>(() => Backups.Restore(Vault, report.Id));
        Assert.Equal("user recreated this", File.ReadAllText(a));
        Assert.False(File.Exists(b), "a damaged backup is not restored");
    }

    [Fact] // DE-04
    public void File_replaced_after_planning_is_skipped()
    {
        var f = Write("swap.txt", "original");
        var plan = Plan(Method.Delete, Category.Cache, f);
        File.Delete(f);
        File.WriteAllText(f, "a different file at the same path");
        var r = Run(plan);
        Assert.Equal(Outcome.Skipped, r.Results.Single().Outcome);
        Assert.Contains(Reason.IdentityChanged, r.Results.Single().Reasons);
        Assert.True(File.Exists(f));
    }

    [Fact] // DE-05
    public void File_gone_before_run_is_skipped()
    {
        var f = Write("gone.txt");
        var plan = Plan(Method.Delete, Category.Cache, f);
        File.Delete(f);
        var r = Run(plan);
        Assert.Equal(Reason.Gone, r.Results.Single().Reasons[0]);
    }

    [Fact] // DE-07: blocked at plan time and again at run time
    public void Protected_paths_are_never_touched()
    {
        var mine = Write("keep\\precious.txt");
        var pol = Pol(Path.Combine(Data, "keep"));
        var plan = Executor.Plan(new[] { new Target(mine, Method.Delete, Category.Cache, "t") }, pol);
        Assert.Equal(Verdict.Block, plan.Items.Single().Verdict);
        Assert.Contains(Reason.ProtectedPath, plan.Items.Single().Reasons);
        Run(plan);
        Assert.True(File.Exists(mine));

        var win = Executor.Plan(new[] { new Target(@"C:\Windows\win.ini", Method.Delete, Category.Cache, "t") }, Pol());
        Assert.Equal(Verdict.Block, win.Items.Single().Verdict);
    }

    [Fact] // DE-09 / SC-03: junctions are not followed and not deleted through
    public void Junction_target_is_untouched()
    {
        var target = Write("..\\outside\\important.txt");
        var link = Path.Combine(Data, "tree\\link");
        Write("tree\\cache.bin");
        Directory.CreateDirectory(Path.GetDirectoryName(link)!);
        Process.Start(new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{link}\" \"{Path.GetDirectoryName(target)}\"") { CreateNoWindow = true, UseShellExecute = false })!.WaitForExit();
        Assert.True(Directory.Exists(link));

        var plan = Plan(Method.Delete, Category.Cache, Path.Combine(Data, "tree"), link);
        Assert.DoesNotContain(plan.Items, i => i.Facts.Path.Contains("important"));
        Assert.Contains(plan.Items, i => i.Reasons.Contains(Reason.Reparse));
        Run(plan);
        Assert.True(File.Exists(target));
    }

    [Fact] // DE-08: hard-linked files are flagged and not removed by default
    public void Hard_link_is_flagged()
    {
        var a = Write("hl\\a.bin", "shared data");
        Assert.True(CreateHardLinkW(Path.Combine(Data, "hl\\b.bin"), a, IntPtr.Zero));
        var item = Plan(Method.Delete, Category.Cache, a).Items.Single();
        Assert.Equal(2u, item.Facts.Links);
        Assert.Contains(Reason.HardLink, item.Reasons);
        Assert.Equal(RiskClass.High, item.Risk);
    }

    [Fact] // DE-06
    public void Locked_file_is_skipped_with_reason()
    {
        var f = Write("locked.bin");
        using var hold = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.None);
        var r = Run(Plan(Method.Delete, Category.Cache, f));
        Assert.Equal(Outcome.Skipped, r.Results.Single().Outcome);
        Assert.Contains(Reason.Locked, r.Results.Single().Reasons);
        Assert.True(File.Exists(f));
    }

    [Fact] // DE-10-style: a file we are not allowed to delete is reported as failed, never silently dropped
    public void Access_denied_delete_is_reported()
    {
        var f = Write("denied\\x.bin");
        var me = WindowsIdentity.GetCurrent().User!;
        var fi = new FileInfo(f);
        var di = fi.Directory!;
        var fRule = new FileSystemAccessRule(me, FileSystemRights.Delete, AccessControlType.Deny);
        var dRule = new FileSystemAccessRule(me, FileSystemRights.DeleteSubdirectoriesAndFiles, AccessControlType.Deny);
        var fAcl = fi.GetAccessControl(); fAcl.AddAccessRule(fRule); fi.SetAccessControl(fAcl);
        var dAcl = di.GetAccessControl(); dAcl.AddAccessRule(dRule); di.SetAccessControl(dAcl);
        try
        {
            var r = Run(Plan(Method.Delete, Category.Cache, f)).Results.Single();
            Assert.Equal(Outcome.Failed, r.Outcome);
            Assert.False(string.IsNullOrEmpty(r.Error));
        }
        finally
        {
            dAcl.RemoveAccessRule(dRule); di.SetAccessControl(dAcl);
            fAcl.RemoveAccessRule(fRule); fi.SetAccessControl(fAcl);
        }
        Assert.True(File.Exists(f));
    }

    [Fact] // DE-14 + measured free space: every item has one audit line, job line has predicted and measured
    public void Delete_writes_one_audit_line_per_item()
    {
        var files = Enumerable.Range(0, 20).Select(i => Write($"cache\\{i}.tmp", new string('x', 10000))).ToList();
        var r = Run(Plan(Method.Delete, Category.Cache, Path.Combine(Data, "cache")));
        Assert.Equal(20, r.Done);
        Assert.All(files, f => Assert.False(File.Exists(f)));
        var lines = Audit.Read(r.Id).ToList();
        Assert.Equal(20, lines.Count(l => l.Kind == "item" && l.Outcome == "Done"));
        var job = lines.Single(l => l.Kind == "job");
        Assert.NotNull(job.Measured);
        Assert.Contains(Path.GetPathRoot(Data)!, r.FreedByDrive.Keys);
    }

    [Fact] // DE-11: a cancelled job deletes nothing it did not report
    public void Cancelled_job_is_consistent()
    {
        var files = Enumerable.Range(0, 10).Select(i => Write($"c\\{i}.tmp")).ToList();
        var plan = Plan(Method.Delete, Category.Cache, Path.Combine(Data, "c"));
        var r = Run(plan, ct: new CancellationToken(true));
        Assert.True(r.Cancelled);
        Assert.Equal(files.Count(f => !File.Exists(f)), r.Done);
    }

    [Fact] // warnings stop the job unless the caller accepted them
    public void Warnings_need_consent()
    {
        var f = Write("recent.tmp"); // just written, so "changed recently"
        var plan = Plan(Method.Delete, Category.Cache, f);
        Assert.Equal(Verdict.Warn, plan.Items.Single().Verdict);
        Assert.Equal(Outcome.Skipped, Run(plan, warn: false).Results.Single().Outcome);
        Assert.Equal(Outcome.Done, Run(Plan(Method.Delete, Category.Cache, f), warn: true).Results.Single().Outcome);
    }

    [Fact] // DE-01
    public void Recycle_moves_file_out()
    {
        var f = Write("bin\\r.tmp");
        var r = Run(Plan(Method.Recycle, Category.Cache, f));
        Assert.Equal(Outcome.Done, r.Results.Single().Outcome);
        Assert.False(File.Exists(f));
    }

    [Fact] // SC-05-style: compressed files report allocated size below logical size
    public void Compressed_file_allocated_size_is_smaller()
    {
        var f = Write("big.txt", new string('a', 1 << 20));
        Process.Start(new ProcessStartInfo("compact.exe", $"/c \"{f}\"") { CreateNoWindow = true, UseShellExecute = false })!.WaitForExit();
        var facts = Files.Read(f);
        Assert.Equal(1 << 20, facts.Logical);
        Assert.True(facts.Allocated < facts.Logical, $"allocated {facts.Allocated} should be below {facts.Logical}");
    }

    [Fact]
    public void Same_drive_vault_is_called_out()
    {
        var plan = Plan(Method.Vault, Category.UserFile, Write("v.txt"));
        Assert.Contains("frees no space", plan.Describe(Vault));
    }

    [Fact]
    public void Vault_cap_removes_oldest_runs_first()
    {
        foreach (var id in new[] { "2026-01-01_000000", "2026-02-01_000000", "2026-03-01_000000" })
        {
            Directory.CreateDirectory(Path.Combine(Vault, id));
            File.WriteAllText(Path.Combine(Vault, id, "manifest.jsonl"), $"{{\"Original\":\"x\",\"Backup\":\"y\",\"Size\":100,\"Rule\":\"r\"}}\n");
        }
        var removed = Backups.PruneToCap(Vault, 150, keep: "2026-01-01_000000", dryRun: false);
        Assert.Equal(new[] { "2026-02-01_000000", "2026-03-01_000000" }, removed);
    }

    [Fact]
    public void Risk_classes()
    {
        var old = Write("old.tmp");
        File.SetLastWriteTimeUtc(old, DateTime.UtcNow.AddYears(-1));
        var f = Files.Read(old);
        Assert.Equal(RiskClass.Safe, Risk.Classify(f, Category.Cache, false, Pol()));
        Assert.Equal(RiskClass.Review, Risk.Classify(f, Category.UserFile, false, Pol()));
        Assert.Equal(RiskClass.High, Risk.Classify(f, Category.Cache, true, Pol()));
        Assert.Equal(RiskClass.Protected, Risk.Classify(Files.Read(@"C:\Windows\win.ini"), Category.Cache, false, Pol()));
    }

    [Fact] // batch 1.3: drive list includes the system drive with sizes
    public void Volumes_lists_the_system_drive()
    {
        var sys = Volumes.List().Single(v => v.IsSystem);
        Assert.True(sys.Total > 0 && sys.Free > 0 && sys.ClusterBytes > 0);
        Assert.Equal("NTFS", sys.FileSystem);
        Assert.True(sys.HardLinks);
        Assert.False(Volumes.Available(@"Q:\does-not-exist"));
    }

    [Fact] // F-17 lock inspector: names the process holding a file
    public void Lock_holder_is_named()
    {
        var f = Write("held.bin");
        using (new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.None))
            Assert.Contains(Locks.Holders(f), h => h.Contains($"process {Environment.ProcessId}"));
        Assert.Empty(Locks.Holders(f));
    }

    [Fact] // Leftovers: AppData/ProgramData always; Program Files only for chosen drives
    public void Leftover_roots_follow_the_chosen_drives()
    {
        var sysOnly = Leftovers.Roots(null);
        Assert.Equal(3, sysOnly.Count);
        var withE = Leftovers.Roots(new[] { @"E:\" });
        Assert.Contains(@"E:\Program Files", withE);
        Assert.Contains(@"E:\Program Files (x86)", withE);
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateHardLinkW(string newLink, string existing, IntPtr sa);
}
