using System.Text;
using CDClean.Core;
using CDClean.Core.Safety;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace CDClean.App.Views;

public sealed partial class OrganizerPage : Page
{
    private List<PlannedMove> _plan = new();
    private static string LogDir => Path.Combine(AppSettings.DataDir, "organize");

    public OrganizerPage()
    {
        InitializeComponent();
        Root.Text = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
    }

    private async void BrowseBtn_Click(object sender, RoutedEventArgs e) { if (await Ui.PickFolder() is { } p) Root.Text = p; }

    private void PreviewBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!Directory.Exists(Root.Text)) { Status.Text = "Folder not found."; return; }
        _plan = Organizer.Plan(Root.Text);
        var counts = _plan.GroupBy(m => m.Category).ToDictionary(g => g.Key, g => g.Count());
        var max = counts.Count == 0 ? 0 : counts.Values.Max();
        List.ItemsSource = _plan.GroupBy(m => m.Category).OrderByDescending(g => g.Count()).SelectMany(g =>
            new[] { new Row { Title = $"{g.Key}  ({g.Count()} files)", Bar = Row.BarOf(g.Count(), max), Size = $"{g.Count()}" } }
                .Concat(g.Take(15).Select(m => new Row { Title = "    " + Path.GetFileName(m.From), Detail = "→ " + m.To }))).ToList();
        Status.Text = _plan.Count == 0 ? "No loose files to sort." : $"{_plan.Count} files would be moved into {counts.Count} folders. Press Organize to do it.";
    }

    private async void ApplyBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_plan.Count == 0) { Status.Text = "Press Preview first."; return; }
        if (!await Ui.Confirm(XamlRoot, "Organize folder?", $"{_plan.Count} files move into category folders. Undo last puts them back.", "Organize")) return;
        var (moved, failed) = Organizer.Apply(_plan, LogDir);
        _plan.Clear();
        List.ItemsSource = null;
        Status.Text = $"Moved {moved} files. {failed.Count} failed (in use).";
    }

    private async void UndoBtn_Click(object sender, RoutedEventArgs e)
    {
        var log = Organizer.LatestLog(LogDir);
        if (log is null) { Status.Text = "Nothing to undo."; return; }
        if (!await Ui.Confirm(XamlRoot, "Undo last organize?", "Files move back to where they were.", "Undo")) return;
        Status.Text = $"Moved {Organizer.Undo(log)} files back.";
    }
}

public sealed partial class BackupsPage : Page
{
    private List<Row> _rows = new();

    public BackupsPage()
    {
        InitializeComponent();
        KeepDays.Value = State.Settings.KeepDays;
        Loaded += (_, _) => Load();
    }

    private void Load()
    {
        var runs = Backups.List(State.Settings.BackupRoot);
        var max = runs.Count == 0 ? 0 : runs.Max(r => r.Bytes);
        _rows = runs.Select(r => new Row
        {
            Title = r.Id, Detail = $"{r.Files:N0} files{(r.Restored ? " · restored" : "")}",
            Size = Fmt.Size(r.Bytes), Bar = Row.BarOf(r.Bytes, max), Tag = r,
        }).ToList();
        List.ItemsSource = _rows;
        Status.Text = $"{runs.Count} runs in {State.Settings.BackupRoot}.";
    }

    private List<BackupRun> Picked() => _rows.Where(r => r.Selected).Select(r => (BackupRun)r.Tag!).ToList();
    private void LoadBtn_Click(object sender, RoutedEventArgs e) => Load();

    private async void RestoreBtn_Click(object sender, RoutedEventArgs e)
    {
        var picked = Picked().Where(r => !r.Restored).ToList();
        if (picked.Count == 0) { Status.Text = "Tick a run that has not been restored."; return; }
        if (!await Ui.Confirm(XamlRoot, "Restore?", "Files go back to where they were. Existing files are not overwritten.", "Restore")) return;
        var msg = new StringBuilder();
        foreach (var r in picked)
            try { var (ok, skip) = await Task.Run(() => Backups.Restore(State.Settings.BackupRoot, r.Id)); msg.AppendLine($"{r.Id}: {ok} restored, {skip} already existed"); }
            catch (Exception ex) { msg.AppendLine($"{r.Id}: {ex.Message}"); }
        Load();
        Status.Text = msg.ToString();
    }

    private async void DeleteBtn_Click(object sender, RoutedEventArgs e)
    {
        var picked = Picked();
        if (picked.Count == 0) return;
        if (!await Ui.Confirm(XamlRoot, "Delete backups?", $"{picked.Count} runs are deleted for good. Their files can no longer be restored.", "Delete")) return;
        foreach (var r in picked) Backups.Delete(State.Settings.BackupRoot, r.Id);
        Load();
    }

    private async void PruneBtn_Click(object sender, RoutedEventArgs e)
    {
        var days = (int)KeepDays.Value;
        var old = Backups.Prune(State.Settings.BackupRoot, days, dryRun: true);
        if (old.Count == 0) { Status.Text = $"No runs older than {days} days."; return; }
        if (!await Ui.Confirm(XamlRoot, "Prune?", $"Delete {old.Count} runs older than {days} days?\n\n" + string.Join("\n", old), "Delete")) return;
        Backups.Prune(State.Settings.BackupRoot, days, dryRun: false);
        Load();
    }
}

public sealed partial class SchedulePage : Page
{
    private bool _loading = true;

    public SchedulePage()
    {
        InitializeComponent();
        WeeklyTime.Time = DailyTime.Time = new TimeSpan(10, 0, 0);
        WeeklySwitch.IsOn = Schedule.Exists("weekly");
        DailySwitch.IsOn = Schedule.Exists("daily");
        _loading = false;
    }

    private async void Switch_Toggled(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        var weekly = ReferenceEquals(sender, WeeklySwitch);
        var sw = (ToggleSwitch)sender;
        var on = sw.IsOn;
        var time = (weekly ? WeeklyTime : DailyTime).Time;
        var exe = Environment.ProcessPath;
        try
        {
            if (string.IsNullOrWhiteSpace(exe)) throw new InvalidOperationException("Could not locate CDClean.exe.");
            await Task.Run(() => Schedule.Set(weekly ? "weekly" : "daily", on, $"{time.Hours:00}:{time.Minutes:00}", exe!));
            Status.Text = on ? $"{(weekly ? "Weekly" : "Daily")} clean set for {time.Hours:00}:{time.Minutes:00}." : "Turned off.";
        }
        catch (Exception ex)
        {
            try
            {
                Directory.CreateDirectory(AppSettings.LogDir);
                File.AppendAllText(Path.Combine(AppSettings.LogDir, "schedule.log"), $"{DateTime.Now:s} {ex}\n");
            }
            catch { }
            Status.Text = "Failed: " + (string.IsNullOrWhiteSpace(ex.Message) ? ex.GetType().Name : ex.Message);
            _loading = true; sw.IsOn = !on; _loading = false;
        }
    }

    private async void RunNowBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!await Ui.Confirm(XamlRoot, "Run now?", "Cleans the safe ticked rules now. Manual rules are queued for review, the same way the schedule does.", "Run")) return;
        RunNowBtn.IsEnabled = false;
        Status.Text = "Running...";
        try
        {
            var s = await Task.Run(() => Cleaner.RunScheduled(Scanner.ScanAll(State.LoadRules()), State.IsAutoRule, State.Settings.BackupRoot, policy: State.Settings.Policy()));
            Status.Text = $"Done: {Fmt.Size(s.Bytes)}. {s.Deleted:N0} deleted, {s.Archived:N0} archived, {s.Failed.Count:N0} skipped." +
                          (s.Queued > 0 ? $" {s.Queued:N0} manual item(s) queued for review." : "");
        }
        catch (Exception ex) { Status.Text = "Failed: " + ex.Message; }
        finally { RunNowBtn.IsEnabled = true; }
    }
}

public sealed partial class RulesPage : Page
{
    private List<Row> _rows = new();

    public RulesPage()
    {
        InitializeComponent();
        Load();
    }

    private void Load()
    {
        try
        {
            Editor.Text = File.ReadAllText(State.Settings.ResolvedRulesPath);
            _rows = State.LoadRules().Select(r => new Row
            {
                Title = r.Name + (r.Safe ? "" : "  (manual)"),
                Detail = $"{r.Action} · {r.Path}" + (r.OlderThanDays is int d && d > 0 ? $" · older than {d} days" : ""),
                Selected = State.IsAutoRule(r), Tag = r,
            }).ToList();
            List.ItemsSource = _rows;
            Status.Text = State.Settings.ResolvedRulesPath;
        }
        catch (Exception ex) { Status.Text = "Rules file problem: " + ex.Message; }
    }

    private void SaveTicksBtn_Click(object sender, RoutedEventArgs e)
    {
        State.Settings.EnabledRules = _rows.Where(r => r.Selected).Select(r => ((Rule)r.Tag!).Name).ToList();
        State.Settings.Save();
        Status.Text = $"{State.Settings.EnabledRules.Count} rules selected. The schedule runs safe rules and queues manual ones for review.";
    }

    private void SafeBtn_Click(object sender, RoutedEventArgs e)
    {
        State.Settings.EnabledRules = new();
        State.Settings.Save();
        Load();
    }

    private void SaveFileBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var text = Rules.NormalizeNewlines(Editor.Text);
            Rules.Parse(text); // refuse to save a broken file
            File.WriteAllText(State.Settings.ResolvedRulesPath, text);
            Load();
            Status.Text = "Saved.";
        }
        catch (Exception ex) { Status.Text = "Not saved: " + ex.Message; }
    }

    private void ReloadBtn_Click(object sender, RoutedEventArgs e) => Load();
}

public sealed partial class SettingsPage : Page
{
    private static readonly string[] Categories = new[]
    {
        "General", "Appearance", "Scan", "Drives", "Cleanup rules", "Uninstall",
        "Registry safety", "Exclusions & protected", "Backups & recovery", "Performance",
        "Notifications", "Privacy & diagnostics", "Advanced", "About & updates",
    };

    private string _selected = "General";
    private readonly Dictionary<string, FrameworkElement> _panels = new();

    public SettingsPage()
    {
        InitializeComponent();
        CategoryList.ItemsSource = Categories;
        CategoryList.SelectedIndex = 0;
        BuildPanel(_selected);
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var q = SearchBox.Text.Trim();
        CategoryList.ItemsSource = string.IsNullOrEmpty(q)
            ? Categories
            : Categories.Where(c => c.Contains(q, StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    private void CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryList.SelectedItem is string c) { _selected = c; BuildPanel(c); }
    }

    private void BuildPanel(string category)
    {
        ContentPanel.Children.Clear();
        ContentPanel.Children.Add(new TextBlock { Text = category, Style = (Style)Application.Current.Resources["TitleTextBlockStyle"] });

        switch (category)
        {
            case "Backups & recovery":
                BuildBackupsPanel();
                break;
            case "Cleanup rules":
                BuildRulesPanel();
                break;
            case "Notifications":
                BuildNotificationsPanel();
                break;
            case "Advanced":
                BuildAdvancedPanel();
                break;
            case "About & updates":
                BuildAboutPanel();
                break;
            default:
                ContentPanel.Children.Add(new TextBlock
                {
                    Text = $"{category} settings are coming in a later batch. Use the existing controls below where available.",
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                });
                break;
        }
    }

    private void BuildBackupsPanel()
    {
        var s = State.Settings;
        var root = new TextBox { Header = "Backup folder (use another drive)", Text = s.BackupRoot, Width = 420 };
        var browse = new Button { Content = "Browse", VerticalAlignment = VerticalAlignment.Bottom };
        browse.Click += async (_, _) => { if (await Ui.PickFolder() is { } p) root.Text = p; };
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        row.Children.Add(root); row.Children.Add(browse);
        ContentPanel.Children.Add(row);

        var keep = new NumberBox { Header = "Keep backups for (days)", Value = s.KeepDays, Minimum = 1, Maximum = 3650, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact, Width = 200, HorizontalAlignment = HorizontalAlignment.Left };
        ContentPanel.Children.Add(keep);

        var vaultCap = new NumberBox { Header = "Vault size cap (GB, 0 = no cap)", Value = s.VaultCapGb, Minimum = 0, Maximum = 10000, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact, Width = 200, HorizontalAlignment = HorizontalAlignment.Left };
        ContentPanel.Children.Add(vaultCap);

        var save = new Button { Content = "Save", Style = (Style)Application.Current.Resources["AccentButtonStyle"] };
        var status = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        save.Click += (_, _) =>
        {
            var r = root.Text.Trim();
            if (r.StartsWith("C:", StringComparison.OrdinalIgnoreCase)) { status.Text = "Pick a folder on another drive, so backups don't use C:."; return; }
            var ss = State.Settings;
            ss.BackupRoot = r;
            ss.KeepDays = (int)keep.Value;
            ss.VaultCapGb = (int)vaultCap.Value;
            ss.Save();
            status.Text = "Saved.";
        };
        var saveRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        saveRow.Children.Add(save); saveRow.Children.Add(status);
        ContentPanel.Children.Add(saveRow);
    }

    private void BuildRulesPanel()
    {
        var s = State.Settings;
        var rules = new TextBox { Header = "Rules file (empty = rules.toml next to the app)", Text = s.RulesPath, Width = 520, HorizontalAlignment = HorizontalAlignment.Left };
        var save = new Button { Content = "Save", Style = (Style)Application.Current.Resources["AccentButtonStyle"] };
        var status = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        save.Click += (_, _) =>
        {
            if (rules.Text.Trim().Length > 0 && !File.Exists(rules.Text.Trim())) { status.Text = "Rules file not found."; return; }
            State.Settings.RulesPath = rules.Text.Trim();
            State.Settings.Save();
            status.Text = "Saved.";
        };
        ContentPanel.Children.Add(rules);
        var saveRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        saveRow.Children.Add(save); saveRow.Children.Add(status);
        ContentPanel.Children.Add(saveRow);
    }

    private void BuildNotificationsPanel()
    {
        var s = State.Settings;
        var low = new NumberBox { Header = "Warn when C: free space is below (GB)", Value = s.LowSpaceGb, Minimum = 1, Maximum = 2000, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact, Width = 300, HorizontalAlignment = HorizontalAlignment.Left };
        var save = new Button { Content = "Save", Style = (Style)Application.Current.Resources["AccentButtonStyle"] };
        save.Click += (_, _) => { State.Settings.LowSpaceGb = (int)low.Value; State.Settings.Save(); };
        ContentPanel.Children.Add(low);
        ContentPanel.Children.Add(save);
    }

    private void BuildAdvancedPanel()
    {
        ContentPanel.Children.Add(new Border
        {
            BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 0xC6, 0x28, 0x28)),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(10),
            Child = new TextBlock { Text = "Advanced risk settings. Changes here can make the app delete more. Type 'ADVANCED' to unlock.", TextWrapping = TextWrapping.Wrap },
        });
        var box = new TextBox { Header = "Type ADVANCED to unlock", Width = 260 };
        var unlock = new Button { Content = "Unlock", IsEnabled = false };
        box.TextChanged += (_, _) => unlock.IsEnabled = box.Text.Trim() == "ADVANCED";
        unlock.Click += async (_, _) => await Ui.Info(XamlRoot, "Unlocked", "Advanced settings are now editable (placeholder).");
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        row.Children.Add(box); row.Children.Add(unlock);
        ContentPanel.Children.Add(row);
    }

    private void BuildAboutPanel()
    {
        ContentPanel.Children.Add(new TextBlock
        {
            Text = $"SpacePilot {typeof(App).Assembly.GetName().Version?.ToString(3)}\nApp folder: {AppContext.BaseDirectory}\nData and logs: {AppSettings.DataDir}",
            TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
        });
        var logs = new Button { Content = "Open logs folder" };
        logs.Click += (_, _) => { Directory.CreateDirectory(AppSettings.LogDir); Ui.ShowInExplorer(AppSettings.LogDir); };
        ContentPanel.Children.Add(logs);
        var notices = new Button { Content = "Third-party notices" };
        notices.Click += (_, _) => Ui.ShowInExplorer(Path.Combine(AppContext.BaseDirectory, "THIRD-PARTY-NOTICES.md"));
        ContentPanel.Children.Add(notices);
    }

    private void ResetCategory_Click(object sender, RoutedEventArgs e)
    {
        // Reset the currently selected category's settings to defaults.
        var s = State.Settings;
        switch (_selected)
        {
            case "Backups & recovery":
                s.BackupRoot = @"E:\cdclean-backups"; s.KeepDays = 30; s.VaultCapGb = 0; break;
            case "Cleanup rules":
                s.RulesPath = ""; break;
            case "Notifications":
                s.LowSpaceGb = 60; break;
        }
        s.Save();
        BuildPanel(_selected);
    }

    private async void ResetAll_Click(object sender, RoutedEventArgs e)
    {
        if (!await Ui.Confirm(XamlRoot, "Reset all settings?", "Every setting returns to its default. This cannot be undone.", "Reset")) return;
        var defaults = new AppSettings();
        var s = State.Settings;
        s.BackupRoot = defaults.BackupRoot;
        s.KeepDays = defaults.KeepDays;
        s.LowSpaceGb = defaults.LowSpaceGb;
        s.RulesPath = defaults.RulesPath;
        s.VaultCapGb = defaults.VaultCapGb;
        s.KeepFolders = defaults.KeepFolders;
        s.EnabledRules = defaults.EnabledRules;
        s.ProtectedPaths = defaults.ProtectedPaths;
        s.RedactAudit = defaults.RedactAudit;
        s.Save();
        BuildPanel(_selected);
    }

    private void LogsBtn_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(AppSettings.LogDir);
        Ui.ShowInExplorer(AppSettings.LogDir);
    }
}
