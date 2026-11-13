using System.Diagnostics;

namespace CDClean.Core;

public sealed class AppSettings
{
    /// <summary>Schema version written by <see cref="SettingsStore"/>. Missing in 1.5.x files, which is treated as version 1.</summary>
    public int SchemaVersion { get; set; } = SettingsStore.CurrentVersion;
    public string BackupRoot { get; set; } = @"E:\cdclean-backups";
    public int KeepDays { get; set; } = 30;
    public string RulesPath { get; set; } = "";
    public int LowSpaceGb { get; set; } = 60;
    public List<string> KeepFolders { get; set; } = new();
    public List<string> EnabledRules { get; set; } = new();
    /// <summary>Your own folders that are never offered for deletion (added to the built-in Windows list).</summary>
    public List<string> ProtectedPaths { get; set; } = new();
    /// <summary>Backup vault size cap in GB; oldest runs are removed past it. 0 = no cap.</summary>
    public int VaultCapGb { get; set; }
    public bool RedactAudit { get; set; }
    /// <summary>Saved table layout per page: column widths, order, hidden columns, sort keys.</summary>
    public Dictionary<string, GridLayout> GridLayouts { get; set; } = new();
    /// <summary>Last folder or drive each page scanned, so the scope is kept and never widened silently.</summary>
    public Dictionary<string, string> Scopes { get; set; } = new();
    /// <summary>Main window position and size, restored if it still fits on a connected screen.</summary>
    public int[]? WindowRect { get; set; }

    public Safety.Policy Policy() => Safety.Policy.Default(BackupRoot, ProtectedPaths);
    public ExecOptions Exec(bool proceedOnWarn) => new(BackupRoot, proceedOnWarn, VaultCapBytes: VaultCapGb * (1L << 30));

    public static string DataDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CDClean");
    public static string LogDir => Path.Combine(DataDir, "logs");

    public string ResolvedRulesPath =>
        string.IsNullOrWhiteSpace(RulesPath) ? Path.Combine(AppContext.BaseDirectory, "rules.toml") : RulesPath;

    /// <summary>A notice from the last load, shown on the Settings page when a file could not be used.</summary>
    public static string? LastLoadNotice { get; private set; }

    public static AppSettings Load()
    {
        var result = SettingsStore.Load();
        LastLoadNotice = result.Notice;
        return result.Settings;
    }

    public void Save() => SettingsStore.Save(this);

    public static void ClearLoadNotice() => LastLoadNotice = null;
}

public sealed class GridLayout
{
    public Dictionary<string, double> Widths { get; set; } = new();
    public List<string> Order { get; set; } = new();
    public List<string> Hidden { get; set; } = new();
    /// <summary>Up to three keys like "Size:desc".</summary>
    public List<string> Sort { get; set; } = new();
}

public sealed record DiskStatus(long Total, long Free)
{
    public long Used => Total - Free;
    public static DiskStatus Of(string drive)
    {
        var d = new DriveInfo(drive);
        return new DiskStatus(d.TotalSize, d.AvailableFreeSpace);
    }
}

/// <summary>An admin task, run by CDClean.Elevated.exe after the Windows UAC prompt.</summary>
public sealed record AdminTask(string Id, string Title, string What, string Gain, string Undo, bool Risky)
{
    /// <summary>The exact commands the elevated helper runs, one per line (kept in sync by a test).</summary>
    public string Commands { get; init; } = "";
    public string Duration { get; init; } = "";
    public string Restart { get; init; } = "";
    public string Risks { get; init; } = "";
    /// <summary>What this task will not touch.</summary>
    public string Excluded { get; init; } = "";
    public ConfirmClass Class { get; init; } = ConfirmClass.Elevated;
    /// <summary>Word the user must type for Irreversible tasks.</summary>
    public string Token { get; init; } = "";
    /// <summary>What the Elevated-class checkbox makes the user acknowledge.</summary>
    public string Consequence { get; init; } = "";
}

/// <summary>Report §4.11.2: how much confirmation an operation needs.</summary>
public enum ConfirmClass { Standard, Elevated, Irreversible }

public static class AdminTasks
{
    public static readonly IReadOnlyList<AdminTask> All = new[]
    {
        new AdminTask("winsxs", "Trim WinSxS",
            "Removes old versions of Windows components that newer updates replaced.",
            "Usually 1-3 GB", "Safe. You can still uninstall updates.", false)
        {
            Commands = "dism.exe /Online /Cleanup-Image /StartComponentCleanup",
            Duration = "5-20 minutes", Restart = "Not required.",
            Risks = "Do not shut down while it runs. CPU and disk are busy.",
            Excluded = "Installed updates stay uninstallable. No personal files are touched.",
            Consequence = "Windows removes superseded components; this cannot be stopped halfway safely.",
        },
        new AdminTask("winsxs-deep", "Deep trim WinSxS (ResetBase)",
            "Also removes the rollback copies of installed updates.",
            "Usually 1-3 GB more", "Cannot be undone. Installed updates can no longer be uninstalled.", true)
        {
            Commands = "dism.exe /Online /Cleanup-Image /StartComponentCleanup /ResetBase",
            Duration = "10-30 minutes", Restart = "Not required.",
            Risks = "Every update installed so far becomes permanent. If an update causes problems later, it cannot be removed.",
            Excluded = "No personal files, programs or settings are touched.",
            Class = ConfirmClass.Irreversible, Token = "RESETBASE",
        },
        new AdminTask("reserved-off", "Turn off reserved storage",
            "Frees the space Windows keeps aside so updates can install on a full disk.",
            "About 7 GB", "Undo with the 'Turn on reserved storage' task.", false)
        {
            Commands = "dism.exe /Online /Set-ReservedStorageState /State:Disabled",
            Duration = "Under a minute", Restart = "Not required. Fails if an update is pending: restart first, then retry.",
            Risks = "A future feature update needs that free space itself; keep 20 GB or more free.",
            Excluded = "No files are deleted by this task.",
            Consequence = "updates no longer have space set aside for them",
        },
        new AdminTask("reserved-on", "Turn on reserved storage",
            "Gives the reserved space back to Windows updates.",
            "Uses about 7 GB", "Undo with 'Turn off reserved storage'.", false)
        {
            Commands = "dism.exe /Online /Set-ReservedStorageState /State:Enabled",
            Duration = "Under a minute", Restart = "Not required.", Risks = "Uses about 7 GB of free space.",
            Excluded = "No files are deleted by this task.", Consequence = "about 7 GB of free space goes to Windows",
        },
        new AdminTask("drivers", "Remove old driver versions",
            "Keeps the newest copy of each driver package. Drivers a device is using are never removed.",
            "Hundreds of MB", "Reinstall a driver from the maker's site if needed.", false)
        {
            Commands = "pnputil.exe /enum-drivers\npnputil.exe /delete-driver oemNN.inf   (once per older duplicate, never /force)",
            Duration = "1-3 minutes", Restart = "Not required.",
            Risks = "You cannot roll a device back to an older driver version from Device Manager.",
            Excluded = "The newest version of every driver, and every driver a device is using right now.",
            Consequence = "older driver versions cannot be rolled back to",
        },
        new AdminTask("wu-cache", "Clear Windows Update downloads",
            "Stops the update service, empties SoftwareDistribution\\Download, then starts it again.",
            "0-5 GB", "Safe. Windows downloads updates again if needed.", false)
        {
            Commands = "net.exe stop wuauserv\nnet.exe stop bits\ndelete everything in C:\\Windows\\SoftwareDistribution\\Download\nnet.exe start bits\nnet.exe start wuauserv",
            Duration = "1-4 minutes", Restart = "Not required. A pending update may restart the PC on its own schedule.",
            Risks = "Do not run while Windows Update is downloading. Update history may look reset.",
            Excluded = "Installed updates, update history, Windows.old, the Delivery Optimization cache.",
            Consequence = "the update engine downloads anything it still needs again",
        },
        new AdminTask("do-cache", "Clear Delivery Optimization cache",
            "Removes update files kept to share with other PCs.",
            "0-2 GB", "Safe.", false)
        {
            Commands = "powershell.exe -NoProfile -Command Delete-DeliveryOptimizationCache -Force",
            Duration = "Under a minute", Restart = "Not required.", Risks = "Other PCs on your network can no longer fetch updates from this one until it re-downloads.",
            Excluded = "Windows Update downloads and installed updates.", Consequence = "shared update files are removed",
        },
        new AdminTask("cleanmgr", "Windows Disk Cleanup (all system items)",
            "Runs Windows' own cleanup with every item ticked: old logs, error reports, upgrade leftovers, thumbnails, and previous Windows installations.",
            "0.5-2 GB, much more if Windows.old exists", "Cannot be undone.", true)
        {
            Commands = "reg: set StateFlags0001 = 2 on every key under HKLM\\...\\Explorer\\VolumeCaches\ncleanmgr.exe /sagerun:1",
            Duration = "5-30 minutes", Restart = "Not required.",
            Risks = "Every Disk Cleanup item is included. If Windows.old exists, you lose the option to go back to the previous Windows version. Recycle Bin contents may be emptied.",
            Excluded = "Your documents, programs and settings.",
            Class = ConfirmClass.Irreversible, Token = "CLEANUP",
        },
        new AdminTask("hibernate-off", "Turn off hibernation",
            "Deletes hiberfil.sys, which is about the size of your RAM. Sleep still works.",
            "RAM size, if hibernation is on", "Undo with 'powercfg /h on'.", false)
        {
            Commands = "powercfg.exe /h off",
            Duration = "Seconds", Restart = "Not required.",
            Risks = "Hibernate and Fast Startup stop working until you turn hibernation back on.",
            Excluded = "Sleep still works. No other files are touched.", Consequence = "Hibernate and Fast Startup are switched off",
        },
        new AdminTask("restore-oldest", "Delete the oldest restore point",
            "Removes the oldest System Restore point on C:.",
            "Varies", "Cannot be undone.", true)
        {
            Commands = "vssadmin.exe delete shadows /for=C: /oldest /quiet",
            Duration = "Seconds", Restart = "Not required.",
            Risks = "That restore point is backup data: you can no longer roll Windows back to it.",
            Excluded = "Newer restore points and your files.",
            Class = ConfirmClass.Irreversible, Token = "DELETE",
        },
    };

    public static string ElevatedExe => Path.Combine(AppContext.BaseDirectory, "CDClean.Elevated.exe");

    /// <summary>Starts the elevated helper. Throws Win32Exception (1223) if the user declines the UAC prompt.</summary>
    public static string Start(string taskId)
    {
        Directory.CreateDirectory(AppSettings.LogDir);
        var log = Path.Combine(AppSettings.LogDir, $"admin-{taskId}-{DateTime.Now:yyyyMMdd-HHmmss}.log");
        Process.Start(new ProcessStartInfo(ElevatedExe, $"{taskId} \"{log}\"") { UseShellExecute = true, Verb = "runas" });
        return log;
    }

    /// <summary>Reads the helper's log: PROGRESS n, LOG text, DONE ok|fail.</summary>
    public static (double? Percent, List<string> Lines, bool Done, bool Ok) ReadLog(string log)
    {
        var lines = new List<string>();
        double? pct = null; bool done = false, ok = false;
        if (!File.Exists(log)) return (null, lines, false, false);
        using var fs = new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var r = new StreamReader(fs);
        while (r.ReadLine() is { } line)
        {
            if (line.StartsWith("PROGRESS ") && double.TryParse(line[9..], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var p)) pct = p;
            else if (line.StartsWith("DONE ")) { done = true; ok = line == "DONE ok"; }
            else if (line.StartsWith("LOG ")) lines.Add(line[4..]);
        }
        return (pct, lines, done, ok);
    }
}

public static class Schedule
{
    public static string TaskName(string kind) => kind == "weekly" ? "CDClean-Weekly" : "CDClean-Daily";

    private static (int Code, string Output) Schtasks(params string[] args)
    {
        var psi = new ProcessStartInfo("schtasks.exe") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi)!;
        var output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, output.Trim());
    }

    public static bool Exists(string kind) => Schtasks("/Query", "/TN", TaskName(kind)).Code == 0;

    public static void Set(string kind, bool on, string time, string exePath)
    {
        if (kind is not ("weekly" or "daily")) throw new ArgumentException("kind must be weekly or daily");
        if (!TimeOnly.TryParseExact(time, "HH:mm", out _)) throw new ArgumentException("time must be HH:mm");
        if (!on)
        {
            Schtasks("/Delete", "/TN", TaskName(kind), "/F");
            return;
        }
        var args = new List<string> { "/Create", "/TN", TaskName(kind), "/TR", $"\"{exePath}\" --clean-safe", "/ST", time, "/F" };
        args.AddRange(kind == "weekly" ? new[] { "/SC", "WEEKLY", "/D", "SUN" } : new[] { "/SC", "DAILY" });
        var (code, output) = Schtasks(args.ToArray());
        if (code != 0) throw new InvalidOperationException($"schtasks failed: {output}");
    }
}
