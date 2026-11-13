using CDClean.Core.Safety;

namespace CDClean.Core;

public sealed record CleanProgress(int Done, int Total, string Current);

public sealed class CleanSummary
{
    public string RunId { get; init; } = "";
    public bool DryRun { get; init; }
    public int Files { get; set; }
    public long Bytes { get; set; }
    public int Deleted { get; set; }
    public int Archived { get; set; }
    /// <summary>Items from manual rules that a scheduled run queued for review instead of acting on.</summary>
    public int Queued { get; set; }
    /// <summary>Skipped or failed items, with the reason.</summary>
    public List<string> Failed { get; } = new();
    /// <summary>Measured change in free space across the drives touched (0 for a dry run).</summary>
    public long Freed { get; set; }
    public JobReport? Report { get; set; }
}

/// <summary>Runs rules through the Executor: "delete" rules are caches (deleted), "archive" rules go to the vault.</summary>
public static class Cleaner
{
    public static string BackupPath(string runDir, string original) => Vault.BackupPath(runDir, original);

    public static CleanSummary Run(
        IEnumerable<RuleResult> results,
        Func<Rule, bool> include,
        bool dryRun,
        string backupRoot,
        IProgress<CleanProgress>? progress = null,
        CancellationToken ct = default,
        Policy? policy = null)
    {
        var targets = results.Where(r => include(r.Rule))
            .SelectMany(r => r.Items.Select(i => r.Rule.Action == "delete"
                ? new Target(i.Path, Method.Delete, Category.Cache, r.Rule.Name, IsDirectory: false)
                : new Target(i.Path, Method.Vault, Category.UserFile, r.Rule.Name, IsDirectory: false)))
            .ToList();
        var plan = Executor.Plan(targets, policy ?? Policy.Default(backupRoot), ct);

        if (dryRun)
        {
            var dry = new CleanSummary { RunId = "dry-run", DryRun = true, Files = plan.Items.Count, Bytes = plan.PredictedBytes };
            dry.Failed.AddRange(plan.Items.Where(i => i.Verdict == Verdict.Block)
                .Select(i => $"{i.Facts.Path}: {string.Join(", ", i.Reasons.Select(Gate.Text))}"));
            return dry;
        }

        // Rules are reviewed when they are written, so warnings (recently changed, large) do not stop them. Locked files still do.
        var report = Executor.Execute(plan, new ExecOptions(backupRoot, ProceedOnWarn: true), progress, ct);
        var s = Summarize(report);
        History.Append(backupRoot, s); // partial (cancelled) runs are recorded too
        return s;
    }

    /// <summary>
    /// Scheduled execution (F-23): run Safe-class rules only. Items from manual rules are queued for review,
    /// never deleted or archived. "Run now" uses this same path so the UI cannot bypass the contract.
    /// </summary>
    public static CleanSummary RunScheduled(
        IEnumerable<RuleResult> results,
        Func<Rule, bool> include,
        string backupRoot,
        IProgress<CleanProgress>? progress = null,
        CancellationToken ct = default,
        Policy? policy = null)
    {
        var selected = results.Where(r => include(r.Rule)).ToList();
        var manual = selected.Where(r => !r.Rule.Safe).ToList();
        Automation.Enqueue(manual.SelectMany(r => r.Items.Select(i =>
            new Automation.QueuedItem(r.Rule.Name, i.Path, i.Size, "Manual rule: scheduled runs clean safe rules only."))));

        var s = Run(selected.Where(r => r.Rule.Safe), _ => true, false, backupRoot, progress, ct, policy);
        s.Queued = manual.Sum(r => r.Items.Count);
        return s;
    }

    public static CleanSummary Summarize(JobReport report)
    {
        var s = new CleanSummary
        {
            RunId = report.Id, Files = report.Results.Count, Bytes = report.DoneBytes, Freed = report.Measured, Report = report,
            Deleted = report.Results.Count(r => r.Outcome == Outcome.Done && r.Item.Target.Method != Method.Vault),
            Archived = report.Results.Count(r => r.Outcome == Outcome.Done && r.Item.Target.Method == Method.Vault),
        };
        s.Failed.AddRange(report.Results.Where(r => r.Outcome != Outcome.Done)
            .Select(r => $"{r.Item.Facts.Path}: {r.Error ?? string.Join(", ", r.Reasons.Select(Gate.Text))}"));
        return s;
    }
}

public sealed record HistoryRow(string RunId, int Files, long Bytes, int Deleted, int Archived, int Failed);

/// <summary>history.csv, same format as the Rust version, so old runs show up.</summary>
public static class History
{
    public static void Append(string root, CleanSummary s)
    {
        Directory.CreateDirectory(root);
        var file = Path.Combine(root, "history.csv");
        var isNew = !File.Exists(file);
        using var w = new StreamWriter(file, append: true);
        if (isNew) w.WriteLine("run_id,files,bytes,deleted,archived,failed");
        w.WriteLine($"{s.RunId},{s.Files},{s.Bytes},{s.Deleted},{s.Archived},{s.Failed.Count}");
    }

    public static List<HistoryRow> Read(string root)
    {
        var file = Path.Combine(root, "history.csv");
        var rows = new List<HistoryRow>();
        if (!File.Exists(file)) return rows;
        foreach (var line in File.ReadLines(file).Skip(1))
        {
            var p = line.Split(',');
            if (p.Length >= 6 && int.TryParse(p[1], out var files) && long.TryParse(p[2], out var bytes)
                && int.TryParse(p[3], out var del) && int.TryParse(p[4], out var arc) && int.TryParse(p[5], out var fail))
                rows.Add(new HistoryRow(p[0], files, bytes, del, arc, fail));
        }
        return rows;
    }
}
