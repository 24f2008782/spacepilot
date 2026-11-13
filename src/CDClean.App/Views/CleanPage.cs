using System.Text;
using System.Text.Json;
using CDClean.App.Controls;
using CDClean.Core;
using CDClean.Core.Safety;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace CDClean.App.Views;

/// <summary>Clean Run preview (playbook-v2 batch 1.2, report §4.12.1): preview by default, nothing deleted until reviewed.</summary>
public sealed partial class CleanPage : Page
{
    /// <summary>Rule names picked on the Scan page. Null means "the auto rules".</summary>
    public static HashSet<string>? Pending { get; set; }
    private CancellationTokenSource? _cts;
    private Dictionary<string, Rule> _rules = new();

    public CleanPage()
    {
        InitializeComponent();
        Table.CurrentChanged += (_, i) => ShowExplain(i);
        Table.RowInvoked += (_, i) => Ui.ShowInExplorer(i.Path);
        Loaded += (_, _) => Status.Text = Pending is { Count: > 0 }
            ? $"Rules picked on Scan: {string.Join(", ", Pending)}. Press Preview."
            : "Uses the rules ticked on the Rules page. Press Preview.";
    }

    private bool Include(Rule r) => Pending is { Count: > 0 } ? Pending.Contains(r.Name) : State.IsAutoRule(r);

    private void ShowExplain(GridItem? i)
    {
        if (i?.Tag is not PlanItem p || !_rules.TryGetValue(p.Target.Source, out var rule)) { Explain.IsOpen = false; return; }
        Explain.Title = rule.Name;
        Explain.Message = (rule.Explain.Length > 0 ? rule.Explain : "No explanation written for this rule yet.") +
                          $"  Action: {(rule.Action == "delete" ? "deleted (cache)" : "archived to your backup drive")}. Folder: {Rules.Expand(rule.Path)}";
        Explain.IsOpen = true;
    }

    private async void PreviewBtn_Click(object sender, RoutedEventArgs e)
    {
        _cts = new CancellationTokenSource();
        Busy.Visibility = Visibility.Visible;
        PreviewBtn.IsEnabled = CleanBtn.IsEnabled = false;
        try
        {
            Status.Text = "Scanning...";
            var rules = State.LoadRules();
            _rules = rules.ToDictionary(r => r.Name);
            State.LastScan = await Task.Run(() => Scanner.ScanAll(rules, _cts.Token));
            Status.Text = "Checking every file (identity, locks, risk)...";
            var targets = State.LastScan.Where(r => Include(r.Rule)).SelectMany(r => r.Items.Select(i => r.Rule.Action == "delete"
                ? new Target(i.Path, Method.Delete, Category.Cache, r.Rule.Name, IsDirectory: false)
                : new Target(i.Path, Method.Vault, Category.UserFile, r.Rule.Name, IsDirectory: false))).ToList();
            var pol = State.Settings.Policy();
            var plan = await Task.Run(() => Executor.Plan(targets, pol, _cts.Token, resolveFinalPath: false));
            var rows = await Task.Run(() => plan.Items.Select(p => new GridItem
            {
                Key = p.Facts.Exists ? p.Facts.Key : GridItem.IdKey(p.Facts.Path),
                Name = Path.GetFileName(p.Facts.Path), Type = "file", Path = p.Facts.Path, App = p.Target.Source,
                Size = p.Facts.Allocated, Modified = p.Facts.Exists ? p.Facts.LastWriteUtc.ToLocalTime() : null, Risk = p.Risk,
                Action = p.Verdict == Verdict.Block ? "Blocked" : p.Target.Method == Method.Delete ? "Delete" : "Archive",
                Extra = string.Join(", ", p.Reasons.Select(Gate.Text)), Checked = p.Verdict != Verdict.Block, Tag = p,
            }).ToList());
            Table.SetItems(rows, keepSelection: false);
            var c = new DriveInfo("C");
            Status.Text = $"{plan.Items.Count:N0} files from {plan.Items.Select(i => i.Target.Source).Distinct().Count()} rules. " +
                          $"Free space now (measured): C: {Fmt.Size(c.AvailableFreeSpace)}. " +
                          $"Estimated reclaim: {Fmt.Size(plan.PredictedBytes)} (estimate; the real change is measured after the run). " +
                          (plan.Blocked > 0 ? $"{plan.Blocked:N0} blocked by safety checks and unticked. " : "") + "Nothing has been changed.";
        }
        catch (OperationCanceledException) { Status.Text = "Cancelled. Nothing was changed."; }
        catch (Exception ex) { Status.Text = "Preview failed: " + ex.Message; }
        finally { Busy.Visibility = Visibility.Collapsed; PreviewBtn.IsEnabled = CleanBtn.IsEnabled = true; }
    }

    private async void CleanBtn_Click(object sender, RoutedEventArgs e)
    {
        if (Table.BlockReason() is { } why) { Status.Text = why; return; }
        _cts = new CancellationTokenSource();
        var progress = new Progress<CleanProgress>(p => { Bar.Value = p.Total == 0 ? 100 : 100.0 * p.Done / p.Total; Current.Text = p.Current; });
        var targets = Table.Targets(i => new[] { ((PlanItem)i.Tag!).Target });
        Bar.Visibility = Visibility.Visible;
        PreviewBtn.IsEnabled = CleanBtn.IsEnabled = false;
        try
        {
            if (await Ui.RunJob(XamlRoot, targets, "Clean these files?", "Clean", Table.ShowHidden, progress, _cts.Token) is not { } r) return;
            var s = Cleaner.Summarize(r);
            History.Append(State.Settings.BackupRoot, s);
            Status.Text = $"{(r.Cancelled ? "Cancelled" : "Done")}: {s.Deleted:N0} deleted, {s.Archived:N0} archived, {s.Failed.Count:N0} skipped. " +
                          $"Measured change: {string.Join(", ", r.FreedByDrive.Select(d => $"{d.Key.TrimEnd('\\')} {(d.Value >= 0 ? "+" : "-")}{Fmt.Size(Math.Abs(d.Value))}"))} " +
                          $"(estimate was {Fmt.Size(r.PredictedBytes)}).";
            var done = r.Results.Where(x => x.Outcome == Outcome.Done).Select(x => x.Item.Facts.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
            Table.RemoveWhere(i => done.Contains(i.Path));
            State.LastScan = new();
        }
        catch (OperationCanceledException) { Status.Text = "Cancelled before anything was removed."; }
        catch (Exception ex) { Status.Text = "Failed: " + ex.Message; }
        finally { Bar.Visibility = Visibility.Collapsed; Current.Text = ""; PreviewBtn.IsEnabled = CleanBtn.IsEnabled = true; }
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e) => _cts?.Cancel();

    private string ExportFile(string ext)
    {
        Directory.CreateDirectory(AppSettings.DataDir);
        return Path.Combine(AppSettings.DataDir, $"clean-preview-{DateTime.Now:yyyyMMdd-HHmmss}.{ext}");
    }

    private void CsvBtn_Click(object sender, RoutedEventArgs e)
    {
        if (Table.Items.Count == 0) { Status.Text = "Press Preview first."; return; }
        var sb = new StringBuilder("path,rule,bytes,modified,risk,action,ticked,notes\n");
        foreach (var i in Table.Items)
            sb.AppendLine($"\"{i.Path}\",\"{i.App}\",{i.Size},{i.ModifiedText},{i.RiskText},{i.Action},{i.Checked},\"{i.Extra}\"");
        var f = ExportFile("csv");
        File.WriteAllText(f, sb.ToString());
        Ui.ShowInExplorer(f);
    }

    private void JsonBtn_Click(object sender, RoutedEventArgs e)
    {
        if (Table.Items.Count == 0) { Status.Text = "Press Preview first."; return; }
        var f = ExportFile("json");
        File.WriteAllText(f, JsonSerializer.Serialize(Table.Items.Select(i => new
        {
            i.Path, Rule = i.App, Bytes = i.Size, Modified = i.Modified, Risk = i.RiskText, i.Action, Ticked = i.Checked, Notes = i.Extra,
        }), new JsonSerializerOptions { WriteIndented = true }));
        Ui.ShowInExplorer(f);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        Pending = null;
    }
}
