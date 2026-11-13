using System.Text.Json;

namespace CDClean.Core;

/// <summary>
/// Preview-first automation (F-23): scheduled runs execute Safe-class rules only. Items that are not Safe-class
/// go into a review queue shown on next launch, instead of being deleted silently.
/// </summary>
public static class Automation
{
    private static string QueueFile => Path.Combine(AppSettings.DataDir, "review-queue.json");

    public sealed record QueuedItem(string Rule, string Path, long Bytes, string Reason);

    public static void Enqueue(IEnumerable<QueuedItem> items)
    {
        try
        {
            var list = Pending();
            var seen = list.Select(i => i.Rule + "\0" + i.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var added = items.Where(i => seen.Add(i.Rule + "\0" + i.Path)).ToList();
            if (added.Count == 0) return;
            list.AddRange(added);
            Directory.CreateDirectory(AppSettings.DataDir);
            File.WriteAllText(QueueFile, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    public static List<QueuedItem> Pending()
    {
        try
        {
            if (!File.Exists(QueueFile)) return new();
            return JsonSerializer.Deserialize<List<QueuedItem>>(File.ReadAllText(QueueFile)) ?? new();
        }
        catch { return new(); }
    }

    public static void Clear() { try { if (File.Exists(QueueFile)) File.Delete(QueueFile); } catch { } }

    public static bool HasPending => Pending().Count > 0;
}
