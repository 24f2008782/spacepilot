using CDClean.Core;
using Xunit;

public class SettingsTests
{
    private static string NewDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "cdclean-settings-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void Cleanup(string dir)
    {
        try { Directory.Delete(dir, recursive: true); } catch { }
    }

    [Fact]
    public void Save_round_trips_and_keeps_previous_file_as_bak()
    {
        var dir = NewDir();
        try
        {
            var settings = new AppSettings { BackupRoot = @"D:\vault", KeepDays = 7, LowSpaceGb = 42 };
            SettingsStore.Save(settings, dir);
            Assert.True(File.Exists(SettingsStore.FilePath(dir)));
            Assert.False(File.Exists(SettingsStore.BackupPath(dir)));

            var loaded = SettingsStore.Load(dir);
            Assert.False(loaded.Recovered);
            Assert.Null(loaded.Notice);
            Assert.Equal(@"D:\vault", loaded.Settings.BackupRoot);
            Assert.Equal(7, loaded.Settings.KeepDays);
            Assert.Equal(42, loaded.Settings.LowSpaceGb);
            Assert.Equal(SettingsStore.CurrentVersion, loaded.Settings.SchemaVersion);

            settings.KeepDays = 14;
            SettingsStore.Save(settings, dir);
            Assert.True(File.Exists(SettingsStore.BackupPath(dir)));
            Assert.Contains("\"KeepDays\": 7", File.ReadAllText(SettingsStore.BackupPath(dir)));
            Assert.Equal(14, SettingsStore.Load(dir).Settings.KeepDays);
        }
        finally { Cleanup(dir); }
    }

    [Fact]
    public void Corrupt_file_falls_back_to_defaults_with_notice()
    {
        var dir = NewDir();
        try
        {
            var file = SettingsStore.FilePath(dir);
            File.WriteAllText(file, "{ this is not json");
            var result = SettingsStore.Load(dir);
            Assert.True(result.Recovered);
            Assert.NotNull(result.Notice);
            Assert.Contains("could not be read", result.Notice!);
            Assert.Equal(30, result.Settings.KeepDays);
            Assert.Equal("{ this is not json", File.ReadAllText(file));
        }
        finally { Cleanup(dir); }
    }

    [Fact]
    public void Newer_schema_falls_back_to_defaults_with_notice()
    {
        var dir = NewDir();
        try
        {
            var settings = new AppSettings { SchemaVersion = SettingsStore.CurrentVersion + 1, KeepDays = 5 };
            File.WriteAllText(SettingsStore.FilePath(dir), System.Text.Json.JsonSerializer.Serialize(settings));
            var result = SettingsStore.Load(dir);
            Assert.True(result.Recovered);
            Assert.Contains("newer version", result.Notice!);
            Assert.Equal(30, result.Settings.KeepDays);
        }
        finally { Cleanup(dir); }
    }

    [Fact]
    public void Reset_field_category_and_all_use_defaults()
    {
        var settings = new AppSettings
        {
            BackupRoot = @"D:\vault",
            KeepDays = 7,
            VaultCapGb = 10,
            LowSpaceGb = 12,
            RulesPath = @"D:\rules.toml",
            ProtectedPaths = new() { @"D:\keep" },
            RedactAudit = true,
        };

        Assert.True(SettingsStore.ResetField(settings, nameof(AppSettings.LowSpaceGb)));
        Assert.Equal(60, settings.LowSpaceGb);

        Assert.True(SettingsStore.ResetCategory(settings, "Backups & recovery"));
        Assert.Equal(30, settings.KeepDays);
        Assert.Equal(0, settings.VaultCapGb);
        Assert.Equal(@"E:\cdclean-backups", settings.BackupRoot);
        Assert.False(SettingsStore.ResetCategory(settings, "Appearance"));

        Assert.True(SettingsStore.ResetCategory(settings, "Advanced"));
        Assert.Equal("", settings.RulesPath);
        Assert.Empty(settings.ProtectedPaths);

        settings.LowSpaceGb = 1;
        SettingsStore.ResetAll(settings);
        Assert.Equal(60, settings.LowSpaceGb);
        Assert.Equal(30, settings.KeepDays);
        Assert.False(settings.RedactAudit);
    }

    [Fact]
    public void Search_finds_settings_by_name_and_category()
    {
        Assert.Contains(SettingsSearch.Find("vault"), s => s.Name == "Vault size cap");
        Assert.Contains(SettingsSearch.Find("PROTECTED"), s => s.Name == "Protected paths");
        Assert.Contains(SettingsSearch.Find("low space"), s => s.Name == "Low-space warning");
        Assert.Contains(SettingsSearch.Find("rules"), s => s.Name == "Rules file");
        Assert.Contains(SettingsSearch.Find("appearance"), s => s.Category == "Appearance");
        Assert.All(SettingsSearch.Find("backups"), s => Assert.Equal("Backups & recovery", s.Category));
        Assert.Empty(SettingsSearch.Find("no-such-setting"));
    }
}
