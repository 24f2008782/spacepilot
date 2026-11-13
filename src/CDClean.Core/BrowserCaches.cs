using System.Diagnostics;

namespace CDClean.Core;

/// <summary>Browser cache analyser (F-04): cache folders for Chrome/Edge/Brave/Firefox. Cache only; browser must be closed.</summary>
public static class BrowserCaches
{
    public sealed record BrowserCache(string Browser, string Path, long Bytes);

    public static List<BrowserCache> Find(CancellationToken ct = default)
    {
        var result = new List<BrowserCache>();
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        foreach (var (browser, rels) in new[]
        {
            ("Chrome", new[] { @"Google\Chrome\User Data\Default\Cache" }),
            ("Edge", new[] { @"Microsoft\Edge\User Data\Default\Cache" }),
            ("Brave", new[] { @"BraveSoftware\Brave-Browser\User Data\Default\Cache" }),
        })
        {
            foreach (var rel in rels)
            {
                var dir = Path.Combine(local, rel);
                if (!Directory.Exists(dir)) continue;
                long bytes = 0;
                try { foreach (var f in Scanner.Files(dir)) try { bytes += new FileInfo(f).Length; } catch { } } catch { }
                result.Add(new BrowserCache(browser, dir, bytes));
            }
        }
        // Firefox: sum Cache directories under each profile.
        var ffRoot = Path.Combine(local, @"Mozilla\Firefox\Profiles");
        if (Directory.Exists(ffRoot))
        {
            try
            {
                foreach (var profile in Directory.EnumerateDirectories(ffRoot))
                {
                    ct.ThrowIfCancellationRequested();
                    var cache = Path.Combine(profile, "cache2");
                    if (!Directory.Exists(cache)) continue;
                    long bytes = 0;
                    try { foreach (var f in Scanner.Files(cache)) try { bytes += new FileInfo(f).Length; } catch { } } catch { }
                    result.Add(new BrowserCache("Firefox", cache, bytes));
                }
            }
            catch { }
        }
        return result.OrderByDescending(b => b.Bytes).ToList();
    }

    public static bool IsBrowserRunning(string browser)
    {
        var names = browser switch
        {
            "Chrome" => new[] { "chrome" },
            "Edge" => new[] { "msedge" },
            "Brave" => new[] { "brave" },
            "Firefox" => new[] { "firefox" },
            _ => Array.Empty<string>(),
        };
        foreach (var n in names)
            if (Process.GetProcessesByName(n).Length > 0) return true;
        return false;
    }
}
