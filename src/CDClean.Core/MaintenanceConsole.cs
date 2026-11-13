namespace CDClean.Core;

/// <summary>Maintenance console (F-24): enforces the right order of DISM -> SFC -> CHKDSK -> Optimize. Read-only listing of steps.</summary>
public sealed record MaintenanceStep(string Order, string Name, string Command, string Purpose, bool Destructive);

public static class MaintenanceConsole
{
    public static List<MaintenanceStep> Steps() => new()
    {
        new("1", "DISM component store analysis", "dism.exe /Online /Cleanup-Image /AnalyzeComponentStore", "Measure what WinSxS can reclaim (read-only).", false),
        new("2", "DISM component cleanup", "dism.exe /Online /Cleanup-Image /StartComponentCleanup", "Remove superseded components.", true),
        new("3", "System File Checker", "sfc.exe /scannow", "Repair protected system files.", false),
        new("4", "CHKDSK (read-only)", "chkdsk.exe C:", "Check the file system for errors (read-only).", false),
        new("5", "Optimize drives", "defrag.exe C: /O", "TRIM SSDs / defrag HDDs.", false),
    };

    public static string RecommendedOrderNote =>
        "Run in this order: DISM analysis -> DISM cleanup -> SFC -> CHKDSK -> Optimize. " +
        "Do not run CHKDSK /F or /R without a backup; do not run Optimize on a failing drive.";
}
