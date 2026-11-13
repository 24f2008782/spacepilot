using System.IO.Enumeration;

namespace CDClean.Core;

public sealed record FileItem(string Path, long Size);

public sealed class RuleResult
{
    public required Rule Rule { get; init; }
    public List<FileItem> Items { get; } = new();
    public int ProtectedSkipped { get; set; }
    public long Bytes => Items.Sum(i => i.Size);
}

public static class Scanner
{
    // Reparse points (junctions, symlinks) are skipped so a scan can never leave the rule's folder.
    private static EnumerationOptions Opts(bool recurse, int maxDepth = int.MaxValue) => new()
    {
        RecurseSubdirectories = recurse,
        MaxRecursionDepth = maxDepth,
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.ReparsePoint,
    };

    public static IEnumerable<string> Files(string dir)
    {
        try { return Directory.EnumerateFiles(dir, "*", Opts(true)); }
        catch { return Array.Empty<string>(); }
    }

    private sealed record ScanFile(string Path, long Size, DateTime LastWriteUtc);

    /// <summary>Reads name, size and date from the directory entry in one pass, without a second stat per file.</summary>
    private static List<ScanFile> FilesWithSize(string dir, CancellationToken ct)
    {
        var list = new List<ScanFile>();
        try
        {
            var opts = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint,
            };
            var files = new FileSystemEnumerable<ScanFile>(dir,
                (ref FileSystemEntry entry) => new ScanFile(entry.ToFullPath(), entry.Length, entry.LastWriteTimeUtc.UtcDateTime),
                opts)
            {
                ShouldIncludePredicate = (ref FileSystemEntry entry) => !entry.IsDirectory,
            };
            foreach (var file in files)
            {
                ct.ThrowIfCancellationRequested();
                list.Add(file);
            }
        }
        catch (OperationCanceledException) { throw; }
        catch { /* inaccessible or vanished: keep what was read */ }
        return list;
    }

    private static IEnumerable<string> Targets(Rule rule, string root)
    {
        if (!Directory.Exists(root)) return Array.Empty<string>();
        if (rule.Include.Count == 0) return new[] { root };
        var depth = Math.Max(0, (rule.Depth ?? 1) - 1);
        try
        {
            return Directory.EnumerateDirectories(root, "*", Opts(true, depth))
                .Where(d => rule.Include.Any(i =>
                    string.Equals(i, System.IO.Path.GetFileName(d), StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
        catch { return Array.Empty<string>(); }
    }

    public static RuleResult Scan(Rule rule, CancellationToken ct = default)
    {
        var result = new RuleResult { Rule = rule };
        var root = Rules.Expand(rule.Path);
        DateTime? cutoff = rule.OlderThanDays is int d ? DateTime.UtcNow.AddDays(-d) : null;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var dir in Targets(rule, root))
        foreach (var file in FilesWithSize(dir, ct))
        {
            if (Protected.IsProtected(file.Path)) { result.ProtectedSkipped++; continue; }
            if (!seen.Add(file.Path)) continue; // nested include folders can overlap
            if (cutoff is DateTime c && file.LastWriteUtc > c) continue;
            result.Items.Add(new FileItem(file.Path, file.Size));
        }
        return result;
    }

    public static List<RuleResult> ScanAll(IEnumerable<Rule> rules, CancellationToken ct = default) =>
        rules.AsParallel().AsOrdered().WithCancellation(ct).Select(r => Scan(r, ct)).ToList();
}
