using System.Diagnostics;
using CDClean.Core.Disk;
using Xunit;

/// <summary>
/// Times the fast scan against the exact scan on a real folder and checks that both agree.
/// Results are written to a temp file (not printed), so the test output stays short.
/// </summary>
public class ScanBenchmarkTests
{
    private const string Folder = @"C:\Windows\System32";

    [Fact]
    public void Fast_and_exact_scans_agree_and_timings_are_recorded()
    {
        if (!Directory.Exists(Folder)) return;

        var sw = Stopwatch.StartNew();
        var fast = DiskScanner.Scan(Folder, fastMetadata: true);
        var fastMs = sw.ElapsedMilliseconds;

        sw.Restart();
        var exact = DiskScanner.Scan(Folder);
        var exactMs = sw.ElapsedMilliseconds;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var heapMb = GC.GetTotalMemory(true) / (1024 * 1024);
        var workMb = Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024);

        var bench = Path.Combine(Path.GetTempPath(), "cdclean-bench.txt");
        File.AppendAllText(bench,
            $"{DateTime.Now:s} folder={Folder} items={fast.Nodes.Count} fastMs={fastMs} exactMs={exactMs} " +
            $"fastBytes={fast.TotalAllocated} exactBytes={exact.TotalAllocated} exactHardLinkSaved={exact.HardLinkSaved} " +
            $"heapMb={heapMb} workMb={workMb} budgetMb={DiskTree.BudgetMb} maxNodes={DiskTree.MaxNodes} truncated={exact.Truncated}{Environment.NewLine}");

        Assert.Equal(exact.FileCount, fast.FileCount);
        Assert.Equal(exact.TotalLogical, fast.TotalLogical);
        Assert.Equal(exact.Nodes.Count, fast.Nodes.Count);
        // Allocated sizes may differ slightly (see DiskScanner docs); the file and item counts must match exactly.
    }

    [Fact] // one-off memory evidence; set CDCLEAN_BENCH=1 to run the 40 s large-tree check
    public void Large_scan_stays_within_budget()
    {
        if (Environment.GetEnvironmentVariable("CDCLEAN_BENCH") != "1") return;
        const string big = @"C:\Windows";
        if (!Directory.Exists(big)) return;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetTotalMemory(true);
        var sw = Stopwatch.StartNew();
        var tree = DiskScanner.Scan(big);
        var ms = sw.ElapsedMilliseconds;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var heapMb = (GC.GetTotalMemory(true) - before) / (1024 * 1024);
        var workMb = Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024);
        var bench = Path.Combine(Path.GetTempPath(), "cdclean-bench.txt");
        File.AppendAllText(bench,
            $"{DateTime.Now:s} large={big} items={tree.Nodes.Count} ms={ms} heapDeltaMb={heapMb} workMb={workMb} " +
            $"budgetMb={DiskTree.BudgetMb} truncated={tree.Truncated}{Environment.NewLine}");
        Assert.False(tree.Truncated);
        Assert.True(heapMb < DiskTree.BudgetMb, $"scan used {heapMb} MB, budget {DiskTree.BudgetMb} MB");
    }
}
