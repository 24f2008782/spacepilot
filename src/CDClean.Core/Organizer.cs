using System.Text.Json;

namespace CDClean.Core;

public sealed record PlannedMove(string From, string To, string Category);

/// <summary>Sorts loose files in one folder into category subfolders. Every move is logged so it can be undone.</summary>
public static class Organizer
{
    public static readonly IReadOnlyDictionary<string, string[]> Categories = new Dictionary<string, string[]>
    {
        ["Images"] = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".heic", ".svg", ".tif", ".tiff", ".ico", ".raw" },
        ["Videos"] = new[] { ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".webm", ".flv", ".m4v" },
        ["Audio"] = new[] { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a", ".wma", ".opus" },
        ["Documents"] = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".csv", ".ppt", ".pptx", ".txt", ".md", ".rtf", ".odt", ".epub" },
        ["Archives"] = new[] { ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".iso" },
        ["Installers"] = new[] { ".exe", ".msi", ".msix", ".appx", ".appinstaller" },
        ["Code"] = new[] { ".py", ".js", ".ts", ".cs", ".rs", ".java", ".cpp", ".c", ".h", ".html", ".css", ".json", ".xml", ".yml", ".yaml", ".toml", ".sql", ".ipynb" },
    };

    public static string CategoryOf(string file)
    {
        var ext = Path.GetExtension(file);
        foreach (var (cat, exts) in Categories)
            if (exts.Contains(ext, StringComparer.OrdinalIgnoreCase)) return cat;
        return "Other";
    }

    /// <summary>Only files directly in `folder` are planned. Subfolders are left alone.</summary>
    public static List<PlannedMove> Plan(string folder)
    {
        var moves = new List<PlannedMove>();
        foreach (var f in Directory.EnumerateFiles(folder))
        {
            var cat = CategoryOf(f);
            var dest = Path.Combine(folder, cat, Path.GetFileName(f));
            moves.Add(new PlannedMove(f, UniquePath(dest), cat));
        }
        return moves;
    }

    private static string UniquePath(string path)
    {
        if (!File.Exists(path)) return path;
        var dir = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var ext = Path.GetExtension(path);
        for (var i = 2; ; i++)
        {
            var candidate = Path.Combine(dir, $"{name} ({i}){ext}");
            if (!File.Exists(candidate)) return candidate;
        }
    }

    /// <summary>Applies the moves and writes an undo log. Returns (moved, failed).</summary>
    public static (int Moved, List<string> Failed) Apply(IEnumerable<PlannedMove> moves, string logDir)
    {
        Directory.CreateDirectory(logDir);
        var log = Path.Combine(logDir, $"organize-{DateTime.Now:yyyy-MM-dd_HHmmss}.jsonl");
        using var w = new StreamWriter(log);
        int moved = 0; var failed = new List<string>();
        foreach (var m in moves)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(m.To)!);
                File.Move(m.From, m.To);
                w.WriteLine(JsonSerializer.Serialize(m));
                w.Flush();
                moved++;
            }
            catch (Exception e) { failed.Add($"{m.From}: {e.Message}"); }
        }
        return (moved, failed);
    }

    public static string? LatestLog(string logDir) =>
        Directory.Exists(logDir)
            ? Directory.EnumerateFiles(logDir, "organize-*.jsonl").OrderByDescending(f => f).FirstOrDefault()
            : null;

    /// <summary>Moves every file in the log back. The log is renamed to *.undone.jsonl.</summary>
    public static int Undo(string log)
    {
        var count = 0;
        foreach (var line in File.ReadLines(log).Where(l => l.Trim().Length > 0).Reverse().ToList())
        {
            var m = JsonSerializer.Deserialize<PlannedMove>(line)!;
            if (File.Exists(m.To) && !File.Exists(m.From)) { File.Move(m.To, m.From); count++; }
        }
        File.Move(log, Path.ChangeExtension(log, ".undone.jsonl"), overwrite: true);
        return count;
    }
}
