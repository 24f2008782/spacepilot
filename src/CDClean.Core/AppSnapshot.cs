using System.Text;

namespace CDClean.Core;

/// <summary>
/// A small before/after snapshot of the folders an uninstaller is expected to touch: the install folder and the
/// app's AppData/ProgramData folders. Only sizes and file counts are recorded, so the snapshot is cheap and the
/// diff honestly reports what is left (it never claims "completely removed").
/// </summary>
public sealed record AppSnapshot(long InstallBytes, int InstallFiles, long AppDataBytes, int AppDataFiles)
{
    public long TotalBytes => InstallBytes + AppDataBytes;
    public int TotalFiles => InstallFiles + AppDataFiles;

    public static AppSnapshot Capture(InstalledApp app)
    {
        long ib = 0; int ifc = 0;
        if (!string.IsNullOrWhiteSpace(app.InstallLocation) && Directory.Exists(app.InstallLocation))
        {
            try
            {
                foreach (var f in Scanner.Files(app.InstallLocation))
                {
                    try { var fi = new FileInfo(f); ib += fi.Length; ifc++; } catch { }
                }
            }
            catch { }
        }
        long ab = 0; int afc = 0;
        var norm = Leftovers.Norm(app.Name);
        foreach (var root in Leftovers.Roots(null))
        {
            if (!Directory.Exists(root)) continue;
            try
            {
                foreach (var dir in Directory.EnumerateDirectories(root))
                {
                    if (Leftovers.Norm(Path.GetFileName(dir)) != norm) continue;
                    foreach (var f in Scanner.Files(dir))
                    {
                        try { var fi = new FileInfo(f); ab += fi.Length; afc++; } catch { }
                    }
                }
            }
            catch { }
        }
        return new AppSnapshot(ib, ifc, ab, afc);
    }

    public static (long RemovedBytes, int RemovedFiles, long RemainingBytes, int RemainingFiles) Diff(AppSnapshot before, AppSnapshot after)
    {
        var removedBytes = Math.Max(0, before.TotalBytes - after.TotalBytes);
        var removedFiles = Math.Max(0, before.TotalFiles - after.TotalFiles);
        return (removedBytes, removedFiles, after.TotalBytes, after.TotalFiles);
    }
}

public static class AppSnapshotExtensions
{
    // Convenience for callers that want a one-line report.
    public static string Describe(this AppSnapshot s) => $"{FmtSize(s.TotalBytes)} in {s.TotalFiles:N0} files";
    private static string FmtSize(long b) => b >= 1L << 30 ? $"{b / 1073741824.0:0.00} GB" :
        b >= 1L << 20 ? $"{b / 1048576.0:0.0} MB" : $"{b / 1024.0:0} KB";
}