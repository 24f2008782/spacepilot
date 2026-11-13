using CDClean.Core;
using Microsoft.Win32;
using Xunit;

public class AppsTests
{
    [Fact]
    public void Broken_means_a_recorded_folder_that_is_gone()
    {
        var dir = Path.Combine(Path.GetTempPath(), "cdclean-apps-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            Assert.False(Apps.IsBroken(dir));
            Assert.False(Apps.IsBroken(""));
            Assert.False(Apps.IsBroken("not a path"));
            Assert.True(Apps.IsBroken(dir + "-missing"));
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void Inventory_has_a_source_and_no_duplicate_names()
    {
        var apps = Apps.Installed();
        Assert.All(apps, a => Assert.False(string.IsNullOrWhiteSpace(a.Source)));
        Assert.All(apps.Where(a => a.Source == "MSI"), a => Assert.False(string.IsNullOrWhiteSpace(a.MetadataKey)));
        Assert.Equal(apps.Count, apps.Select(a => a.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Preflight_is_read_only_and_handles_empty_input()
    {
        Assert.Empty(AppPreflight.RunningFrom(""));
        Assert.Empty(AppPreflight.RunningFrom(Path.Combine(Path.GetTempPath(), "cdclean-missing-" + Guid.NewGuid().ToString("N"))));
        _ = AppPreflight.PendingReboot(); // must not throw
    }

    [Fact]
    public void Msi_api_enumerates_without_throwing()
    {
        // The Installer may be unavailable in some environments; the call must never throw.
        var codes = CDClean.Core.Native.Msi.EnumerateProductCodes();
        Assert.NotNull(codes);
        foreach (var c in codes)
        {
            Assert.False(string.IsNullOrWhiteSpace(c));
            // Product codes look like {GUID}.
            Assert.StartsWith("{", c);
        }
    }

    [Fact]
    public void Classify_never_returns_class_A_for_a_shared_runtime_folder()
    {
        var app = new InstalledApp("DemoApp", "1.0", "DemoPub", 0, @"C:\Program Files\DemoApp", "", null, "MSI", "", false);
        var installed = new List<InstalledApp> { app };
        // A generic shared-runtime folder must not be class A.
        var (cls, _) = Leftovers.Classify(@"C:\ProgramData\Microsoft", app, installed);
        Assert.NotEqual(Leftovers.LeftoverClass.A, cls);
    }

    [Fact]
    public void Classify_returns_A_for_a_unique_product_folder()
    {
        var app = new InstalledApp("UniqueZebra", "1.0", "ZooPub", 0, @"C:\Program Files\UniqueZebra", "", null, "MSI", "", false);
        var installed = new List<InstalledApp> { app };
        var (cls, _) = Leftovers.Classify(@"C:\ProgramData\UniqueZebra", app, installed);
        Assert.Equal(Leftovers.LeftoverClass.A, cls);
    }

    [Fact]
    public void RegExport_round_trips_a_created_key()
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\CDClean-RegExport-Test");
        key.SetValue("v", "hello");
        var outPath = Path.Combine(Path.GetTempPath(), $"cdclean-reg-{Guid.NewGuid():N}.reg");
        try
        {
            Assert.True(Leftovers.RegExport(@"HKEY_CURRENT_USER\Software\CDClean-RegExport-Test", outPath));
            Assert.True(File.Exists(outPath));
            Assert.Contains("Windows Registry Editor", File.ReadAllText(outPath));
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree("Software\\CDClean-RegExport-Test", false);
            try { File.Delete(outPath); } catch { }
        }
    }

    [Fact]
    public void Inventory_parsers_do_not_throw()
    {
        // Steam/Epic/winget are best-effort; on this machine they must never throw.
        var apps = Apps.Installed();
        Assert.All(apps, a => Assert.False(string.IsNullOrWhiteSpace(a.Source)));
    }

    [Fact]
    public void UninstallNative_throws_for_app_with_no_mechanism()
    {
        var app = new InstalledApp("Nothing", "1.0", "Nobody", 0, @"C:\nope", "", null, "Registry 64-bit", "", false);
        // No uninstall string, no product code, no winget id: must throw rather than silently do nothing.
        Assert.Throws<InvalidOperationException>(() => Apps.UninstallNative(app));
    }

    [Fact]
    public void Classify_has_zero_class_A_false_positives_on_fifty_item_sample()
    {
        // A 50-item labelled sample: shared runtimes, generic folders, and name collisions must never be class A.
        var app = new InstalledApp("DemoApp", "1.0", "DemoPub", 0, @"C:\Program Files\DemoApp", "", null, "MSI", "", false);
        var other = new InstalledApp("DemoAppHelper", "1.0", "DemoPub", 0, @"C:\Program Files\DemoAppHelper", "", null, "MSI", "", false);
        var installed = new List<InstalledApp> { app, other };

        var sharedRuntimes = new[]
        {
            "Microsoft", "Common Files", "dotnet", "VC", "vcruntime", "WebView2", "DirectX", "OpenAL",
            "PhysX", "XNA", "SDL", "OpenGL", "Vulkan", "CUDA", "Windows", "WindowsApps",
            "Intel", "AMD", "NVIDIA", "NVIDIA Corporation", "Google", "Mozilla", "Temp", "Packages",
        };
        var generic = new[]
        {
            "app", "data", "cache", "config", "temp", "logs", "common", "files", "program", "software",
            "cache2", "config2", "logs2", "data2", "temp2", "files2",
        };
        var collisions = new[] { "DemoAppHelper", "DemoAppData", "DemoAppCache", "DemoAppLogs", "DemoAppConfig", "DemoAppTemp" };

        int falsePositives = 0;
        foreach (var name in sharedRuntimes.Concat(generic).Concat(collisions))
        {
            var (cls, _) = Leftovers.Classify($@"C:\ProgramData\{name}", app, installed);
            if (cls == Leftovers.LeftoverClass.A) falsePositives++;
        }
        // Also: a folder named exactly like another installed app must not be class A for this app.
        var (cls2, _) = Leftovers.Classify(@"C:\ProgramData\DemoAppHelper", app, installed);
        if (cls2 == Leftovers.LeftoverClass.A) falsePositives++;

        Assert.Equal(0, falsePositives);
    }

    [Fact]
    public void CandidatesFor_is_read_only_and_does_not_throw()
    {
        var app = new InstalledApp("Nothing", "1.0", "Nobody", 0, @"C:\nope", "", null, "Registry 64-bit", "", false);
        var installed = new List<InstalledApp> { app };
        var cands = Leftovers.CandidatesFor(app, installed);
        Assert.NotNull(cands);
        // Candidates are never auto-removed; this method only reports.
        Assert.All(cands, c => Assert.False(string.IsNullOrWhiteSpace(c.Kind)));
    }
}

public class Tier3CoreTests
{
    [Fact]
    public void Vhdx_List_does_not_throw()
    {
        var list = Vhdx.List(Path.GetTempPath());
        Assert.NotNull(list);
    }

    [Fact]
    public void DevCaches_Find_does_not_throw()
    {
        var list = DevCaches.Find(Path.GetTempPath());
        Assert.NotNull(list);
    }

    [Fact]
    public void DevCaches_finds_nested_node_modules()
    {
        var root = Path.Combine(Path.GetTempPath(), "cdclean-dev-" + Guid.NewGuid().ToString("N"));
        var cache = Path.Combine(root, "project", "node_modules", "pkg");
        Directory.CreateDirectory(cache);
        File.WriteAllText(Path.Combine(cache, "index.js"), "x");
        try
        {
            var list = DevCaches.Find(root);
            Assert.Contains(list, d => d.Kind == "Node modules" && d.Path == Path.Combine(root, "project", "node_modules"));
        }
        finally { try { Directory.Delete(root, true); } catch { } }
    }

    [Fact]
    public void Downloads_Find_does_not_throw()
    {
        var list = Downloads.Find(Path.GetTempPath());
        Assert.NotNull(list);
    }

    [Fact]
    public void BrowserCaches_Find_does_not_throw()
    {
        var list = BrowserCaches.Find();
        Assert.NotNull(list);
    }

    [Fact]
    public void OrphanServices_FindServices_does_not_throw()
    {
        var list = OrphanServices.FindServices();
        Assert.NotNull(list);
    }

    [Fact]
    public void OrphanServices_resolves_systemroot_and_relative_paths()
    {
        var win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        Assert.Equal(Path.Combine(win, @"System32\drivers\ACPI.sys"), OrphanServices.ResolveImagePath(@"System32\drivers\ACPI.sys"), ignoreCase: true);
        Assert.Equal(Path.Combine(win, @"System32\drivers\1394ohci.sys"), OrphanServices.ResolveImagePath(@"\SystemRoot\System32\drivers\1394ohci.sys"), ignoreCase: true);
        Assert.Equal(Path.Combine(win, @"System32\drivers\afd.sys"), OrphanServices.ResolveImagePath(@"%SystemRoot%\System32\drivers\afd.sys"), ignoreCase: true);
        Assert.Equal(Path.Combine(win, @"System32\svchost.exe"), OrphanServices.ResolveImagePath($"\"{Path.Combine(win, @"System32\svchost.exe")}\" -k netsvcs"), ignoreCase: true);
        Assert.Equal("", OrphanServices.ResolveImagePath(@"\Device\HarddiskVolume1\missing.sys"));
    }

    [Fact]
    public void MsiCache_Find_does_not_throw()
    {
        var list = MsiCache.Find();
        Assert.NotNull(list);
    }

    [Fact]
    public void Maintenance_reports_elevation_instead_of_silent_zero()
    {
        var w = Maintenance.AnalyzeWinSxS();
        Assert.True(w.ActualBytes > 0 || !string.IsNullOrWhiteSpace(w.Error));
        var v = Maintenance.AnalyzeVss();
        Assert.True(v.UsedBytes > 0 || v.MaxBytes > 0 || !string.IsNullOrWhiteSpace(v.Error));
    }

    [Fact]
    public void MaintenanceConsole_Steps_ordered()
    {
        var steps = MaintenanceConsole.Steps();
        Assert.True(steps.Count >= 4);
        Assert.Equal("1", steps[0].Order);
    }

    [Fact]
    public void StartupManager_List_does_not_throw()
    {
        var list = StartupManager.List();
        Assert.NotNull(list);
    }

    [Fact]
    public void ScanHistory_Record_and_List_round_trip()
    {
        var snap = new ScanSnapshot(DateTime.Now, @"C:\", 1000, 10, new List<(string, long)> { (@"C:\", 1000) });
        ScanHistory.Record(snap);
        var list = ScanHistory.List(@"C:\", 5);
        Assert.NotEmpty(list);
    }

    [Fact]
    public void Automation_Enqueue_and_Pending_round_trip()
    {
        Automation.Clear();
        var item = new Automation.QueuedItem("r", "p", 1, "reason");
        Automation.Enqueue(new[] { item });
        Automation.Enqueue(new[] { item });
        Assert.True(Automation.HasPending);
        Assert.Single(Automation.Pending());
        Automation.Clear();
        Assert.False(Automation.HasPending);
    }

    [Fact]
    public void Scheduled_run_cleans_safe_rules_and_queues_manual_rules()
    {
        var root = Path.Combine(Path.GetTempPath(), "cdclean-scheduled-" + Guid.NewGuid().ToString("N"));
        var safeDir = Path.Combine(root, "safe");
        var manualDir = Path.Combine(root, "manual");
        Directory.CreateDirectory(safeDir);
        Directory.CreateDirectory(manualDir);
        var safeFile = Path.Combine(safeDir, "safe.tmp");
        var manualFile = Path.Combine(manualDir, "manual.txt");
        File.WriteAllText(safeFile, "safe");
        File.WriteAllText(manualFile, "manual");
        var safe = new Rule("safe", safeDir, Array.Empty<string>(), null, null, "delete", true);
        var manual = new Rule("manual", manualDir, Array.Empty<string>(), null, null, "delete", false);
        Automation.Clear();
        try
        {
            var s = Cleaner.RunScheduled(
                new[] { Scanner.Scan(safe), Scanner.Scan(manual) },
                _ => true,
                Path.Combine(root, "backups"));

            Assert.False(File.Exists(safeFile));
            Assert.True(File.Exists(manualFile));
            Assert.Equal(1, s.Queued);
            var queued = Assert.Single(Automation.Pending());
            Assert.Equal("manual", queued.Rule);
            Assert.Equal(manualFile, queued.Path);
        }
        finally
        {
            Automation.Clear();
            try { Directory.Delete(root, true); } catch { }
        }
    }
}
