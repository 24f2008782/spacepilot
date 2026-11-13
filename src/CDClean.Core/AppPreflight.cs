using System.Diagnostics;
using Microsoft.Win32;

namespace CDClean.Core;

public sealed record AppPreflightResult(IReadOnlyList<string> Running, bool PendingReboot, bool Broken)
{
    public bool HasWarning => Running.Count > 0 || PendingReboot || Broken;
}

/// <summary>Read-only checks before an uninstaller runs. Nothing is closed or changed here.</summary>
public static class AppPreflight
{
    public static AppPreflightResult Check(InstalledApp app) =>
        new(RunningFrom(app.InstallLocation), PendingReboot(), app.Broken);

    /// <summary>Processes whose executable lives inside the app's install folder.</summary>
    public static List<string> RunningFrom(string folder)
    {
        var names = new List<string>();
        if (string.IsNullOrWhiteSpace(folder)) return names;
        string prefix;
        try
        {
            if (!Path.IsPathFullyQualified(folder) || !Directory.Exists(folder)) return names;
            prefix = Path.GetFullPath(folder).TrimEnd('\\') + "\\";
        }
        catch { return names; }

        Process[] all;
        try { all = Process.GetProcesses(); } catch { return names; }
        foreach (var p in all)
        {
            try
            {
                var path = p.MainModule?.FileName;
                if (path != null && path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    names.Add($"{p.ProcessName} ({p.Id})");
            }
            catch { }
            finally { p.Dispose(); }
        }
        return names.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n).ToList();
    }

    /// <summary>Windows is waiting for a restart, so some uninstallers will fail or ask for one.</summary>
    public static bool PendingReboot()
    {
        try
        {
            using var cbs = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending");
            if (cbs != null) return true;
            using var wu = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired");
            if (wu != null) return true;
            using var sm = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\Session Manager");
            if (sm?.GetValue("PendingFileRenameOperations") != null) return true;
        }
        catch { }
        return false;
    }
}
