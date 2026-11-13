namespace CDClean.Core;

/// <summary>One searchable setting. Dangerous settings require the session ADVANCED unlock before they can change.</summary>
public sealed record SettingInfo(string Category, string Name, string Description, bool Dangerous, string[] Keywords);

/// <summary>
/// The 14 Settings categories and the settings that have a backing field in this batch.
/// Decision (approved 2026-11-12): categories without a backing field stay as placeholders; we do not invent
/// settings here. Dangerous settings are a custom RulesPath, clearing ProtectedPaths, and VaultCapGb = 0.
/// </summary>
public static class SettingsSearch
{
    public static readonly string[] Categories =
    {
        "General", "Appearance", "Scan", "Drives", "Cleanup rules", "Uninstall",
        "Registry safety", "Exclusions & protected", "Backups & recovery", "Performance",
        "Notifications", "Privacy & diagnostics", "Advanced", "About & updates",
    };

    public static readonly IReadOnlyList<SettingInfo> All = new SettingInfo[]
    {
        new("General", "General", "General application behavior is reserved for a later batch.", false, new[] { "general", "startup", "language" }),
        new("Appearance", "Appearance", "Theme and layout settings are reserved for a later batch.", false, new[] { "appearance", "theme", "color", "layout" }),
        new("Scan", "Scan scopes", "Each page remembers its last scan scope. No editable scan setting exists in this batch.", false, new[] { "scan", "scope", "folder", "drive" }),
        new("Drives", "Drives", "Drive selection settings are reserved for a later batch.", false, new[] { "drives", "drive", "volume" }),
        new("Cleanup rules", "Rules file", "Custom rules.toml path. Changing it is an advanced action.", true, new[] { "rules", "toml", "path", "custom" }),
        new("Cleanup rules", "Enabled rules", "Rules ticked on the Rules page. Scheduled runs still execute only Safe rules.", false, new[] { "rules", "enabled", "safe", "manual" }),
        new("Uninstall", "Uninstall", "Uninstall settings are reserved for a later batch.", false, new[] { "uninstall", "apps" }),
        new("Registry safety", "Registry safety", "Registry safety settings are reserved for a later batch.", false, new[] { "registry", "safety" }),
        new("Exclusions & protected", "Protected paths", "Extra folders that are never offered for deletion. Clearing the list is an advanced action.", true, new[] { "protected", "exclusions", "folders", "paths" }),
        new("Backups & recovery", "Backup folder", "Where the backup vault stores files.", false, new[] { "backup", "vault", "folder", "drive" }),
        new("Backups & recovery", "Keep backups for", "Backup retention in days.", false, new[] { "backup", "retention", "days", "prune" }),
        new("Backups & recovery", "Vault size cap", "Vault size cap in GB. 0 removes the cap and is an advanced action.", true, new[] { "vault", "cap", "size", "gb" }),
        new("Performance", "Performance", "Performance settings are reserved for a later batch.", false, new[] { "performance", "memory", "workers" }),
        new("Notifications", "Low-space warning", "Warn when C: free space is below this many GB.", false, new[] { "low", "space", "warning", "gb", "notification" }),
        new("Privacy & diagnostics", "Redact audit paths", "Hide paths in the local audit log.", false, new[] { "audit", "privacy", "redact", "paths", "diagnostics" }),
        new("Advanced", "Advanced risk settings", "Unlock custom rules, clearing protected paths, and removing the vault cap.", true, new[] { "advanced", "danger", "unlock", "risk" }),
        new("About & updates", "Version", "Installed SpacePilot version.", false, new[] { "about", "version", "update" }),
        new("About & updates", "Logs and notices", "Open the logs folder and third-party notices.", false, new[] { "logs", "notices", "third-party", "privacy" }),
    };

    public static IEnumerable<SettingInfo> Find(string query)
    {
        var terms = query.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (terms.Length == 0) return All;
        return All
            .Where(s => terms.All(term => Matches(s, term)))
            .OrderBy(s => Array.IndexOf(Categories, s.Category))
            .ThenBy(s => s.Name);
    }

    private static bool Matches(SettingInfo setting, string term) =>
        setting.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
        || setting.Description.Contains(term, StringComparison.OrdinalIgnoreCase)
        || setting.Category.Contains(term, StringComparison.OrdinalIgnoreCase)
        || setting.Keywords.Any(k => k.Contains(term, StringComparison.OrdinalIgnoreCase));
}
