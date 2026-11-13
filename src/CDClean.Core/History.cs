using System.Text.Json;

namespace CDClean.Core;

/// <summary>
/// Storage history: a snapshot of used space + top folders after a scan, used to show "what grew" on the Dashboard.
/// Stored under %LOCALAPPDATA%\CDClean\scans\ with a retention cap.
/// </summary>
public sealed record ScanSnapshot(DateTime Taken, string Drive, long UsedBytes, int Files,
    List<(string Path, long Bytes)> TopFolders);

public static class ScanHistory
{
    private static string Dir => Path.Combine(AppSettings.DataDir, "scans");
    private const int MaxSnapshots = 20;

    public static void Record(ScanSnapshot snap)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            var file = Path.Combine(Dir, $"scan-{snap.Taken:yyyyMMdd-HHmmss}-{Path.GetFileName(snap.Drive.TrimEnd('\\'))}.json");
            var obj = new
            {
                snap.Taken, snap.Drive, snap.UsedBytes, snap.Files,
                TopFolders = snap.TopFolders.Select(t => new { t.Path, t.Bytes }).ToList(),
            };
            File.WriteAllText(file, JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true }));
            Prune();
        }
        catch { /* history is best-effort */ }
    }

    public static List<ScanSnapshot> List(string drive, int take = 5)
    {
        var result = new List<ScanSnapshot>();
        if (!Directory.Exists(Dir)) return result;
        foreach (var f in Directory.EnumerateFiles(Dir, "scan-*.json").OrderByDescending(File.GetLastWriteTimeUtc))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(f));
                var r = doc.RootElement;
                var driveVal = r.GetProperty("Drive").GetString() ?? "";
                if (!string.Equals(driveVal.TrimEnd('\\'), drive.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)) continue;
                var top = new List<(string, long)>();
                if (r.TryGetProperty("TopFolders", out var tf))
                    foreach (var t in tf.EnumerateArray())
                        top.Add((t.GetProperty("Path").GetString() ?? "", t.GetProperty("Bytes").GetInt64()));
                result.Add(new ScanSnapshot(
                    r.GetProperty("Taken").GetDateTime(),
                    driveVal,
                    r.GetProperty("UsedBytes").GetInt64(),
                    r.GetProperty("Files").GetInt32(),
                    top));
                if (result.Count >= take) break;
            }
            catch { }
        }
        return result;
    }

    public static (long UsedDelta, int FilesDelta, List<(string Path, long Delta)> TopDelta)? Diff(ScanSnapshot prev, ScanSnapshot curr)
    {
        if (prev is null || curr is null) return null;
        var usedDelta = curr.UsedBytes - prev.UsedBytes;
        var filesDelta = curr.Files - prev.Files;
        var topDelta = new List<(string, long)>();
        var prevMap = prev.TopFolders.ToDictionary(t => t.Path, t => t.Bytes);
        foreach (var (p, b) in curr.TopFolders)
        {
            var d = prevMap.TryGetValue(p, out var pb) ? b - pb : b;
            if (d != 0) topDelta.Add((p, d));
        }
        return (usedDelta, filesDelta, topDelta);
    }

    private static void Prune()
    {
        try
        {
            var files = Directory.EnumerateFiles(Dir, "scan-*.json").OrderByDescending(File.GetLastWriteTimeUtc).ToList();
            foreach (var f in files.Skip(MaxSnapshots)) { try { File.Delete(f); } catch { } }
        }
        catch { }
    }
}
