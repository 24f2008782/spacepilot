using System.Text.Json;

namespace CDClean.Core;

/// <summary>One line in the audit log. Kind is "item" for each file and "job" for the job summary.</summary>
public sealed record AuditLine(
    DateTime Time, string Job, string Kind, string Path, string Method, string Outcome,
    string Verdict, string[] Reasons, long Bytes, string? Error = null,
    long? Predicted = null, long? Measured = null);

/// <summary>Append-only JSONL log, one file per day. Never contains file contents.</summary>
public static class Audit
{
    private static readonly object Lock = new();
    public static string Dir { get; set; } = Path.Combine(AppSettings.DataDir, "audit");

    /// <summary>Replace the user profile folder with %USERPROFILE% in logged paths.</summary>
    public static bool Redact { get; set; }

    public static void Write(AuditLine line)
    {
        if (Redact)
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            line = line with { Path = line.Path.Replace(home, "%USERPROFILE%", StringComparison.OrdinalIgnoreCase) };
        }
        var json = JsonSerializer.Serialize(line);
        lock (Lock)
        {
            Directory.CreateDirectory(Dir);
            File.AppendAllText(System.IO.Path.Combine(Dir, $"{line.Time:yyyy-MM-dd}.jsonl"), json + "\n");
        }
    }

    public static IEnumerable<AuditLine> Read(string job) =>
        Directory.Exists(Dir)
            ? Directory.EnumerateFiles(Dir, "*.jsonl").SelectMany(File.ReadLines)
                .Select(l => JsonSerializer.Deserialize<AuditLine>(l)!).Where(a => a.Job == job)
            : Enumerable.Empty<AuditLine>();

    /// <summary>Deletes daily log files older than keepDays.</summary>
    public static void Rotate(int keepDays = 90)
    {
        if (!Directory.Exists(Dir)) return;
        var cutoff = DateTime.Today.AddDays(-keepDays);
        foreach (var f in Directory.EnumerateFiles(Dir, "*.jsonl"))
            if (DateTime.TryParse(System.IO.Path.GetFileNameWithoutExtension(f), out var d) && d < cutoff)
                File.Delete(f);
    }
}
