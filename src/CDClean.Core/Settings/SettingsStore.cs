using System.Reflection;
using System.Text.Json;

namespace CDClean.Core;

/// <summary>The result of reading settings, including a user-facing notice when the file could not be used.</summary>
public sealed record SettingsLoadResult(AppSettings Settings, bool Recovered, string? Notice);

/// <summary>
/// Versioned, atomic settings persistence. A save writes a temporary file and swaps it in, keeping the
/// previous file as settings.json.bak. A corrupt or newer file falls back to defaults with a notice and is
/// left untouched until the user saves again.
/// </summary>
public static class SettingsStore
{
    public const int CurrentVersion = 1;
    public const string FileName = "settings.json";
    public const string BackupName = "settings.json.bak";

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private static readonly object Gate = new();

    public static string FilePath(string? dataDir = null) => Path.Combine(dataDir ?? AppSettings.DataDir, FileName);
    public static string BackupPath(string? dataDir = null) => Path.Combine(dataDir ?? AppSettings.DataDir, BackupName);

    public static SettingsLoadResult Load(string? dataDir = null)
    {
        var file = FilePath(dataDir);
        if (!File.Exists(file)) return new(new AppSettings(), false, null);

        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(file), Json)
                           ?? throw new JsonException("settings.json was empty.");
            if (settings.SchemaVersion > CurrentVersion)
            {
                return new(new AppSettings(), true,
                    $"settings.json was written by a newer version (schema {settings.SchemaVersion}). Defaults are in use; the file was not changed.");
            }

            settings.SchemaVersion = CurrentVersion;
            return new(settings, false, null);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return new(new AppSettings(), true,
                "settings.json could not be read. Defaults are in use; the existing file was not changed. A .bak copy is kept next to it.");
        }
    }

    public static void Save(AppSettings settings, string? dataDir = null)
    {
        var dir = dataDir ?? AppSettings.DataDir;
        var file = Path.Combine(dir, FileName);
        var backup = Path.Combine(dir, BackupName);
        var temp = file + ".tmp";

        lock (Gate)
        {
            Directory.CreateDirectory(dir);
            settings.SchemaVersion = CurrentVersion;
            File.WriteAllText(temp, JsonSerializer.Serialize(settings, Json));
            try
            {
                if (!File.Exists(file))
                {
                    File.Move(temp, file);
                    return;
                }

                try
                {
                    File.Replace(temp, file, backup);
                }
                catch (Exception ex) when (ex is IOException or PlatformNotSupportedException)
                {
                    // Some file systems do not support File.Replace; keep the same backup contract.
                    File.Copy(file, backup, overwrite: true);
                    File.Move(temp, file, overwrite: true);
                }
            }
            finally
            {
                try { if (File.Exists(temp)) File.Delete(temp); } catch { /* temp cleanup must not hide the save error */ }
            }
        }
    }

    public static AppSettings Clone(AppSettings settings) =>
        JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(settings, Json), Json) ?? new AppSettings();

    public static void CopyFrom(AppSettings source, AppSettings target) =>
        Copy(source, target, typeof(AppSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite));

    public static void ResetAll(AppSettings target) => CopyFrom(new AppSettings(), target);

    /// <summary>Resets only the fields owned by a category. Returns false for placeholder categories.</summary>
    public static bool ResetCategory(AppSettings target, string category)
    {
        var defaults = new AppSettings();
        switch (category)
        {
            case "Cleanup rules":
                target.RulesPath = defaults.RulesPath;
                target.EnabledRules = new List<string>(defaults.EnabledRules);
                target.KeepFolders = new List<string>(defaults.KeepFolders);
                return true;
            case "Exclusions & protected":
                target.ProtectedPaths = new List<string>(defaults.ProtectedPaths);
                return true;
            case "Backups & recovery":
                target.BackupRoot = defaults.BackupRoot;
                target.KeepDays = defaults.KeepDays;
                target.VaultCapGb = defaults.VaultCapGb;
                return true;
            case "Notifications":
                target.LowSpaceGb = defaults.LowSpaceGb;
                return true;
            case "Privacy & diagnostics":
                target.RedactAudit = defaults.RedactAudit;
                return true;
            case "Advanced":
                target.RulesPath = defaults.RulesPath;
                target.ProtectedPaths = new List<string>(defaults.ProtectedPaths);
                target.VaultCapGb = defaults.VaultCapGb;
                return true;
            default:
                return false;
        }
    }

    public static bool ResetField(AppSettings target, string field)
    {
        var property = typeof(AppSettings).GetProperty(field, BindingFlags.Public | BindingFlags.Instance);
        if (property?.CanWrite != true) return false;
        property.SetValue(target, property.GetValue(new AppSettings()));
        return true;
    }

    private static void Copy(AppSettings source, AppSettings target, IEnumerable<PropertyInfo> properties)
    {
        foreach (var property in properties)
        {
            var value = property.GetValue(source);
            value = value switch
            {
                List<string> list => new List<string>(list),
                Dictionary<string, GridLayout> layouts => layouts.ToDictionary(pair => pair.Key, pair => pair.Value),
                Dictionary<string, string> scopes => new Dictionary<string, string>(scopes),
                int[] rect => (int[])rect.Clone(),
                _ => value,
            };
            property.SetValue(target, value);
        }
    }
}
