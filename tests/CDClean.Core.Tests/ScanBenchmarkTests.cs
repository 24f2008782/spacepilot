using System.Diagnostics;
using CDClean.Core.Disk;
using Xunit;

/// <summary>
/// Checks that the fast and exact scans agree on a stable synthetic tree.
/// Results are written to a temp file (not printed), so the test output stays short.
/// </summary>
public class ScanBenchmarkTests
{
    [Fact]
    public void Fast_and_exact_scans_agree_on_a_stable_fixture()
    {
        var root = Path.Combine(Path.GetTempPath(), "cdclean-scan-" + Guid.NewGuid().ToString("N"));
        try
        {
            for (var d = 0; d < 3; d++)
            {
                var dir = Path.Combine(root, "d" + d, "nested");
                Directory.CreateDirectory(dir);
                for (var i = 0; i < 5; i++)
                    File.WriteAllBytes(Path.Combine(dir, $"f{i}.bin"), new byte[100 + d * 100 + i]);
            }
            File.WriteAllBytes(Path.Combine(root, "empty.bin"), Array.Empty<byte>());

            var sw = Stopwatch.StartNew();
            var fast = DiskScanner.Scan(root, fastMetadata: true);
            var fastMs = sw.ElapsedMilliseconds;

            sw.Restart();
            var exact = DiskScanner.Scan(root);
            var exactMs = sw.ElapsedMilliseconds;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var heapMb = GC.GetTotalMemory(true) / (1024 * 1024);
            var workMb = Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024);

            var bench = Path.Combine(Path.GetTempPath(), "cdclean-bench.txt");
            File.AppendAllText(bench,
                $"{DateTime.Now:s} folder=fixture items={fast.Nodes.Count} fastMs={fastMs} exactMs={exactMs} " +
                $"fastBytes={fast.TotalAllocated} exactBytes={exact.TotalAllocated} exactHardLinkSaved={exact.HardLinkSaved} " +
                $"heapMb={heapMb} workMb={workMb} budgetMb={DiskTree.BudgetMb} maxNodes={DiskTree.MaxNodes} truncated={exact.Truncated}{Environment.NewLine}");

            Assert.Equal(exact.FileCount, fast.FileCount);
            Assert.Equal(exact.TotalLogical, fast.TotalLogical);
            Assert.Equal(exact.Nodes.Count, fast.Nodes.Count);
            // Allocated sizes may differ slightly (see DiskScanner docs); the file and item counts must match exactly.
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
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
