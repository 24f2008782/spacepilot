using Tomlyn;
using Tomlyn.Model;

namespace CDClean.Core;

/// <summary>One folder to clean, as written in rules.toml.</summary>
public sealed record Rule(
    string Name,
    string Path,
    IReadOnlyList<string> Include,
    int? Depth,
    int? OlderThanDays,
    string Action,
    bool Safe,
    string Explain = "");

public static class Rules
{
    /// <summary>Expands %VAR% references. Unknown variables are left as-is.</summary>
    public static string Expand(string path) => Environment.ExpandEnvironmentVariables(path);

    /// <summary>WinUI text boxes expose CR-only line breaks; TOML requires LF.</summary>
    public static string NormalizeNewlines(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n');

    public static List<Rule> Parse(string text)
    {
        var model = Toml.ToModel(NormalizeNewlines(text));
        var list = new List<Rule>();
        if (!model.TryGetValue("rule", out var raw) || raw is not TomlTableArray rules)
            return list;

        foreach (TomlTable t in rules)
        {
            string Str(string key, string fallback = "") =>
                t.TryGetValue(key, out var v) && v is string s ? s : fallback;
            int? Int(string key) =>
                t.TryGetValue(key, out var v) && v is long l ? (int)l : null;

            var name = Str("name");
            var path = Str("path");
            if (name.Length == 0 || path.Length == 0)
                throw new FormatException("Every rule needs a name and a path.");

            var action = Str("action", "delete");
            if (action is not ("delete" or "archive"))
                throw new FormatException($"Rule '{name}': action must be \"delete\" or \"archive\".");

            var include = t.TryGetValue("include", out var inc) && inc is TomlArray arr
                ? arr.Select(x => x?.ToString() ?? "").Where(x => x.Length > 0).ToList()
                : new List<string>();

            var safe = !t.TryGetValue("safe", out var sv) || sv is not bool b || b;
            list.Add(new Rule(name, path, include, Int("depth"), Int("older_than_days"), action, safe, Str("explain")));
        }
        return list;
    }

    public static List<Rule> Load(string file) => Parse(File.ReadAllText(file));
}

/// <summary>Files and folders that are never touched, whatever a rule says.</summary>
public static class Protected
{
    private static readonly HashSet<string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        "Cookies", "Cookies-journal", "Login Data", "Login Data-journal", "History",
        "Web Data", "Bookmarks", "Local State", "Preferences", "key4.db", "logins.json",
    };

    public static bool IsProtected(string path) =>
        path.Contains("Claude-Data", StringComparison.OrdinalIgnoreCase)
        || Names.Contains(System.IO.Path.GetFileName(path));
}
