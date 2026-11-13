using CDClean.Core.Native;

namespace CDClean.Core.Safety;

public enum Verdict { Allow, Warn, Block }

public enum Reason
{
    None, Gone, IdentityChanged, ProtectedPath, Reparse, SystemFile,
    Locked, HardLink, Cloud, RecentUse, SizeThreshold, RiskExceeded, Unreadable,
}

[Flags]
public enum ItemFlags { None = 0, Reparse = 1, HardLink = 2, Locked = 4, System = 8, Cloud = 16, RecentlyUsed = 32 }

/// <summary>What the gate looks at for one item, re-read right before acting on it.</summary>
public sealed record GateInput(string Path, bool Exists, FileKey Key, long Bytes, ItemFlags Flags, RiskClass Risk, bool ProtectedPath = false)
{
    public static GateInput From(FileFacts f, bool locked, RiskClass risk, int recentDays, bool protectedPath = false)
    {
        var flags = ItemFlags.None;
        if (f.IsLink) flags |= ItemFlags.Reparse;
        if (f.IsHardLinked) flags |= ItemFlags.HardLink;
        if (locked) flags |= ItemFlags.Locked;
        if (f.IsSystem) flags |= ItemFlags.System;
        if (f.IsCloudPlaceholder) flags |= ItemFlags.Cloud;
        if (f.Exists && f.LastWriteUtc > DateTime.UtcNow.AddDays(-recentDays)) flags |= ItemFlags.RecentlyUsed;
        return new GateInput(f.FinalPath, f.Exists, f.Key, f.Allocated, flags, risk, protectedPath);
    }
}

/// <summary>Safety settings for one job. The protected list always includes the built-in Windows locations.</summary>
public sealed record Policy(
    IReadOnlyList<string> ProtectedPrefixes,
    long SizeWarnBytes = 1L << 30,
    RiskClass MaxRisk = RiskClass.Review,
    bool NeverFollowReparse = true,
    bool BlockSystemAttributes = true,
    int RecentDays = 30)
{
    /// <summary>Windows, WindowsApps, System Volume Information on every drive, the vault, and the user's own list.</summary>
    public static Policy Default(string? vaultRoot = null, IEnumerable<string>? extra = null)
    {
        var p = new List<string>
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps"),
        };
        foreach (var d in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed))
            p.Add(Path.Combine(d.RootDirectory.FullName, "System Volume Information"));
        if (!string.IsNullOrWhiteSpace(vaultRoot)) p.Add(vaultRoot);
        if (extra != null) p.AddRange(extra.Where(x => !string.IsNullOrWhiteSpace(x)));
        return new Policy(p.Select(Norm).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
    }

    /// <summary>Folder prefixes end in a backslash so C:\Windows does not match C:\WindowsOld.</summary>
    internal static string Norm(string p) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(p)) + "\\";

    public bool IsProtected(string path)
    {
        if (Protected.IsProtected(path)) return true;
        var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path)) + "\\";
        return ProtectedPrefixes.Any(pre => full.StartsWith(pre, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>The last check before anything is deleted. Port of dw_gate_evaluate from the DiskWise prototype.</summary>
public static class Gate
{
    public static (Verdict Verdict, List<Reason> Reasons) Evaluate(GateInput? current, FileKey expected, Policy pol)
    {
        var rs = new List<Reason>();
        if (current is null || !current.Exists) { rs.Add(Reason.Gone); return (Verdict.Block, rs); }
        if (current.Key != expected) { rs.Add(Reason.IdentityChanged); return (Verdict.Block, rs); }
        if (current.ProtectedPath || pol.IsProtected(current.Path)) { rs.Add(Reason.ProtectedPath); return (Verdict.Block, rs); }
        if (pol.NeverFollowReparse && current.Flags.HasFlag(ItemFlags.Reparse)) { rs.Add(Reason.Reparse); return (Verdict.Block, rs); }
        if (pol.BlockSystemAttributes && current.Flags.HasFlag(ItemFlags.System)) { rs.Add(Reason.SystemFile); return (Verdict.Block, rs); }

        var v = Verdict.Allow;
        void Warn(Reason r) { rs.Add(r); v = Verdict.Warn; }
        if (current.Flags.HasFlag(ItemFlags.Locked)) Warn(Reason.Locked);
        if (current.Flags.HasFlag(ItemFlags.HardLink)) Warn(Reason.HardLink);
        if (current.Flags.HasFlag(ItemFlags.Cloud)) Warn(Reason.Cloud);
        if (current.Flags.HasFlag(ItemFlags.RecentlyUsed)) Warn(Reason.RecentUse);
        if (current.Bytes > pol.SizeWarnBytes) Warn(Reason.SizeThreshold);
        if (current.Risk > pol.MaxRisk) { rs.Add(Reason.RiskExceeded); v = Verdict.Block; }
        return (v, rs);
    }

    public static string Text(Reason r) => r switch
    {
        Reason.None => "no issue",
        Reason.Gone => "no longer exists",
        Reason.IdentityChanged => "the path now holds a different file",
        Reason.ProtectedPath => "protected location",
        Reason.Reparse => "link or junction, not followed",
        Reason.SystemFile => "Windows system file",
        Reason.Locked => "in use by another program",
        Reason.HardLink => "has other hard links (deleting frees no space)",
        Reason.Cloud => "cloud file: deleting may also delete it online",
        Reason.RecentUse => "changed recently",
        Reason.SizeThreshold => "very large",
        Reason.RiskExceeded => "risk too high for this action",
        Reason.Unreadable => "could not be read (access denied)",
        _ => r.ToString(),
    };
}
