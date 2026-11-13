using System.Diagnostics;
using System.Text.RegularExpressions;

namespace CDClean.Core;

/// <summary>Read-only maintenance analysers (Batch 2.4). Nothing here deletes or changes anything.</summary>
public static class Maintenance
{
    public sealed record WinSxS(long ActualBytes, long SharedBytes, long BackupsBytes, long CacheBytes, string LastCleanup, string Raw, string Error = "");
    public sealed record WindowsOld(long Bytes, bool Exists, bool CanRollback);
    public sealed record Vss(long UsedBytes, long MaxBytes, string Raw, string Error = "");
    public sealed record WuWorkspace(long Bytes);

    /// <summary>Runs DISM /AnalyzeComponentStore and parses the report (read-only).</summary>
    public static WinSxS AnalyzeWinSxS(CancellationToken ct = default)
    {
        long actual = 0, shared = 0, backups = 0, cache = 0; string last = "";
        var run = Run("dism.exe", "/Online /Cleanup-Image /AnalyzeComponentStore", ct);
        var raw = run.Output;
        actual = ParseBytes(raw, @"Actual Size of Component Store\s*:\s*([\d\.,]+\s*\w+)");
        shared = ParseBytes(raw, @"Shared with Windows\s*:\s*([\d\.,]+\s*\w+)");
        backups = ParseBytes(raw, @"Backups and Disabled Features\s*:\s*([\d\.,]+\s*\w+)");
        cache = ParseBytes(raw, @"Cache and Temporary Data\s*:\s*([\d\.,]+\s*\w+)");
        var m = Regex.Match(raw, @"Date of Last Cleanup\s*:\s*(.+)");
        if (m.Success) last = m.Groups[1].Value.Trim();
        var error = Explain(run, actual == 0 && shared == 0 && backups == 0 && cache == 0);
        return new WinSxS(actual, shared, backups, cache, last, raw, error);
    }

    public static WindowsOld MeasureWindowsOld()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "..", "Windows.old");
        // Resolve to C:\Windows.old
        var root = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows)) ?? @"C:\";
        dir = Path.Combine(root, "Windows.old");
        if (!Directory.Exists(dir)) return new WindowsOld(0, false, false);
        long bytes = 0;
        try { foreach (var f in Scanner.Files(dir)) try { bytes += new FileInfo(f).Length; } catch { } } catch { }
        // Rollback is possible while Windows.old exists and is recent.
        bool canRollback = false;
        try
        {
            var di = new DirectoryInfo(dir);
            canRollback = (DateTime.Now - di.CreationTime).TotalDays < 10;
        }
        catch { }
        return new WindowsOld(bytes, true, canRollback);
    }

    public static Vss AnalyzeVss(CancellationToken ct = default)
    {
        var run = Run("vssadmin.exe", "list shadowstorage", ct);
        var raw = run.Output;
        long used = 0, max = 0;
        var mu = Regex.Match(raw, @"Used Shadow Copy Storage space:\s*([\d\.,]+\s*\w+)");
        if (mu.Success) used = ParseBytes2(mu.Groups[1].Value);
        var mm = Regex.Match(raw, @"Maximum Shadow Copy Storage space:\s*([\d\.,]+\s*\w+)");
        if (mm.Success) max = ParseBytes2(mm.Groups[1].Value);
        var error = Explain(run, used == 0 && max == 0);
        return new Vss(used, max, raw, error);
    }

    public static WuWorkspace MeasureWuWorkspace()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution", "Download");
        long bytes = 0;
        if (Directory.Exists(dir))
            try { foreach (var f in Scanner.Files(dir)) try { bytes += new FileInfo(f).Length; } catch { } } catch { }
        return new WuWorkspace(bytes);
    }

    private sealed record CommandResult(int ExitCode, string Output);

    private static CommandResult Run(string exe, string args, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo(exe, args) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi);
            if (p is null) return new(-1, "The command could not be started.");
            var output = p.StandardOutput.ReadToEnd() + "\n" + p.StandardError.ReadToEnd();
            if (!p.WaitForExit(120000))
            {
                try { p.Kill(); } catch { }
                return new(-1, output + "\nTimed out.");
            }
            ct.ThrowIfCancellationRequested();
            return new(p.ExitCode, output);
        }
        catch (Exception e) { return new(-1, "Error: " + e.Message); }
    }

    /// <summary>Turns an empty/failed read-only command into a message instead of a misleading zero.</summary>
    private static string Explain(CommandResult r, bool noData)
    {
        if (!noData) return "";
        var text = r.Output.Trim();
        if (r.ExitCode == 740
            || text.Contains("Elevated permissions", StringComparison.OrdinalIgnoreCase)
            || text.Contains("requires elevation", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Access is denied", StringComparison.OrdinalIgnoreCase)
            || text.Contains("correct permissions", StringComparison.OrdinalIgnoreCase))
            return "Requires administrator. Run SpacePilot as administrator to read this value.";
        if (r.ExitCode != 0)
            return $"Exit code {r.ExitCode}: {text.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim()}";
        return "The command returned no readable data.";
    }

    private static long ParseBytes(string text, string pattern)
    {
        var m = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        if (!m.Success) return 0;
        return ParseBytes2(m.Groups[1].Value);
    }

    private static long ParseBytes2(string s)
    {
        s = s.Trim();
        var m = Regex.Match(s, @"([\d\.,]+)\s*(\w+)");
        if (!m.Success) return 0;
        var num = m.Groups[1].Value.Replace(",", "");
        if (!double.TryParse(num, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v)) return 0;
        var unit = m.Groups[2].Value.ToUpperInvariant();
        return unit switch
        {
            "B" => (long)v,
            "KB" => (long)(v * 1024),
            "MB" => (long)(v * 1024 * 1024),
            "GB" => (long)(v * 1024 * 1024 * 1024),
            "TB" => (long)(v * 1024L * 1024 * 1024 * 1024),
            _ => (long)v,
        };
    }
}
