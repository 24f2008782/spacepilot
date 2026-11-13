using System.Collections.ObjectModel;
using System.Text;
using CDClean.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace CDClean.App.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardPage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            var vols = Volumes.List();
            DrivePick.ItemsSource = vols;
            DrivePick.DisplayMemberPath = nameof(Volume.Display);
            DrivePick.SelectedItem = vols.FirstOrDefault(v => v.Root == _drive) ?? vols.FirstOrDefault(v => v.IsSystem) ?? vols.FirstOrDefault();
            LayoutCards(ActualWidth);
            Refresh();
        };
        SizeChanged += (_, e) => LayoutCards(e.NewSize.Width);
    }

    private static string _drive = "";

    /// <summary>Four cards in a row on wide windows, two by two below 1200 px.</summary>
    private void LayoutCards(double width)
    {
        var cols = width >= 1200 ? 4 : 2;
        var cards = Cards.Children.OfType<FrameworkElement>().ToList();
        for (var i = 0; i < cards.Count; i++) { Grid.SetColumn(cards[i], i % cols); Grid.SetRow(cards[i], i / cols); }
        for (var c = 0; c < Cards.ColumnDefinitions.Count; c++)
            Cards.ColumnDefinitions[c].Width = c < cols ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        Cards.ColumnSpacing = 12;
    }

    private void DrivePick_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DrivePick.SelectedItem is Volume v) { _drive = v.Root; Refresh(); }
    }

    private void Refresh()
    {
        var s = State.Settings;
        var root = _drive.Length > 0 ? _drive : @"C:\";
        DiskStatus c;
        try { c = DiskStatus.Of(root); }
        catch { FreeText.Text = "Not available"; UsedText.Text = $"{root} is not connected."; return; }
        var letter = root.TrimEnd('\\');
        FreeLabel.Text = $"Free on {letter}";
        FreeText.Text = Fmt.Size(c.Free);
        UsedBar.Value = 100.0 * c.Used / c.Total;
        UsedText.Text = $"{Fmt.Size(c.Used)} used of {Fmt.Size(c.Total)} (measured now)";
        LowSpace.IsOpen = c.Free < s.LowSpaceGb * (1L << 30);
        LowSpace.Title = $"{letter} is getting full";
        LowSpace.Message = $"Less than {s.LowSpaceGb} GB free on {letter}. Run a scan, or see System cleanup.";

        if (State.LastScan.Count > 0)
        {
            var auto = State.LastScan.Where(r => State.IsAutoRule(r.Rule)).Sum(r => r.Bytes);
            var all = State.LastScan.Sum(r => r.Bytes);
            ReclaimText.Text = Fmt.Size(auto);
            ReclaimNote.Text = $"{Fmt.Size(all)} including manual rules";
        }

        var history = History.Read(s.BackupRoot);
        FreedText.Text = Fmt.Size(history.Sum(h => h.Bytes));
        RunsText.Text = $"{history.Count} runs";
        var max = history.Count == 0 ? 0 : history.Max(h => h.Bytes);
        HistoryList.ItemsSource = history.AsEnumerable().Reverse().Take(20).Select(h => new Row
        {
            Title = h.RunId, Size = Fmt.Size(h.Bytes), Bar = Row.BarOf(h.Bytes, max),
            Detail = $"{h.Deleted} deleted, {h.Archived} archived, {h.Failed} skipped",
        }).ToList();
        NoHistory.Visibility = history.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        var weekly = Schedule.Exists("weekly");
        var daily = Schedule.Exists("daily");
        ScheduleText.Text = weekly && daily ? "Weekly + daily" : weekly ? "Weekly" : daily ? "Daily" : "Off";
        BackupText.Text = $"Backups: {s.BackupRoot}";
    }

    private async void QuickScanBtn_Click(object sender, RoutedEventArgs e)
    {
        Busy.Visibility = Visibility.Visible;
        QuickScanBtn.IsEnabled = false;
        try { State.LastScan = await Task.Run(() => Scanner.ScanAll(State.LoadRules())); }
        catch (Exception ex) { await Ui.Info(XamlRoot, "Scan failed", ex.Message); }
        finally { Busy.Visibility = Visibility.Collapsed; QuickScanBtn.IsEnabled = true; }
        Refresh();
    }

    private void GoCleanBtn_Click(object sender, RoutedEventArgs e) => App.Main?.Go("Clean");
    private void GoSystemBtn_Click(object sender, RoutedEventArgs e) => App.Main?.Go("System");
}

public sealed partial class ScanPage : Page
{
    private CancellationTokenSource? _cts;
    private List<Row> _rows = new();

    public ScanPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Show();
    }

    private void Show()
    {
        var max = State.LastScan.Count == 0 ? 0 : State.LastScan.Max(r => r.Bytes);
        _rows = State.LastScan.Select(r => new Row
        {
            Title = r.Rule.Name + (r.Rule.Safe ? "" : "  (manual)"),
            Detail = $"{r.Items.Count:N0} files · {r.Rule.Action} · {r.Rule.Path}" +
                     (r.ProtectedSkipped > 0 ? $" · {r.ProtectedSkipped} protected skipped" : ""),
            Size = Fmt.Size(r.Bytes), Bar = Row.BarOf(r.Bytes, max), Selected = State.IsAutoRule(r.Rule), Tag = r,
        }).ToList();
        List.ItemsSource = _rows;
        if (State.LastScan.Count > 0)
            Status.Text = $"Total {Fmt.Size(State.LastScan.Sum(r => r.Bytes))} in {State.LastScan.Sum(r => r.Items.Count):N0} files.";
    }

    private async void ScanBtn_Click(object sender, RoutedEventArgs e)
    {
        _cts = new CancellationTokenSource();
        Busy.Visibility = Visibility.Visible;
        Status.Text = "Scanning...";
        try { State.LastScan = await Task.Run(() => Scanner.ScanAll(State.LoadRules(), _cts.Token)); Show(); }
        catch (OperationCanceledException) { Status.Text = "Cancelled."; }
        catch (Exception ex) { Status.Text = "Scan failed: " + ex.Message; }
        finally { Busy.Visibility = Visibility.Collapsed; }
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e) => _cts?.Cancel();

    private void CleanSelectedBtn_Click(object sender, RoutedEventArgs e)
    {
        CleanPage.Pending = _rows.Where(r => r.Selected).Select(r => ((RuleResult)r.Tag!).Rule.Name).ToHashSet();
        App.Main?.Go("Clean");
    }
}

public sealed partial class SystemPage : Page
{
    private DispatcherTimer? _timer;
    private string? _log;
    private AdminTask? _task;
    private long _freeBefore;

    public SystemPage()
    {
        InitializeComponent();
        foreach (var t in AdminTasks.All) Cards.Children.Add(Card(t));
    }

    private Border Card(AdminTask t)
    {
        var run = new Button { Content = "Run", Style = (Style)Application.Current.Resources["AccentButtonStyle"], VerticalAlignment = VerticalAlignment.Center };
        run.Click += async (_, _) => await Start(t);
        var text = new StackPanel { Spacing = 2 };
        text.Children.Add(new TextBlock { Text = (t.Risky ? "⚠ " : "") + t.Title, Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"] });
        text.Children.Add(new TextBlock { Text = t.What, TextWrapping = TextWrapping.Wrap });
        text.Children.Add(new TextBlock { Text = $"Gain: {t.Gain}   ·   Undo: {t.Undo}", FontSize = 12, TextWrapping = TextWrapping.Wrap, Opacity = 0.7 });
        var grid = new Grid { ColumnSpacing = 12 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.Children.Add(text);
        Grid.SetColumn(run, 1);
        grid.Children.Add(run);
        return new Border { Style = (Style)Application.Current.Resources["Card"], Child = grid };
    }

    private async Task Start(AdminTask t)
    {
        if (_timer is not null) { await Ui.Info(XamlRoot, "Busy", "Wait for the running task to finish."); return; }
        if (!await Review.Admin(XamlRoot, t)) return;
        _task = t;
        _freeBefore = new DriveInfo("C").TotalFreeSpace;
        try { _log = AdminTasks.Start(t.Id); }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        { TaskTitle.Text = "Cancelled at the Windows prompt."; return; }
        catch (Exception ex) { await Ui.Info(XamlRoot, "Could not start", ex.Message); return; }

        TaskTitle.Text = t.Title;
        Bar.Value = 0;
        Bar.IsIndeterminate = true;
        LogBox.Text = "";
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _timer.Tick += (_, _) => Poll();
        _timer.Start();
    }

    private async void Analyse_Click(object sender, RoutedEventArgs e)
    {
        TaskTitle.Text = "Maintenance analysis running...";
        Bar.IsIndeterminate = true;
        AnalysisText.Text = "Analysing (this can take a minute)...";
        try
        {
            var w = await Task.Run(() => Maintenance.AnalyzeWinSxS());
            var o = Maintenance.MeasureWindowsOld();
            var v = await Task.Run(() => Maintenance.AnalyzeVss());
            var u = Maintenance.MeasureWuWorkspace();
            var sb = new StringBuilder();
            sb.AppendLine(w.Error.Length > 0
                ? $"WinSxS: unavailable — {w.Error}"
                : $"WinSxS: actual {Fmt.Size(w.ActualBytes)}, shared {Fmt.Size(w.SharedBytes)}, backups {Fmt.Size(w.BackupsBytes)}, cache {Fmt.Size(w.CacheBytes)}. Last cleanup: {w.LastCleanup}.");
            sb.AppendLine($"Windows.old: {(o.Exists ? $"{Fmt.Size(o.Bytes)} ({(o.CanRollback ? "rollback still possible" : "too old to roll back")})" : "not present")}");
            sb.AppendLine(v.Error.Length > 0
                ? $"Shadow storage: unavailable — {v.Error}"
                : $"Shadow storage: {Fmt.Size(v.UsedBytes)} used of {Fmt.Size(v.MaxBytes)} max.");
            sb.AppendLine($"Windows Update download cache: {Fmt.Size(u.Bytes)}.");
            AnalysisText.Text = sb.ToString();
        }
        catch (Exception ex) { AnalysisText.Text = "Analysis failed: " + ex.Message; }
        finally { TaskTitle.Text = "No task running"; Bar.IsIndeterminate = false; Bar.Value = 0; }
    }

    private void Startup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var entries = StartupManager.List();
            var sb = new StringBuilder();
            sb.AppendLine($"Startup entries: {entries.Count} ({entries.Count(x => x.Orphan)} orphans).");
            foreach (var g in entries.GroupBy(x => x.Kind))
            {
                sb.AppendLine($"\n{g.Key} ({g.Count()}):");
                foreach (var x in g.Take(40))
                    sb.AppendLine($"  {x.Name}: {x.Command}" + (x.Orphan ? "  [ORPHAN]" : ""));
            }
            AnalysisText.Text = sb.ToString();
        }
        catch (Exception ex) { AnalysisText.Text = "Startup list failed: " + ex.Message; }
    }

    private void Vhdx_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var list = Vhdx.List(Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\");
            var sb = new StringBuilder();
            sb.AppendLine($"VHDX files: {list.Count}, {Fmt.Size(list.Sum(v => v.SizeBytes))} total.");
            foreach (var v in list.Take(30)) sb.AppendLine($"  {v.Path}  {Fmt.Size(v.SizeBytes)}  {(v.InUse ? "IN USE" : "free")}");
            AnalysisText.Text = sb.ToString();
        }
        catch (Exception ex) { AnalysisText.Text = "VHDX list failed: " + ex.Message; }
    }

    private async void DevCaches_Click(object sender, RoutedEventArgs e)
    {
        TaskTitle.Text = "Finding dev caches...";
        Bar.IsIndeterminate = true;
        AnalysisText.Text = "Scanning your profile for developer caches...";
        try
        {
            var list = await Task.Run(() => DevCaches.Find(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)));
            var sb = new StringBuilder();
            sb.AppendLine($"Dev caches: {list.Count}, {Fmt.Size(list.Sum(d => d.Bytes))} total.");
            foreach (var d in list.Take(40)) sb.AppendLine($"  [{d.Kind}] {d.Path}  {Fmt.Size(d.Bytes)}");
            AnalysisText.Text = sb.ToString();
        }
        catch (Exception ex) { AnalysisText.Text = "Dev cache find failed: " + ex.Message; }
        finally { TaskTitle.Text = "No task running"; Bar.IsIndeterminate = false; Bar.Value = 0; }
    }

    private void Downloads_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            var list = Downloads.Find(dir, 90);
            var sb = new StringBuilder();
            sb.AppendLine($"Downloads older than 90 days: {list.Count}, {Fmt.Size(list.Sum(f => f.Size))}.");
            foreach (var f in list.Take(40)) sb.AppendLine($"  {f.Path}  {Fmt.Size(f.Size)}");
            AnalysisText.Text = sb.ToString();
        }
        catch (Exception ex) { AnalysisText.Text = "Downloads find failed: " + ex.Message; }
    }

    private void BrowserCaches_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var list = BrowserCaches.Find();
            var sb = new StringBuilder();
            sb.AppendLine($"Browser caches: {list.Count}, {Fmt.Size(list.Sum(b => b.Bytes))} total.");
            foreach (var b in list) sb.AppendLine($"  {b.Browser}: {b.Path}  {Fmt.Size(b.Bytes)}  {(BrowserCaches.IsBrowserRunning(b.Browser) ? "RUNNING" : "closed")}");
            AnalysisText.Text = sb.ToString();
        }
        catch (Exception ex) { AnalysisText.Text = "Browser cache find failed: " + ex.Message; }
    }

    private void OrphanServices_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var svcs = OrphanServices.FindServices();
            var sb = new StringBuilder();
            sb.AppendLine($"Orphan services: {svcs.Count}.");
            foreach (var s in svcs.Take(40)) sb.AppendLine($"  {s.Name}: {s.ImagePath}");
            AnalysisText.Text = sb.ToString();
        }
        catch (Exception ex) { AnalysisText.Text = "Orphan services failed: " + ex.Message; }
    }

    private void MsiCache_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var list = MsiCache.Find();
            var sb = new StringBuilder();
            sb.AppendLine($"MSI cache files: {list.Count}, {Fmt.Size(MsiCache.TotalBytes(list))} total.");
            foreach (var f in list.Take(20)) sb.AppendLine($"  {f.Path}  {Fmt.Size(f.Bytes)}");
            AnalysisText.Text = sb.ToString();
        }
        catch (Exception ex) { AnalysisText.Text = "MSI cache find failed: " + ex.Message; }
    }

    private void MaintenanceConsole_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine(MaintenanceConsole.RecommendedOrderNote);
            sb.AppendLine();
            foreach (var s in MaintenanceConsole.Steps())
                sb.AppendLine($"{s.Order}. {s.Name}  [{(s.Destructive ? "destructive" : "read-only")}]");
            sb.AppendLine();
            sb.AppendLine("Commands:");
            foreach (var s in MaintenanceConsole.Steps())
                sb.AppendLine($"  {s.Command}");
            AnalysisText.Text = sb.ToString();
        }
        catch (Exception ex) { AnalysisText.Text = "Maintenance console failed: " + ex.Message; }
    }

    private void Poll()
    {
        if (_log is null) return;
        var (pct, lines, done, ok) = AdminTasks.ReadLog(_log);
        if (pct is double p) { Bar.IsIndeterminate = false; Bar.Value = p; Percent.Text = $"{p:0.0}%"; }
        LogBox.Text = string.Join("\n", lines);
        if (!done) return;
        _timer!.Stop();
        _timer = null;
        Bar.IsIndeterminate = false;
        Bar.Value = 100;
        var freed = new DriveInfo("C").TotalFreeSpace - _freeBefore;
        var measured = $"Measured on C: {(freed >= 0 ? "+" : "-")}{Fmt.Size(Math.Abs(freed))} (estimate was {_task?.Gain}).";
        Percent.Text = (ok ? "Finished. " : "Failed. See the log. ") + measured;
        if (_task is { } t)
            Audit.Write(new AuditLine(DateTime.Now, "admin-" + Path.GetFileNameWithoutExtension(_log!), "admin", @"C:\", t.Id,
                ok ? "Finished" : "Failed", t.Class.ToString(), Array.Empty<string>(), freed, measured, Measured: freed));
    }
}
