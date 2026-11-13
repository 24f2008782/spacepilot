using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using Microsoft.Win32;

namespace CDClean.Core;

public sealed record InstalledApp(string Name, string Version, string Publisher, long SizeBytes,
    string InstallLocation, string UninstallString, DateTime? InstallDate,
    string Source = "Registry", string MetadataKey = "", bool Broken = false,
    string ProductCode = "", string WingetId = "");

public static class Apps
{
    public static List<InstalledApp> Installed()
    {
        var list = new List<InstalledApp>();
        (RegistryKey Hive, string Path, string Source)[] keys =
        {
            (Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", "Registry 64-bit"),
            (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", "Registry 32-bit"),
            (Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", "Registry (user)"),
        };
        // MSI product codes, keyed by install location, so an MSI record can carry its product code.
        var msiCodes = MsiProductCodes();
        foreach (var (hive, path, baseSource) in keys)
        {
            using var key = hive.OpenSubKey(path);
            if (key is null) continue;
            foreach (var sub in key.GetSubKeyNames())
            {
                using var s = key.OpenSubKey(sub);
                if (s?.GetValue("DisplayName") is not string name || string.IsNullOrWhiteSpace(name)) continue;
                if (s.GetValue("SystemComponent") is int sc && sc == 1) continue;
                if (s.GetValue("ParentKeyName") is string) continue; // an update or patch, not a program
                long size = s.GetValue("EstimatedSize") is int kb ? kb * 1024L : 0;
                DateTime? date = DateTime.TryParseExact(s.GetValue("InstallDate") as string, "yyyyMMdd",
                    null, DateTimeStyles.None, out var d) ? d : null;
                var location = (s.GetValue("InstallLocation") as string ?? "").Trim().Trim('"');
                var msi = s.GetValue("WindowsInstaller") is int wi && wi == 1;
                var productCode = msi && location.Length > 0 && msiCodes.TryGetValue(location.ToLowerInvariant(), out var pc) ? pc : "";
                list.Add(new InstalledApp(name.Trim(), s.GetValue("DisplayVersion") as string ?? "",
                    s.GetValue("Publisher") as string ?? "", size, location,
                    s.GetValue("UninstallString") as string ?? "", date,
                    msi ? "MSI" : baseSource, $"{hive.Name}\\{path}\\{sub}", IsBroken(location), productCode));
            }
        }
        AddMsix(list);
        AddWinget(list);
        AddSteam(list);
        AddEpic(list);
        return list.GroupBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First()).OrderByDescending(a => a.SizeBytes).ToList();
    }

    /// <summary>Maps an MSI install location to its product code. Empty when the Installer is unavailable.</summary>
    public static Dictionary<string, string> MsiProductCodes()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var code in Native.Msi.EnumerateProductCodes())
            {
                var loc = Native.Msi.InstallLocation(code);
                if (!string.IsNullOrWhiteSpace(loc)) map[loc!.ToLowerInvariant()] = code;
            }
        }
        catch { } // Installer service unavailable; the registry inventory still works
        return map;
    }

    /// <summary>True when the program records an install folder that no longer exists.</summary>
    public static bool IsBroken(string location)
    {
        if (string.IsNullOrWhiteSpace(location)) return false;
        try
        {
            if (!Path.IsPathFullyQualified(location)) return false;
            return !File.Exists(location) && !Directory.Exists(location);
        }
        catch { return false; }
    }

    /// <summary>MSIX/Store packages. Read-only here; Windows Settings removes them.</summary>
    private static void AddMsix(List<InstalledApp> list)
    {
        try
        {
            var pm = new Windows.Management.Deployment.PackageManager();
            foreach (var p in pm.FindPackagesForUser(""))
            {
                try
                {
                    if (p.IsFramework || p.IsResourcePackage) continue;
                    var name = p.DisplayName;
                    if (string.IsNullOrWhiteSpace(name) || name.StartsWith("ms-resource:", StringComparison.OrdinalIgnoreCase))
                        name = p.Id.Name;
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    var location = p.InstalledLocation?.Path ?? "";
                    list.Add(new InstalledApp(name.Trim(), p.Id.Version.ToString() ?? "", p.PublisherDisplayName ?? "",
                        0, location, "", null, "MSIX", p.Id.FullName, false, WingetId: p.Id.FullName));
                }
                catch { }
            }
        }
        catch { } // package APIs are unavailable on some systems; the registry inventory still works
    }

    /// <summary>
    /// winget inventory: asks winget to export its installed-package list to JSON, then adds any package whose
    /// name is not already in the registry inventory. Reads only; nothing is installed or removed here.
    /// winget's table output is unstable, so the JSON export is the only thing parsed.
    /// </summary>
    private static void AddWinget(List<InstalledApp> list)
    {
        var existing = list.Select(a => NormName(a.Name)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var file = Path.Combine(Path.GetTempPath(), $"cdclean-winget-{Guid.NewGuid():N}.json");
        try
        {
            var psi = new ProcessStartInfo("winget.exe",
                $"export -o \"{file}\" --include-versions --accept-source-agreements --disable-interactivity")
            { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            using var p = Process.Start(psi);
            if (p is null) return;
            p.WaitForExit(15000);
            if (!File.Exists(file)) return;
            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(file));
            if (!doc.RootElement.TryGetProperty("Packages", out var pkgs)) return;
            foreach (var pkg in pkgs.EnumerateArray())
            {
                var id = pkg.TryGetProperty("PackageIdentifier", out var idEl) ? idEl.GetString() : null;
                var version = pkg.TryGetProperty("PackageVersion", out var vEl) ? vEl.GetString() : "";
                if (string.IsNullOrWhiteSpace(id)) continue;
                var name = id!.Split('.').Last();
                if (string.IsNullOrWhiteSpace(name) || existing.Contains(NormName(name))) continue;
                list.Add(new InstalledApp(name.Trim(), version ?? "", "", 0, "", "", null, "winget", id, false, WingetId: id));
                existing.Add(NormName(name));
            }
        }
        catch { } // winget missing or export failed: the registry inventory still works
        finally { try { if (File.Exists(file)) File.Delete(file); } catch { } }
    }

    /// <summary>Steam games: read the Steam path from the registry, then each library's appmanifest_*.acf files.</summary>
    private static void AddSteam(List<InstalledApp> list)
    {
        var existing = list.Select(a => NormName(a.Name)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            var steamPath = key?.GetValue("SteamPath") as string;
            if (string.IsNullOrWhiteSpace(steamPath)) return;
            steamPath = steamPath!.Replace('/', '\\');
            var libs = new List<string> { steamPath };
            var vdf = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
            if (File.Exists(vdf))
            {
                foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s*\"([^\"]+)\""))
                    libs.Add(m.Groups[1].Value.Replace("\\\\", "\\"));
            }
            foreach (var lib in libs.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var dir = Path.Combine(lib, "steamapps");
                if (!Directory.Exists(dir)) continue;
                foreach (var acf in Directory.EnumerateFiles(dir, "appmanifest_*.acf"))
                {
                    try
                    {
                        var text = File.ReadAllText(acf);
                        var name = Match(text, "\"name\"\\s*\"([^\"]+)\"");
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        if (existing.Contains(NormName(name))) continue;
                        var installDir = Match(text, "\"installdir\"\\s*\"([^\"]+)\"");
                        var location = string.IsNullOrWhiteSpace(installDir) ? "" : Path.Combine(dir, "common", installDir);
                        var size = 0L;
                        if (!string.IsNullOrWhiteSpace(location) && Directory.Exists(location))
                            try { size = new DirectoryInfo(location).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length); } catch { }
                        list.Add(new InstalledApp(name.Trim(), "", "Valve / Steam", size, location, "", null, "Steam", acf, IsBroken(location)));
                        existing.Add(NormName(name));
                    }
                    catch { }
                }
            }
        }
        catch { } // Steam not installed or unreadable; the registry inventory still works
    }

    /// <summary>Epic Games Launcher installs: read LauncherInstalled.dat (JSON) and add any new app.</summary>
    private static void AddEpic(List<InstalledApp> list)
    {
        var existing = list.Select(a => NormName(a.Name)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var file = @"C:\ProgramData\Epic\UnrealEngineLauncher\LauncherInstalled.dat";
        if (!File.Exists(file)) return;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(file));
            if (!doc.RootElement.TryGetProperty("InstallationList", out var items)) return;
            foreach (var item in items.EnumerateArray())
            {
                var location = item.TryGetProperty("InstallLocation", out var loc) ? loc.GetString() : null;
                var appName = item.TryGetProperty("AppName", out var an) ? an.GetString() : null;
                var displayName = item.TryGetProperty("DisplayName", out var dn) ? dn.GetString() : appName;
                if (string.IsNullOrWhiteSpace(displayName)) continue;
                if (existing.Contains(NormName(displayName))) continue;
                var name = displayName!.Trim();
                list.Add(new InstalledApp(name, "", "Epic Games", 0, location ?? "", "", null, "Epic", file, IsBroken(location ?? "")));
                existing.Add(NormName(name));
            }
        }
        catch { } // Epic not installed or unreadable
    }

    private static string NormName(string s) =>
        new string(s.ToLowerInvariant().Where(char.IsAsciiLetterOrDigit).ToArray());

    private static string Match(string text, string pattern)
    {
        var m = System.Text.RegularExpressions.Regex.Match(text, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : "";
    }

    /// <summary>Starts the program's own uninstaller. It shows its own windows and prompts.</summary>
    public static void RunUninstaller(InstalledApp app)
    {
        if (string.IsNullOrWhiteSpace(app.UninstallString))
            throw new InvalidOperationException($"{app.Name} has no uninstaller registered.");
        Process.Start(new ProcessStartInfo("cmd.exe", $"/s /c \"{app.UninstallString}\"")
        { UseShellExecute = false, CreateNoWindow = true });
    }

    /// <summary>
    /// Native uninstall: MSI runs through the Installer's own mechanism (REMOVE=ALL), MSIX through
    /// PackageManager.RemovePackageAsync, winget through `winget uninstall --id`, and everything else
    /// falls back to the registered uninstaller. Nothing is closed or force-killed here.
    /// </summary>
    public static void UninstallNative(InstalledApp app)
    {
        if (app.Source == "MSI" && !string.IsNullOrWhiteSpace(app.ProductCode))
        {
            var rc = Native.Msi.Remove(app.ProductCode);
            if (rc != 0) throw new InvalidOperationException($"{app.Name}: Installer returned {rc}.");
            return;
        }
        if (app.Source == "MSIX" && !string.IsNullOrWhiteSpace(app.MetadataKey))
        {
            var pm = new Windows.Management.Deployment.PackageManager();
            var op = pm.RemovePackageAsync(app.MetadataKey);
            // Wait synchronously with a timeout so the caller can take a snapshot afterwards.
            var t = op.AsTask();
            if (!t.Wait(300000)) throw new TimeoutException($"{app.Name}: MSIX removal timed out.");
            if (t.IsFaulted) throw t.Exception!.GetBaseException();
            return;
        }
        if (!string.IsNullOrWhiteSpace(app.WingetId))
        {
            var psi = new ProcessStartInfo("winget.exe",
                $"uninstall --id {app.WingetId} --exact --silent --accept-source-agreements --disable-interactivity")
            { UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi) ?? throw new InvalidOperationException($"{app.Name}: could not start winget.");
            if (!p.WaitForExit(300000)) { try { p.Kill(); } catch { } throw new TimeoutException($"{app.Name}: winget timed out."); }
            if (p.ExitCode != 0) throw new InvalidOperationException($"{app.Name}: winget exited with code {p.ExitCode}.");
            return;
        }
        RunUninstaller(app);
    }
}

/// <summary>A folder summary. OldFiles fills Paths with just the old files, so actions never touch newer files in the same folder.</summary>
public sealed record FolderReport(string Path, long Bytes, int Files, DateTime Newest, IReadOnlyList<string>? Paths = null);

public static class Leftovers
{
    private static readonly HashSet<string> SystemFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Microsoft", "Windows", "Packages", "Temp", "Programs", "Google", "Mozilla", "Desktop", "Comms",
        "ConnectedDevicesPlatform", "Publishers", "PeerDistRepub", "Package Cache", "ssh", "regid",
        "Claude", "Claude-Data", "Code", "NVIDIA", "NVIDIA Corporation", "Intel", "AMD", "uv", "pip",
        "CDClean", "D3DSCache", "CrashDumps", "History", "Application Data", "Microsoft Help",
        "SquirrelTemp", "Downloaded Installations", "USOPrivate", "USOShared", "SoftwareDistribution",
        // Program Files folders that belong to Windows or are shared, never leftovers:
        "WindowsApps", "ModifiableWindowsApps", "Common Files", "Windows Defender", "Windows Defender Advanced Threat Protection",
        "Windows Mail", "Windows Media Player", "Windows NT", "Windows Photo Viewer", "Windows Portable Devices",
        "Windows Security", "Windows Sidebar", "WindowsPowerShell", "Internet Explorer", "Reference Assemblies",
        "MSBuild", "dotnet", "Uninstall Information", "Microsoft.NET", "Windows Kits", "WSL", "Microsoft Update Health Tools",
    };

    /// <summary>Lowercase letters and digits only, with "-updater" removed.</summary>
    public static string Norm(string s) =>
        new(s.ToLowerInvariant().Replace("-updater", "").Where(char.IsAsciiLetterOrDigit).ToArray());

    public static bool Matches(string folderName, IEnumerable<string> normalizedApps)
    {
        var key = Norm(folderName);
        return key.Length < 4 || normalizedApps.Any(h => h.Length >= 4 && (h.Contains(key) || key.Contains(h)));
    }

    /// <summary>
    /// Folders to compare with installed programs. The system drive always contributes AppData and ProgramData
    /// (settings live there wherever the app was installed). Each chosen drive adds its Program Files folders.
    /// </summary>
    public static List<string> Roots(IEnumerable<string>? driveRoots)
    {
        var roots = new List<string>
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        };
        foreach (var d in driveRoots ?? Array.Empty<string>())
        {
            roots.Add(Path.Combine(d, "Program Files"));
            roots.Add(Path.Combine(d, "Program Files (x86)"));
        }
        return roots.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static List<FolderReport> Find(IReadOnlySet<string> keep, CancellationToken ct = default, IEnumerable<string>? driveRoots = null)
    {
        var apps = Apps.Installed()
            .SelectMany(a => new[] { Norm(a.Name), Norm(a.Publisher), Norm(a.InstallLocation) })
            .Where(s => s.Length > 0).Distinct().ToList();
        var roots = Roots(driveRoots);
        var found = new List<FolderReport>();
        foreach (var root in roots.Where(Directory.Exists))
        foreach (var dir in Directory.EnumerateDirectories(root))
        {
            ct.ThrowIfCancellationRequested();
            var name = Path.GetFileName(dir);
            if (name.StartsWith('.') || SystemFolders.Contains(name) || keep.Contains(dir) || Matches(name, apps))
                continue;
            found.Add(Measure(dir, ct));
        }
        return found.OrderByDescending(f => f.Bytes).ToList();
    }

    public static FolderReport Measure(string dir, CancellationToken ct = default)
    {
        long bytes = 0; int files = 0; var newest = DateTime.MinValue;
        foreach (var f in Scanner.Files(dir))
        {
            ct.ThrowIfCancellationRequested();
            try { var fi = new FileInfo(f); bytes += fi.Length; files++; if (fi.LastWriteTime > newest) newest = fi.LastWriteTime; }
            catch { }
        }
        return new FolderReport(dir, bytes, files, newest);
    }

    /// <summary>Ownership class for a leftover folder, per playbook-v2 10.2: A = strong evidence, B = medium, C = unverified.</summary>
    public enum LeftoverClass { A, B, C }

    /// <summary>
    /// Shared runtimes, COM registrations and Windows-owned folders that are never proposed for removal,
    /// even when their name partially matches an app. Consulted by `Classify` and by the leftover review.
    /// </summary>
    public static readonly HashSet<string> Preserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "Microsoft", "Windows", "WindowsApps", "Common Files", "dotnet", "Microsoft.NET", "Windows Kits",
        "MSBuild", "Reference Assemblies", "VC", "vcruntime", "WebView2", ".NET", "COM", "ole32",
        "DirectX", "OpenAL", "PhysX", "XNA", "SDL", "OpenGL", "Vulkan", "CUDA",
    };

    /// <summary>True when a folder or registry key is on the preserved list and must never be proposed for removal.</summary>
    public static bool IsPreserved(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return true;
        var n = Norm(name);
        foreach (var p in Preserved) if (n.Contains(Norm(p))) return true;
        return false;
    }

    /// <summary>
    /// Classifies one leftover folder. Strong signals (class A): folder name contains the app's unique product token
    /// (4+ chars, not a generic word) and no other installed app shares the token. Medium (B): name prefix matches
    /// the app but the token is generic or shared. C: everything else, including shared runtimes and COM data.
    /// </summary>
    public static (LeftoverClass Class, string Evidence) Classify(string folder, InstalledApp app, IEnumerable<InstalledApp> installed)
    {
        var name = Path.GetFileName(folder.TrimEnd('\\'));
        if (IsPreserved(name)) return (LeftoverClass.C, "On the preserved list (shared runtime / COM / Windows-owned); never proposed for removal.");
        var normName = Norm(name);
        var normApp = Norm(app.Name);
        var others = installed.Where(a => !string.Equals(a.Name, app.Name, StringComparison.OrdinalIgnoreCase)).ToList();
        var shared = others.Any(a => Norm(a.Name).Length >= 4 && (Norm(a.Name).Contains(normName) || normName.Contains(Norm(a.Name))));

        // Generic words that never qualify as a unique product token on their own.
        var generic = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "app", "data", "cache", "config", "temp", "logs", "common", "files", "program", "software", "microsoft", "windows" };

        var token = normApp.Length >= 4 ? normApp : "";
        var unique = token.Length >= 4 && !generic.Contains(token) && !shared;
        // A generic word in the folder name (data, cache, ...) means it is likely a shared/runtime folder, not a unique product folder.
        var hasGeneric = generic.Any(g => normName.Contains(g) && normName != token);

        // Strong signal: the folder name carries the app's unique token and no other app shares it, and no generic suffix.
        if (unique && normName.Contains(token) && !shared && !hasGeneric)
            return (LeftoverClass.A, $"Folder name contains the app's unique token '{token}' and no other installed app shares it.");

        // Medium: name prefix matches but the token is generic or shared with another app.
        if (normName.Length >= 4 && (normName.Contains(normApp) || normApp.Contains(normName)) && !shared)
            return (LeftoverClass.B, "Folder name matches the app name, but the token is generic or shared with another app.");

        // Unverified: shared runtime, COM data, or no name match at all.
        var reason = shared ? "Another installed app shares this name token, so it may be shared." :
            (generic.Contains(normName) ? "Generic folder name; likely a shared runtime." : "No strong name match to the app.");
        return (LeftoverClass.C, reason);
    }

    /// <summary>One leftover candidate for an app: a folder, registry key, service, task, Run value, shortcut or file association.</summary>
    public sealed record LeftoverCandidate(string Kind, string Path, long Bytes, LeftoverClass Class, string Evidence);

    /// <summary>
    /// Enumerates leftover candidates for an app (read-only): matching folders under the standard roots, Run/RunOnce
    /// values pointing at the app's exe, and services/tasks whose binary path is under the install folder.
    /// Each candidate carries its A/B/C class. Nothing is removed here.
    /// </summary>
    public static List<LeftoverCandidate> CandidatesFor(InstalledApp app, IEnumerable<InstalledApp> installed)
    {
        var result = new List<LeftoverCandidate>();
        var installedList = installed.ToList();
        var norm = Norm(app.Name);

        // Folders under the standard roots whose normalized name matches the app.
        foreach (var root in Roots(null))
        {
            if (!Directory.Exists(root)) continue;
            try
            {
                foreach (var dir in Directory.EnumerateDirectories(root))
                {
                    var name = Path.GetFileName(dir);
                    if (Norm(name) == norm || Norm(name).Contains(norm))
                    {
                        var (cls, ev) = Classify(dir, app, installedList);
                        long bytes = 0; int files = 0;
                        try { foreach (var f in Scanner.Files(dir)) { try { bytes += new FileInfo(f).Length; files++; } catch { } } } catch { }
                        result.Add(new LeftoverCandidate("Folder", dir, bytes, cls, ev));
                    }
                }
            }
            catch { }
        }

        // Run / RunOnce values pointing at the app's exe.
        if (!string.IsNullOrWhiteSpace(app.InstallLocation))
        {
            var exePrefix = app.InstallLocation.TrimEnd('\\') + "\\";
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
                    foreach (var value in key.GetValueNames())
                    {
                        var data = key.GetValue(value) as string;
                        if (string.IsNullOrWhiteSpace(data)) continue;
                        if (!data.Contains(exePrefix, StringComparison.OrdinalIgnoreCase)) continue;
                        result.Add(new LeftoverCandidate("Run value", $"{hive.Name}\\{path}\\{value}", 0, LeftoverClass.A,
                            "Run/RunOnce value points at the removed app's executable."));
                    }
                }
                catch { }
            }
        }

        // Services whose binary path is under the install folder.
        if (!string.IsNullOrWhiteSpace(app.InstallLocation))
        {
            var prefix = app.InstallLocation.TrimEnd('\\') + "\\";
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services");
                if (key is not null)
                {
                    foreach (var name in key.GetSubKeyNames())
                    {
                        try
                        {
                            using var s = key.OpenSubKey(name);
                            var img = s?.GetValue("ImagePath") as string;
                            if (string.IsNullOrWhiteSpace(img)) continue;
                            if (!img.Contains(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                            result.Add(new LeftoverCandidate("Service", name, 0, LeftoverClass.B,
                                "Service ImagePath is under the app's install folder (report only; removal via SCM)."));
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        return result;
    }

    /// <summary>Exports a registry key to a .reg file before any removal. Returns true when the export succeeded and the file is readable.</summary>
    public static bool RegExport(string keyPath, string outFile)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outFile)!);
            // reg.exe expects the hive in the path (e.g. HKLM\SOFTWARE\...); pass through as-is.
            var psi = new ProcessStartInfo("reg.exe", $"export \"{keyPath}\" \"{outFile}\" /y")
            { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            using var p = Process.Start(psi);
            if (p is null) return false;
            if (!p.WaitForExit(30000)) return false;
            if (p.ExitCode != 0) return false;
            // Re-parse the export to make sure it is readable (playbook-v2 10.3 step 2).
            return File.Exists(outFile) && new FileInfo(outFile).Length > 0 && File.ReadAllText(outFile).Contains("Windows Registry Editor");
        }
        catch { return false; }
    }
}

public static class OldFiles
{
    /// <summary>Files not modified for `months`, grouped by folder `depth` levels below the drive.</summary>
    public static List<FolderReport> Find(string root, int months, int depth, IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        var cutoff = DateTime.Now.AddMonths(-months);
        var winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var groups = new Dictionary<string, (long Bytes, int Files, DateTime Newest)>(StringComparer.OrdinalIgnoreCase);
        var paths = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var seen = 0;
        foreach (var f in Scanner.Files(root))
        {
            ct.ThrowIfCancellationRequested();
            if (f.StartsWith(winDir, StringComparison.OrdinalIgnoreCase)) continue;
            if (++seen % 5000 == 0) progress?.Report(seen);
            FileInfo fi;
            try { fi = new FileInfo(f); if (fi.LastWriteTime >= cutoff) continue; } catch { continue; }
            var parts = f.Split('\\');
            var key = string.Join('\\', parts.Take(Math.Min(depth + 1, parts.Length - 1)));
            groups.TryGetValue(key, out var g);
            groups[key] = (g.Bytes + fi.Length, g.Files + 1, fi.LastWriteTime > g.Newest ? fi.LastWriteTime : g.Newest);
            if (!paths.TryGetValue(key, out var list)) paths[key] = list = new List<string>();
            list.Add(f);
        }
        return groups.Select(kv => new FolderReport(kv.Key, kv.Value.Bytes, kv.Value.Files, kv.Value.Newest, paths[kv.Key]))
            .OrderByDescending(r => r.Bytes).ToList();
    }
}

public static class LargeFiles
{
    public static List<FileItem> Find(string root, int top, long minBytes, CancellationToken ct = default)
    {
        var heap = new PriorityQueue<FileItem, long>();
        foreach (var f in Scanner.Files(root))
        {
            ct.ThrowIfCancellationRequested();
            long len;
            try { len = new FileInfo(f).Length; } catch { continue; }
            if (len < minBytes) continue;
            heap.Enqueue(new FileItem(f, len), len);
            if (heap.Count > top) heap.Dequeue(); // keeps the largest `top`
        }
        var list = new List<FileItem>();
        while (heap.Count > 0) list.Add(heap.Dequeue());
        list.Reverse();
        return list;
    }
}

public sealed record DuplicateGroup(string Hash, long Size, List<string> Paths)
{
    public long Wasted => Size * (Paths.Count - 1);
}

public static class Duplicates
{
    /// <summary>Groups by size first, then by SHA-256, so only same-size files are hashed.</summary>
    public static List<DuplicateGroup> Find(string root, long minBytes, IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var bySize = new Dictionary<long, List<string>>();
        foreach (var f in Scanner.Files(root))
        {
            ct.ThrowIfCancellationRequested();
            long len;
            try { len = new FileInfo(f).Length; } catch { continue; }
            if (len < minBytes) continue;
            if (!bySize.TryGetValue(len, out var l)) bySize[len] = l = new List<string>();
            l.Add(f);
        }
        var groups = new List<DuplicateGroup>();
        foreach (var (size, paths) in bySize.Where(kv => kv.Value.Count > 1))
        {
            var byHash = new Dictionary<string, List<string>>();
            foreach (var p in paths)
            {
                ct.ThrowIfCancellationRequested();
                progress?.Report(p);
                string hash;
                try { using var s = File.OpenRead(p); hash = Convert.ToHexString(SHA256.HashData(s)); }
                catch { continue; }
                if (!byHash.TryGetValue(hash, out var l)) byHash[hash] = l = new List<string>();
                l.Add(p);
            }
            groups.AddRange(byHash.Where(kv => kv.Value.Count > 1).Select(kv => new DuplicateGroup(kv.Key, size, kv.Value)));
        }
        return groups.OrderByDescending(g => g.Wasted).ToList();
    }
}
