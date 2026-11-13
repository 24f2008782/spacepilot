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
