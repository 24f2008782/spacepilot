using System.Globalization;
using System.Text.Json;

namespace CDClean.Core;

/// <summary>One file in a backup job. The fields after Rule were added in 2.0 and are empty for 1.0 runs.</summary>
public sealed record ManifestEntry(
    string Original, string Backup, long Size, string Rule,
    string? Sha256 = null, int Attributes = 0, DateTime? LastWriteUtc = null, DateTime? CreationUtc = null);

public sealed record BackupRun(string Id, int Files, long Bytes, bool Restored);

public static class Backups
{
    private static void Validate(string id)
    {
        if (id.Length == 0 || id.Any(c => !(char.IsAsciiDigit(c) || c is '-' or '_')))
            throw new ArgumentException($"Not a backup run id: {id}");
    }

    public static List<BackupRun> List(string root)
    {
        var runs = new List<BackupRun>();
        if (!Directory.Exists(root)) return runs;
        foreach (var dir in Directory.EnumerateDirectories(root))
        {
            var id = Path.GetFileName(dir);
            if (id.Length < 10 || !DateTime.TryParseExact(id[..10], "yyyy-MM-dd", null, DateTimeStyles.None, out _))
                continue;
            var active = Path.Combine(dir, "manifest.jsonl");
            var restored = Path.Combine(dir, "manifest.restored.jsonl");
            var file = File.Exists(active) ? active : restored;
            int files = 0; long bytes = 0;
            if (File.Exists(file))
                foreach (var line in File.ReadLines(file).Where(l => l.Trim().Length > 0))
                {
                    files++;
                    try { bytes += JsonSerializer.Deserialize<ManifestEntry>(line)?.Size ?? 0; } catch { }
                }
            runs.Add(new BackupRun(id, files, bytes, !File.Exists(active) && File.Exists(restored)));
        }
        return runs.OrderByDescending(r => r.Id).ToList();
    }

    /// <summary>
    /// Puts every file back. Existing files are never overwritten. When the manifest has a hash,
    /// the backup is checked before and the restored file after copying.
    /// </summary>
    public static (int Restored, int Skipped) Restore(string root, string id)
    {
        Validate(id);
        var runDir = Path.Combine(root, id);
        var manifest = Path.Combine(runDir, "manifest.jsonl");
        if (!File.Exists(manifest)) throw new FileNotFoundException($"No restorable manifest for {id}.");
        int ok = 0, skipped = 0;
        foreach (var line in File.ReadLines(manifest).Where(l => l.Trim().Length > 0).ToList())
        {
            var e = JsonSerializer.Deserialize<ManifestEntry>(line)!;
            if (File.Exists(e.Original)) { skipped++; continue; }
            if (e.Sha256 != null && Vault.Sha256(e.Backup) != e.Sha256)
                throw new IOException($"Backup of {e.Original} is damaged (hash mismatch). It was not restored.");
            Directory.CreateDirectory(Path.GetDirectoryName(e.Original)!);
            File.Copy(e.Backup, e.Original);
            if (new FileInfo(e.Original).Length != e.Size || (e.Sha256 != null && Vault.Sha256(e.Original) != e.Sha256))
                throw new IOException($"Restored copy of {e.Original} does not match the backup.");
            if (e.LastWriteUtc is DateTime w) File.SetLastWriteTimeUtc(e.Original, w);
            if (e.CreationUtc is DateTime c) File.SetCreationTimeUtc(e.Original, c);
            if (e.Attributes != 0) File.SetAttributes(e.Original, (FileAttributes)e.Attributes);
            File.Delete(e.Backup);
            ok++;
        }
        File.Move(manifest, Path.Combine(runDir, "manifest.restored.jsonl"), overwrite: true);
        return (ok, skipped);
    }

    public static void Delete(string root, string id)
    {
        Validate(id);
        Directory.Delete(Path.Combine(root, id), recursive: true);
    }

    /// <summary>Deletes runs older than keepDays, judged by the date in the folder name.</summary>
    public static List<string> Prune(string root, int keepDays, bool dryRun)
    {
        var cutoff = DateTime.Today.AddDays(-keepDays);
        var removed = new List<string>();
        foreach (var run in List(root))
        {
            var day = DateTime.ParseExact(run.Id[..10], "yyyy-MM-dd", null);
            if (day >= cutoff) continue;
            if (!dryRun) Delete(root, run.Id);
            removed.Add(run.Id);
        }
        return removed;
    }

    /// <summary>Oldest runs first, deletes until the vault is under capBytes. The run named keep is never removed.</summary>
    public static List<string> PruneToCap(string root, long capBytes, string? keep, bool dryRun)
    {
        var removed = new List<string>();
        if (capBytes <= 0) return removed;
        var runs = List(root);
        var total = runs.Sum(r => r.Bytes);
        foreach (var run in runs.OrderBy(r => r.Id))
        {
            if (total <= capBytes) break;
            if (run.Id == keep) continue;
            if (!dryRun) Delete(root, run.Id);
            total -= run.Bytes;
            removed.Add(run.Id);
        }
        return removed;
    }
}
