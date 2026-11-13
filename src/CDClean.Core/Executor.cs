using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using CDClean.Core.Native;
using CDClean.Core.Safety;

namespace CDClean.Core;

public enum Method { Delete, Vault, Recycle }
public enum Outcome { Done, Skipped, Failed }

/// <summary>Something the user (or a rule) asked to remove. A folder is expanded into its files.</summary>
/// <summary>Hidden = selected but hidden by the table's search; left out unless the user includes them.
/// IsDirectory = false when the caller already knows this is a file, so planning can skip a Directory.Exists call per item.</summary>
public sealed record Target(string Path, Method Method, Category Category, string Source, bool Hidden = false, bool? IsDirectory = null);

public sealed record PlanItem(Target Target, FileFacts Facts, bool Locked, RiskClass Risk, Verdict Verdict, IReadOnlyList<Reason> Reasons);

public sealed record ItemResult(PlanItem Item, Outcome Outcome, IReadOnlyList<Reason> Reasons, string? Error = null);

public sealed record ExecOptions(string VaultRoot, bool ProceedOnWarn = false, int Workers = 4, long VaultCapBytes = 0);

/// <summary>A frozen job: every item, its identity and the verdict at plan time.</summary>
public sealed class JobPlan
{
    public required IReadOnlyList<PlanItem> Items { get; init; }
    public required IReadOnlyList<string> Roots { get; init; }
    public required Policy Policy { get; init; }

    public IEnumerable<PlanItem> Actionable => Items.Where(i => i.Verdict != Verdict.Block);

    /// <summary>The same plan with only the items the user kept in the review dialog.</summary>
    public JobPlan Keep(Func<PlanItem, bool> keep) => new() { Items = Items.Where(keep).ToList(), Roots = Roots, Policy = Policy };
    public long PredictedBytes => Actionable.Sum(i => i.Facts.Allocated);
    public int Blocked => Items.Count(i => i.Verdict == Verdict.Block);
    public int Warned => Items.Count(i => i.Verdict == Verdict.Warn);

    /// <summary>Plain-language summary for the confirmation dialog.</summary>
    public string Describe(string vaultRoot)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{Items.Count - Blocked:N0} files, {Fmt(PredictedBytes)} on disk will be acted on.");
        foreach (var m in Actionable.GroupBy(i => i.Target.Method))
            sb.AppendLine(m.Key switch
            {
                Method.Vault => $"• {m.Count():N0} copied to {vaultRoot}, checked, then removed. Restore from Backups."
                    + (m.Any(i => Vault.SameDrive(vaultRoot, i.Facts.Path)) ? " The backup is on the same drive, so this frees no space there until the backup is deleted." : ""),
                Method.Recycle => $"• {m.Count():N0} sent to the Recycle Bin. Space is freed only when the Recycle Bin is emptied.",
                _ => $"• {m.Count():N0} deleted permanently (caches that rebuild themselves).",
            });
        if (Warned > 0)
            sb.AppendLine($"\n{Warned:N0} with warnings: " + Group(Items.Where(i => i.Verdict == Verdict.Warn)));
        if (Blocked > 0)
            sb.AppendLine($"\n{Blocked:N0} will NOT be touched: " + Group(Items.Where(i => i.Verdict == Verdict.Block)));
        sb.Append("\nUsage is judged from file dates, which cannot prove a file is unused.");
        return sb.ToString();
    }

    private static string Group(IEnumerable<PlanItem> items) => string.Join("; ",
        items.SelectMany(i => i.Reasons).GroupBy(r => r).OrderByDescending(g => g.Count()).Select(g => $"{g.Count():N0} {Gate.Text(g.Key)}"));

    internal static string Fmt(long b) => b >= 1L << 30 ? $"{b / 1073741824.0:0.00} GB" : b >= 1L << 20 ? $"{b / 1048576.0:0.0} MB" : $"{b / 1024.0:0} KB";
}

public sealed class JobReport
{
    public required string Id { get; init; }
    public required IReadOnlyList<ItemResult> Results { get; init; }
    public bool Cancelled { get; init; }
    public long PredictedBytes { get; init; }
    /// <summary>Free space after minus before, per drive root. Measured, not estimated.</summary>
    public required IReadOnlyDictionary<string, long> FreedByDrive { get; init; }
    public string? VaultDir { get; init; }

    public int Done => Results.Count(r => r.Outcome == Outcome.Done);
    public int Skipped => Results.Count(r => r.Outcome == Outcome.Skipped);
    public int Failed => Results.Count(r => r.Outcome == Outcome.Failed);
    public long DoneBytes => Results.Where(r => r.Outcome == Outcome.Done).Sum(r => r.Item.Facts.Allocated);
    public long Measured => FreedByDrive.Values.Sum();

    public string Describe() =>
        $"{Done:N0} done, {Skipped:N0} skipped, {Failed:N0} failed{(Cancelled ? " (cancelled)" : "")}. " +
        $"Measured free space change: {string.Join(", ", FreedByDrive.Select(d => $"{d.Key} {(d.Value >= 0 ? "+" : "-")}{JobPlan.Fmt(Math.Abs(d.Value))}"))}.";
}

/// <summary>
/// The one way anything is removed: plan → re-check → classify → stage → run → verify → record (report §4.5.3).
/// </summary>
public static class Executor
{
    public static JobPlan Plan(IEnumerable<Target> targets, Policy pol, CancellationToken ct = default, bool resolveFinalPath = true)
    {
        var roots = new List<string>();
        var expanded = new List<Target>();
        foreach (var t in targets)
        {
            if (t.IsDirectory != false && Directory.Exists(t.Path) && !IsLink(t.Path))
            {
                roots.Add(t.Path);
                expanded.AddRange(Scanner.Files(t.Path).Select(f => t with { Path = f }));
            }
            else expanded.Add(t);
        }
        var items = expanded.AsParallel().AsOrdered().WithCancellation(ct).Select(t => Check(t, null, pol, resolveFinalPath)).ToList();
        return new JobPlan { Items = items, Roots = roots, Policy = pol };
    }

    /// <summary>Reads the file now and runs the gate. expected = identity recorded at plan time (null when planning).</summary>
    private static PlanItem Check(Target t, FileKey? expected, Policy pol, bool resolveFinalPath = true)
    {
        FileFacts f;
        bool locked;
        try
        {
            // One handle covers facts and deletability; it falls back to a read-only handle when locked.
            (f, locked) = Files.ReadWithLock(t.Path, resolveFinalPath);
        }
        catch { return new PlanItem(t, FileFacts.Missing(t.Path), false, RiskClass.High, Verdict.Block, new[] { Reason.Unreadable }); }

        var protectedPath = f.Exists && pol.IsProtected(f.FinalPath);
        var risk = f.Exists ? Risk.Classify(f, t.Category, locked, pol, protectedPath) : RiskClass.High;
        var (v, rs) = Gate.Evaluate(GateInput.From(f, locked, risk, pol.RecentDays, protectedPath), expected ?? f.Key, pol);
        return new PlanItem(t, f, locked, risk, v, rs);
    }

    private static bool IsLink(string path) => (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

    public static JobReport Execute(JobPlan plan, ExecOptions o, IProgress<CleanProgress>? progress = null, CancellationToken ct = default)
    {
        var results = new ConcurrentBag<ItemResult>();
        var drives = plan.Items.Select(i => Path.GetPathRoot(i.Facts.Path)!)
            .Append(Path.GetPathRoot(Path.GetFullPath(o.VaultRoot))!)
            .Where(d => d.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var before = drives.ToDictionary(d => d, Free);

        string? runDir = null;
        StreamWriter? manifest = null;
        var manifestLock = new object();
        if (plan.Actionable.Any(i => i.Target.Method == Method.Vault))
        {
            runDir = Vault.NewJob(o.VaultRoot);
            manifest = new StreamWriter(Path.Combine(runDir, "manifest.jsonl"), append: false);
        }
        var jobId = runDir != null ? Path.GetFileName(runDir) : "job-" + DateTime.Now.ToString("yyyy-MM-dd_HHmmss_fff");
        var toRecycle = new ConcurrentBag<ItemResult>();
        int done = 0, total = plan.Items.Count;
        long lastReport = 0;
        var cancelled = false;

        void Record(ItemResult r)
        {
            results.Add(r);
            Audit.Write(new AuditLine(DateTime.Now, jobId, "item", r.Item.Facts.Path, r.Item.Target.Method.ToString(), r.Outcome.ToString(),
                r.Item.Verdict.ToString(), r.Reasons.Select(x => x.ToString()).ToArray(), r.Item.Facts.Allocated, r.Error));
            var n = Interlocked.Increment(ref done);
            var now = Environment.TickCount64;
            if (now - Interlocked.Read(ref lastReport) > 250) { Interlocked.Exchange(ref lastReport, now); progress?.Report(new CleanProgress(n, total, r.Item.Facts.Path)); }
        }

        try
        {
            var opts = new ParallelOptions { CancellationToken = ct, MaxDegreeOfParallelism = Math.Max(1, o.Workers) };
            Parallel.ForEach(plan.Items, opts, item =>
            {
                if (item.Verdict == Verdict.Block) { Record(new ItemResult(item, Outcome.Skipped, item.Reasons)); return; }

                // Re-check right before acting: same identity, still not protected, not newly locked.
                var now = Check(item.Target, item.Facts.Key, plan.Policy);
                if (now.Verdict == Verdict.Block || (now.Verdict == Verdict.Warn && (!o.ProceedOnWarn || now.Locked)))
                { Record(new ItemResult(now, Outcome.Skipped, now.Reasons)); return; }

                try
                {
                    switch (item.Target.Method)
                    {
                        case Method.Recycle:
                            toRecycle.Add(new ItemResult(now, Outcome.Done, now.Reasons));
                            return; // recorded after the batch call below
                        case Method.Vault:
                            var entry = Vault.Stage(runDir!, now.Facts, item.Target.Source);
                            lock (manifestLock) { manifest!.WriteLine(JsonSerializer.Serialize(entry)); manifest.Flush(); } // record before removal
                            DeleteFile(now.Facts.Path);
                            break;
                        default:
                            DeleteFile(now.Facts.Path);
                            break;
                    }
                    Record(new ItemResult(now, Outcome.Done, now.Reasons));
                }
                catch (Exception e) { Record(new ItemResult(now, Outcome.Failed, now.Reasons, e.Message)); }
            });
        }
        catch (OperationCanceledException) { cancelled = true; }
        finally
        {
            manifest?.Dispose();
        }

        // Recycle in batches: one shell call per 500 files. If a batch fails, check each file to see what happened.
        foreach (var batch in toRecycle.Chunk(500))
        {
            Shell.Recycle(batch.Select(b => b.Item.Facts.Path).ToList());
            foreach (var r in batch)
                Record(File.Exists(r.Item.Facts.Path) ? r with { Outcome = Outcome.Failed, Error = "Recycle Bin refused the file" } : r);
        }

        if (!cancelled) RemoveEmptyFolders(plan);

        string? verify = null;
        if (runDir != null)
        {
            var staged = results.Count(r => r.Outcome == Outcome.Done && r.Item.Target.Method == Method.Vault);
            verify = $"Job {jobId}\r\nFinished {DateTime.Now:yyyy-MM-dd HH:mm:ss}{(cancelled ? " (cancelled)" : "")}\r\n" +
                     $"{staged} files staged; each copy's SHA-256 matched the original before the original was removed.\r\n" +
                     $"{results.Count(r => r.Outcome == Outcome.Failed && r.Item.Target.Method == Method.Vault)} failed, originals left in place.\r\n";
            File.WriteAllText(Path.Combine(runDir, "verify.txt"), verify);
            if (o.VaultCapBytes > 0) Backups.PruneToCap(o.VaultRoot, o.VaultCapBytes, jobId, dryRun: false);
        }

        var freed = drives.ToDictionary(d => d, d => Free(d) - before[d]);
        progress?.Report(new CleanProgress(done, total, ""));
        var report = new JobReport
        {
            Id = jobId, Results = results.ToList(), Cancelled = cancelled, PredictedBytes = plan.PredictedBytes,
            FreedByDrive = freed, VaultDir = runDir,
        };
        Audit.Write(new AuditLine(DateTime.Now, jobId, "job", "", "", cancelled ? "Cancelled" : "Finished", "", Array.Empty<string>(),
            report.DoneBytes, report.Describe(), plan.PredictedBytes, report.Measured));
        return report;
    }

    private static long Free(string root)
    {
        try { return new DriveInfo(root).TotalFreeSpace; } catch { return 0; }
    }

    private static void DeleteFile(string path)
    {
        var a = File.GetAttributes(path);
        if ((a & FileAttributes.ReadOnly) != 0) File.SetAttributes(path, a & ~FileAttributes.ReadOnly);
        File.Delete(path);
    }

    /// <summary>After files are removed from a chosen folder, removes folders left empty. Never follows links, never touches protected folders.</summary>
    private static void RemoveEmptyFolders(JobPlan plan)
    {
        foreach (var root in plan.Roots)
        {
            if (plan.Policy.IsProtected(root) || !Directory.Exists(root)) continue;
            var dirs = Directory.EnumerateDirectories(root, "*", new EnumerationOptions
            { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint })
                .OrderByDescending(d => d.Length).Append(root);
            foreach (var d in dirs)
                try { if (!Directory.EnumerateFileSystemEntries(d).Any()) Directory.Delete(d); } catch { /* in use or not empty: leave it */ }
        }
    }
}
