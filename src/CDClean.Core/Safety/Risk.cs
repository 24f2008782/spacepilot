using CDClean.Core.Native;

namespace CDClean.Core.Safety;

public enum RiskClass { Safe, Review, High, Protected }

/// <summary>What kind of thing an item is. Cache and Temp can be rebuilt, so they may be Safe.</summary>
public enum Category { Cache, Temp, AppData, UserFile, Unknown }

/// <summary>Additive risk score with hard overrides (report §9.6). The overrides cannot be configured away.</summary>
public static class Risk
{
    private static readonly string ProgramFiles = Root(Environment.SpecialFolder.ProgramFiles);
    private static readonly string ProgramFilesX86 = Root(Environment.SpecialFolder.ProgramFilesX86);
    private static readonly string CommonAppData = Root(Environment.SpecialFolder.CommonApplicationData);

    private static string Root(Environment.SpecialFolder folder)
    {
        var path = Environment.GetFolderPath(folder);
        return path.Length == 0 ? "" : Policy.Norm(path);
    }

    public static int Score(FileFacts f, Category cat, bool locked, int recentDays)
    {
        var path = f.FinalPath;
        var score = cat is Category.Cache or Category.Temp ? 0 : 10;
        if (f.LastWriteUtc > DateTime.UtcNow.AddDays(-recentDays)) score += 20;
        if (Under(path, ProgramFiles) || Under(path, ProgramFilesX86)) score += 15;
        else if (Under(path, CommonAppData)) score += 5;
        if (f.IsHardLinked) score += 20;
        if (locked) score += 25;
        if (f.IsCloudPlaceholder) score += 15;
        return score;
    }

    public static RiskClass Classify(FileFacts f, Category cat, bool locked, Policy pol, bool? protectedPath = null)
    {
        if ((protectedPath ?? pol.IsProtected(f.FinalPath)) || (pol.BlockSystemAttributes && f.IsSystem)) return RiskClass.Protected;
        if (locked || f.IsHardLinked || f.IsCloudPlaceholder) return RiskClass.High;
        var s = Score(f, cat, locked, pol.RecentDays);
        if (s >= 60) return RiskClass.High;
        if (s >= 25) return RiskClass.Review;
        return cat is Category.Cache or Category.Temp ? RiskClass.Safe : RiskClass.Review;
    }

    private static bool Under(string path, string root) =>
        root.Length > 0 && path.StartsWith(root, StringComparison.OrdinalIgnoreCase);
}
