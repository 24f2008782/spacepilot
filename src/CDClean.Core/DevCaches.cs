using System.Diagnostics;

namespace CDClean.Core;

/// <summary>Developer cache manager (F-21): finds node_modules / target / __pycache__ / .gradle folders. Read-only listing + documented prune commands. Never touches source.</summary>
public sealed record DevCache(string Kind, string Path, long Bytes);

public static class DevCaches
{
    private static readonly (string Name, string Kind)[] Targets = new[]
    {
        ("node_modules", "Node modules"),
        ("target", "Rust/Java target"),
        ("__pycache__", "Python cache"),
        (".gradle", "Gradle cache"),
        ("bin", "Build output"),
        ("obj", "Build output"),
        (".next", "Next.js cache"),
        (".nuxt", "Nuxt cache"),
        ("dist", "Dist output"),
    };

    public static List<DevCache> Find(string root, CancellationToken ct = default)
    {
        var result = new List<DevCache>();
        if (!Directory.Exists(root)) return result;
        var kinds = Targets.ToDictionary(t => t.Name, t => t.Kind, StringComparer.OrdinalIgnoreCase);
        var stack = new Stack<string>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            string[] children;
            try { children = Directory.GetDirectories(stack.Pop()); } catch { continue; }
            foreach (var dir in children)
            {
                try
                {
                    if ((File.GetAttributes(dir) & FileAttributes.ReparsePoint) != 0) continue;
                    if (!kinds.TryGetValue(Path.GetFileName(dir), out var kind)) { stack.Push(dir); continue; }
                    long bytes = 0;
                    foreach (var f in Scanner.Files(dir)) try { bytes += new FileInfo(f).Length; } catch { }
                    result.Add(new DevCache(kind, dir, bytes));
                    // A cache inside a cache is part of the outer one; do not list or scan it again.
                }
                catch { }
            }
        }
        return result.OrderByDescending(d => d.Bytes).ToList();
    }

    /// <summary>Documented prune commands for a cache kind. Never runs them; the user runs them.</summary>
    public static string PruneCommand(string kind) => kind switch
    {
        "Node modules" => "npm prune  (or: rimraf node_modules && npm install)",
        "Rust/Java target" => "cargo clean  (or: mvn clean)",
        "Python cache" => "find . -type d -name __pycache__ -exec rm -rf {} +",
        "Gradle cache" => "./gradlew clean",
        "Build output" => "dotnet clean",
        "Next.js cache" => "rm -rf .next",
        "Nuxt cache" => "rm -rf .nuxt",
        "Dist output" => "rm -rf dist",
        _ => "Delete the folder manually after confirming nothing references it.",
    };
}
