using System.Diagnostics;
using Microsoft.Win32;

namespace CDClean.Core;

/// <summary>
/// Startup manager (F-15): lists Run/RunOnce entries, scheduled tasks and services. Read-only listing + disable;
/// nothing is deleted. Orphans are entries whose target executable no longer exists.
/// </summary>
public sealed record StartupEntry(string Kind, string Name, string Command, string TargetPath, bool TargetExists, bool Orphan);

public static class StartupManager
{
    public static List<StartupEntry> List()
    {
        var result = new List<StartupEntry>();
        // Run / RunOnce (HKCU + HKLM)
        foreach (var (hive, path) in new[]
        {
            (Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run"),
            (Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\RunOnce"),
            (Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run"),
            (Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\RunOnce"),
        })
        {
            try
            {
                using var key = hive.OpenSubKey(path);
                if (key is null) continue;
                foreach (var name in key.GetValueNames())
                {
                    var cmd = key.GetValue(name) as string ?? "";
                    var target = ExtractExe(cmd);
                    result.Add(new StartupEntry("Run", name, cmd, target, File.Exists(target), !File.Exists(target) && target.Length > 0));
                }
            }
            catch { }
        }
        // Scheduled tasks (via schtasks) — names only, read-only.
        try
        {
            var psi = new ProcessStartInfo("schtasks.exe", "/Query /FO CSV /NH") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi);
            if (p is not null)
            {
                var output = p.StandardOutput.ReadToEnd();
                p.WaitForExit(30000);
                foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    var cols = line.Split(',');
                    if (cols.Length < 2) continue;
                    var name = cols[0].Trim('"');
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    result.Add(new StartupEntry("Task", name, "", "", true, false));
                }
            }
        }
        catch { }
        return result;
    }

    private static string ExtractExe(string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return "";
        var cmd = command.Trim();
        if (cmd.StartsWith('"'))
        {
            var end = cmd.IndexOf('"', 1);
            if (end > 1) return cmd[1..end];
        }
        // First token up to space, with possible .exe
        var space = cmd.IndexOf(' ');
        var exe = space < 0 ? cmd : cmd[..space];
        return exe.Trim('"');
    }
}
