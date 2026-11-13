using System.Text;
using CDClean.App.Controls;
using CDClean.Core;
using CDClean.Core.Safety;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CDClean.App.Views;

internal static class ListActions
{
    /// <summary>
    /// Removes rows through the safety pipeline, then drops the rows whose files are gone.
    /// Toolbar (only == null): ticked visible rows plus hidden ones flagged for the review. Row menu: exactly `only`.
    /// </summary>
    public static async Task Remove(Page page, ItemsGrid table, TextBlock status, ProgressBar busy,
        Func<GridItem, IEnumerable<string>> paths, Method method, Category cat, string source, string title, string verb,
        IReadOnlyList<GridItem>? only = null, bool? isDirectory = null)
    {
        if (table.StaleCount > 0 || (only == null && table.BlockReason() is not null)) { status.Text = table.BlockReason(); return; }
        var picked = only?.ToList() ?? table.SelectedVisible.Concat(table.SelectedHidden).ToList();
        var targets = only != null
            ? only.SelectMany(i => paths(i).Select(p => new Target(p, method, cat, source, IsDirectory: isDirectory))).ToList()
            : table.Targets(i => paths(i).Select(p => new Target(p, method, cat, source, IsDirectory: isDirectory)));
        busy.Visibility = Visibility.Visible;
        try
        {
            if (await Ui.RunJob(page.XamlRoot, targets, title, verb, table.ShowHidden) is not { } r) return;
            status.Text = r.Describe() + (method == Method.Recycle ? " Empty the Recycle Bin to free the space." : "");
            var set = picked.ToHashSet();
            table.RemoveWhere(i => set.Contains(i) && paths(i).All(p => !File.Exists(p) && !Directory.Exists(p)));
        }
        finally { busy.Visibility = Visibility.Collapsed; }
    }

    public static void Show(ItemsGrid table, Func<GridItem, string> path)
    {
        foreach (var i in table.SelectedVisible.Take(20)) Ui.ShowInExplorer(path(i));
    }

    public static int DepthOf(string path) => path.TrimEnd('\\').Split('\\').Length - 1;

    /// <summary>Message for a scan that stopped on an error, usually a disconnected drive.</summary>
    public static string Stopped(Exception ex, string folder) =>
        $"Scan of {folder} stopped: {ex.Message} If the drive was disconnected, reconnect it and press Find again. Nothing was changed.";

    /// <summary>Restores a page's saved folder. A saved folder on a missing drive is shown as is, with a warning, never swapped for C:.</summary>
    public static void RestoreScope(string key, TextBox root, ComboBox pick, TextBlock status)
    {
        if (State.Settings.Scopes.TryGetValue(key, out var saved) && saved.Length > 0) root.Text = saved;
        Ui.FillDrives(pick, root.Text);
        if (!Volumes.Available(root.Text))
            status.Text = $"Last time you scanned {root.Text}, which is not available now (drive disconnected?). Pick another drive or folder.";
    }

    public static void SaveScope(string key, string folder)
    {
        State.Settings.Scopes[key] = folder;
        try { State.Settings.Save(); } catch { }
    }
}

public sealed partial class AppsPage : Page
{
    public AppsPage()
    {
        InitializeComponent();
        Table.RowInvoked += (_, i) => { if (i.Path.Length > 0) Ui.ShowInExplorer(i.Path); };
        Table.CurrentChanged += (_, i) => UpdateDetails(i as GridItem);
        Table.AddCommand("Uninstall…", async rows => await Uninstall(rows.Select(r => (InstalledApp)r.Tag!).ToList()), destructive: true);
        Loaded += async (_, _) => await Load();
    }

    private void UpdateDetails(GridItem? row)
    {
        if (row?.Tag is not InstalledApp app)
        {
            Details.Text = "Select a program to see its install location, metadata key, source and pre-flight checks.";
            return;
        }
        var check = AppPreflight.Check(app);
        var sb = new StringBuilder();
        sb.AppendLine($"Name: {app.Name}");
        sb.AppendLine($"Source: {app.Source}");
        sb.AppendLine($"Version: {app.Version}   Publisher: {app.Publisher}");
        sb.AppendLine($"Install location: {(app.InstallLocation.Length > 0 ? app.InstallLocation : "(not recorded)")}");
        sb.AppendLine($"Metadata key: {(app.MetadataKey.Length > 0 ? app.MetadataKey : "(none)")}");
        if (!string.IsNullOrWhiteSpace(app.ProductCode)) sb.AppendLine($"MSI product code: {app.ProductCode}");
        if (!string.IsNullOrWhiteSpace(app.WingetId)) sb.AppendLine($"winget id: {app.WingetId}");
        sb.AppendLine($"Broken: {(app.Broken ? "yes (recorded folder is missing)" : "no")}");
        if (check.Running.Count > 0) sb.AppendLine("Running from install folder: " + string.Join(", ", check.Running));
        if (check.PendingReboot) sb.AppendLine("Pending reboot: yes");
        Details.Text = sb.ToString().TrimEnd();
    }

    private async Task Load()
    {
        Busy.Visibility = Visibility.Visible;
        var apps = await Task.Run(Apps.Installed);
        Table.SetItems(apps.Select(a => new GridItem
        {
            Key = GridItem.IdKey($"{a.Name}|{a.Version}|{a.Publisher}"), Name = a.Name, Type = "app",
            App = a.Publisher, Extra = a.Version, Modified = a.InstallDate, Size = a.SizeBytes > 0 ? a.SizeBytes : -1,
            Path = a.InstallLocation, Source = a.Source, Tag = a,
        }), keepSelection: true);
        var drives = apps.Select(a => Path.GetPathRoot(a.InstallLocation) ?? "").Where(d => d.Length > 0).GroupBy(d => d.ToUpperInvariant())
            .Select(g => $"{g.Count()} on {g.Key.TrimEnd('\\')}");
        var sources = apps.GroupBy(a => a.Source).OrderByDescending(g => g.Count()).Select(g => $"{g.Count()} {g.Key}");
        var broken = apps.Count(a => a.Broken);
        Status.Text = $"{apps.Count} programs ({string.Join(", ", sources)}; {string.Join(", ", drives)}; the rest did not record a location)." +
                      (broken > 0 ? $" {broken} broken (the recorded install folder is missing)." : "") +
                      " Sizes are what each installer reports; '?' means none.";
        Busy.Visibility = Visibility.Collapsed;
    }

    private async void LoadBtn_Click(object sender, RoutedEventArgs e) => await Load();

    private async void UninstallBtn_Click(object sender, RoutedEventArgs e) =>
        await Uninstall(Table.SelectedVisible.Select(i => (InstalledApp)i.Tag!).ToList());

    private async Task Uninstall(List<InstalledApp> picked)
    {
        if (picked.Count == 0) { Status.Text = "Tick the programs to uninstall first."; return; }

        var msix = picked.Where(a => a.Source == "MSIX").ToList();
        if (msix.Count > 0)
        {
            picked = picked.Except(msix).ToList();
            Status.Text = "MSIX apps are removed from Windows Settings > Apps: " + string.Join(", ", msix.Select(a => a.Name)) +
                          (picked.Count > 0 ? ". Checking the rest." : ".");
            if (picked.Count == 0) return;
        }

        var checks = await Task.Run(() => picked.Select(a => (App: a, Check: AppPreflight.Check(a))).ToList());
        var warnings = new List<string>();
        if (checks.Any(c => c.Check.PendingReboot)) warnings.Add("Windows has a pending restart.");
        foreach (var (app, check) in checks)
        {
            if (check.Running.Count > 0) warnings.Add($"{app.Name}: close {string.Join(", ", check.Running)} first.");
            if (check.Broken) warnings.Add($"{app.Name}: the recorded install folder is missing.");
        }

        var message = Table.HiddenNote() +
                      "Each program's own uninstaller opens, one after another:\n\n" +
                      string.Join("\n", checks.Select(c => $"{c.App.Name}  ({c.App.Source})")) +
                      (warnings.Count > 0 ? "\n\nBefore you continue:\n" + string.Join("\n", warnings) : "");
        if (!await Ui.Confirm(XamlRoot, "Uninstall?", message, "Uninstall")) return;
        // Before snapshot: install folder + the app's AppData/ProgramData folders (sizes only).
        var before = picked.ToDictionary(a => a, a => AppSnapshot.Capture(a));
        foreach (var app in picked)
        {
            try { Apps.UninstallNative(app); }
            catch (Exception ex) { Status.Text = ex.Message; continue; }
        }
        // After snapshot, on a background thread, then report what is left.
        _ = Task.Run(async () =>
        {
            await Task.Delay(2000);
            var lines = new List<string> { "Uninstallers started. Press Refresh when they finish." };
            foreach (var app in picked)
            {
                try
                {
                    var after = AppSnapshot.Capture(app);
                    var diff = AppSnapshot.Diff(before[app], after);
                    if (diff.RemainingBytes > 0) lines.Add($"{app.Name}: {Fmt.Size(diff.RemainingBytes)} still present in {diff.RemainingFiles} files (leftover review will classify them).");
                }
                catch { }
            }
            DispatcherQueue.TryEnqueue(() => Status.Text = string.Join("\n", lines));
        });
        Status.Text = "Uninstallers started. Press Refresh when they finish.";
    }

    private void OpenBtn_Click(object sender, RoutedEventArgs e)
    {
        foreach (var a in Table.SelectedVisible.Select(i => (InstalledApp)i.Tag!))
            if (a.InstallLocation.Length > 0) Ui.ShowInExplorer(a.InstallLocation);
    }
}

public sealed partial class LeftoversPage : Page
{
    private const string SystemOnly = "System drive only (AppData, ProgramData)";
    private const string AllDrives = "All drives";

    public LeftoversPage()
    {
        InitializeComponent();
        Table.RowInvoked += (_, i) => Ui.ShowInExplorer(i.Path);
        var vols = Volumes.List();
        DrivePick.ItemsSource = new[] { SystemOnly, AllDrives }.Concat(vols.Select(v => v.Display)).ToList();
        DrivePick.SelectedIndex = 0;
        Table.AddCommand("Keep (hide from this list)", rows => { Keep(rows.ToList()); return Task.CompletedTask; });
        Table.AddCommand("Archive to backup…", rows => Remove(Method.Vault, "Archive to backup drive?", "Archive", rows), destructive: true);
        Table.AddCommand("Move to Recycle Bin…", rows => Remove(Method.Recycle, "Move to Recycle Bin?", "Recycle", rows), destructive: true);
    }

    private List<string> ChosenDrives()
    {
        var vols = Volumes.List();
        return (DrivePick.SelectedItem as string) switch
        {
            AllDrives => vols.Where(v => v.Type == DriveType.Fixed).Select(v => v.Root).ToList(),
            SystemOnly or null => new List<string>(),
            var s => vols.Where(v => v.Display == s).Select(v => v.Root).ToList(),
        };
    }

    private async void FindBtn_Click(object sender, RoutedEventArgs e)
    {
        Busy.Visibility = Visibility.Visible;
        var drives = ChosenDrives();
        Status.Text = "Comparing folders with installed programs...";
        var keep = State.Settings.KeepFolders.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pol = State.Settings.Policy();
        try
        {
            var items = await Task.Run(() => Leftovers.Find(keep, default, drives).Select(f => new GridItem
            {
                Key = GridItem.KeyOf(f.Path), Name = Path.GetFileName(f.Path), Type = "folder", Path = f.Path, Size = f.Bytes, Count = f.Files,
                Modified = f.Newest == DateTime.MinValue ? null : f.Newest, Risk = GridItem.RiskOf(f.Path, Category.AppData, pol), Tag = f,
            }).ToList());
            Table.SetItems(items, keepSelection: true);
            Status.Text = $"{items.Count} unmatched folders, {Fmt.Size(items.Sum(i => i.Size))}, in: {string.Join(", ", Leftovers.Roots(drives).Where(Directory.Exists))}. Some belong to apps that are not in the registry: check before removing.";
        }
        catch (Exception ex) { Status.Text = ListActions.Stopped(ex, "the selected drives"); }
        finally { Busy.Visibility = Visibility.Collapsed; }
    }

    private void OpenBtn_Click(object sender, RoutedEventArgs e) => ListActions.Show(Table, i => i.Path);
    private void KeepBtn_Click(object sender, RoutedEventArgs e) => Keep(Table.SelectedVisible);

    private void Keep(List<GridItem> picked)
    {
        State.Settings.KeepFolders.AddRange(picked.Select(p => p.Path));
        State.Settings.Save();
        var set = picked.ToHashSet();
        Table.RemoveWhere(set.Contains);
        Status.Text = $"{picked.Count} folders will be hidden from now on.";
    }

    private Task Remove(Method m, string title, string verb, IReadOnlyList<GridItem>? only = null) =>
        ListActions.Remove(this, Table, Status, Busy, i => new[] { i.Path }, m, Category.AppData, "Leftovers", title, verb, only, isDirectory: true);

    private async void ArchiveBtn_Click(object sender, RoutedEventArgs e) => await Remove(Method.Vault, "Archive to backup drive?", "Archive");
    private async void RecycleBtn_Click(object sender, RoutedEventArgs e) => await Remove(Method.Recycle, "Move to Recycle Bin?", "Recycle");
}

public sealed partial class OldFilesPage : Page
{
    private CancellationTokenSource? _cts;

    public OldFilesPage()
    {
        InitializeComponent();
        Table.RowInvoked += (_, i) => Ui.ShowInExplorer(i.Path);
        Table.AddCommand("Archive old files to backup…", rows => Remove(Method.Vault, "Archive old files to backup drive?", "Archive", rows), destructive: true);
        Table.AddCommand("Move old files to Recycle Bin…", rows => Remove(Method.Recycle, "Move old files to Recycle Bin?", "Recycle", rows), destructive: true);
        ListActions.RestoreScope("OldFiles", Root, DrivePick, Status);
    }

    private static IEnumerable<string> OldPaths(GridItem i) => ((FolderReport)i.Tag!).Paths ?? Array.Empty<string>();

    private void DrivePick_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DrivePick.SelectedItem is Volume v && !Root.Text.StartsWith(v.Root, StringComparison.OrdinalIgnoreCase)) Root.Text = v.Root;
    }

    private async void BrowseBtn_Click(object sender, RoutedEventArgs e) { if (await Ui.PickFolder() is { } p) { Root.Text = p; Ui.FillDrives(DrivePick, p); } }

    private async void FindBtn_Click(object sender, RoutedEventArgs e)
    {
        var root = Root.Text.Trim();
        if (!await Ui.CheckScope(XamlRoot, root, Status)) return;
        ListActions.SaveScope("OldFiles", root);
        int months = (int)Months.Value, depth = (int)Depth.Value;
        _cts = new CancellationTokenSource();
        Busy.Visibility = Visibility.Visible;
        var progress = new Progress<int>(n => Status.Text = $"Checked {n:N0} files...");
        try
        {
            var pol = State.Settings.Policy();
            var items = await Task.Run(() => OldFiles.Find(root, months, depth, progress, _cts.Token).Select(g => new GridItem
            {
                Key = GridItem.KeyOf(g.Path), Name = Path.GetFileName(g.Path.TrimEnd('\\')) is { Length: > 0 } n ? n : g.Path,
                Type = "folder", Path = g.Path, Size = g.Bytes, Count = g.Files, Modified = g.Newest, Depth = ListActions.DepthOf(g.Path),
                Risk = GridItem.RiskOf(g.Path, Category.UserFile, pol), Tag = g,
            }).ToList());
            Table.SetItems(items, keepSelection: true);
            Status.Text = $"{Fmt.Size(items.Sum(i => i.Size))} in {items.Sum(i => i.Count ?? 0):N0} files not modified for {months} months under {root}. Program files are listed too: uninstall programs from Apps instead of deleting their files.";
        }
        catch (OperationCanceledException) { Status.Text = "Cancelled."; }
        catch (Exception ex) { Status.Text = ListActions.Stopped(ex, root); }
        finally { Busy.Visibility = Visibility.Collapsed; }
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e) => _cts?.Cancel();
    private void OpenBtn_Click(object sender, RoutedEventArgs e) => ListActions.Show(Table, i => i.Path);

    private void ExportBtn_Click(object sender, RoutedEventArgs e)
    {
        var file = Path.Combine(AppSettings.DataDir, $"old-files-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
        Directory.CreateDirectory(AppSettings.DataDir);
        var sb = new StringBuilder("folder,bytes,files,newest\n");
        foreach (var g in Table.Items.Select(r => (FolderReport)r.Tag!))
            sb.AppendLine($"\"{g.Path}\",{g.Bytes},{g.Files},{g.Newest:yyyy-MM-dd}");
        File.WriteAllText(file, sb.ToString());
        Ui.ShowInExplorer(file);
    }

    private Task Remove(Method m, string title, string verb, IReadOnlyList<GridItem>? only = null) =>
        ListActions.Remove(this, Table, Status, Busy, OldPaths, m, Category.UserFile, "Old files", title, verb, only, isDirectory: false);

    private async void ArchiveBtn_Click(object sender, RoutedEventArgs e) => await Remove(Method.Vault, "Archive old files to backup drive?", "Archive");
    private async void RecycleBtn_Click(object sender, RoutedEventArgs e) => await Remove(Method.Recycle, "Move old files to Recycle Bin?", "Recycle");
}

public sealed partial class LargeFilesPage : Page
{
    private CancellationTokenSource? _cts;

    public LargeFilesPage()
    {
        InitializeComponent();
        Table.RowInvoked += (_, i) => Ui.ShowInExplorer(i.Path);
        Table.AddCommand("Archive to backup…", rows => Remove(Method.Vault, "Archive to backup drive?", "Archive", rows), destructive: true);
        Table.AddCommand("Move to Recycle Bin…", rows => Remove(Method.Recycle, "Move to Recycle Bin?", "Recycle", rows), destructive: true);
        ListActions.RestoreScope("LargeFiles", Root, DrivePick, Status);
    }

    private void DrivePick_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DrivePick.SelectedItem is Volume v && !Root.Text.StartsWith(v.Root, StringComparison.OrdinalIgnoreCase)) Root.Text = v.Root;
    }

    private async void BrowseBtn_Click(object sender, RoutedEventArgs e) { if (await Ui.PickFolder() is { } p) { Root.Text = p; Ui.FillDrives(DrivePick, p); } }

    private async void FindBtn_Click(object sender, RoutedEventArgs e)
    {
        var root = Root.Text.Trim();
        if (!await Ui.CheckScope(XamlRoot, root, Status)) return;
        ListActions.SaveScope("LargeFiles", root);
        var min = (long)MinMb.Value * (1L << 20);
        _cts = new CancellationTokenSource();
        Busy.Visibility = Visibility.Visible;
        Status.Text = "Searching...";
        try
        {
            var pol = State.Settings.Policy();
            var items = await Task.Run(() => LargeFiles.Find(root, 300, min, _cts.Token).Select(f => new GridItem
            {
                Key = GridItem.KeyOf(f.Path), Name = Path.GetFileName(f.Path), Type = "file", Path = f.Path, Size = f.Size,
                Modified = File.GetLastWriteTime(f.Path), Risk = GridItem.RiskOf(f.Path, Category.UserFile, pol), Tag = f,
            }).ToList());
            Table.SetItems(items, keepSelection: true);
            Status.Text = $"{items.Count} files, {Fmt.Size(items.Sum(i => i.Size))}, under {root}.";
        }
        catch (OperationCanceledException) { Status.Text = "Cancelled."; }
        catch (Exception ex) { Status.Text = ListActions.Stopped(ex, root); }
        finally { Busy.Visibility = Visibility.Collapsed; }
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e) => _cts?.Cancel();
    private void OpenBtn_Click(object sender, RoutedEventArgs e) => ListActions.Show(Table, i => i.Path);

    private Task Remove(Method m, string title, string verb, IReadOnlyList<GridItem>? only = null) =>
        ListActions.Remove(this, Table, Status, Busy, i => new[] { i.Path }, m, Category.UserFile, "Large files", title, verb, only, isDirectory: false);

    private async void ArchiveBtn_Click(object sender, RoutedEventArgs e) => await Remove(Method.Vault, "Archive to backup drive?", "Archive");
    private async void RecycleBtn_Click(object sender, RoutedEventArgs e) => await Remove(Method.Recycle, "Move to Recycle Bin?", "Recycle");
}

public sealed partial class DuplicatesPage : Page
{
    private CancellationTokenSource? _cts;

    public DuplicatesPage()
    {
        InitializeComponent();
        Table.RowInvoked += (_, i) => Ui.ShowInExplorer(i.Path);
        Table.AddCommand("Move to Recycle Bin…", rows => Recycle(rows), destructive: true);
        ListActions.RestoreScope("Duplicates", Root, DrivePick, Status);
    }

    private void DrivePick_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DrivePick.SelectedItem is Volume v && !Root.Text.StartsWith(v.Root, StringComparison.OrdinalIgnoreCase)) Root.Text = v.Root;
    }

    private async void BrowseBtn_Click(object sender, RoutedEventArgs e) { if (await Ui.PickFolder() is { } p) { Root.Text = p; Ui.FillDrives(DrivePick, p); } }

    private async void FindBtn_Click(object sender, RoutedEventArgs e)
    {
        var root = Root.Text.Trim();
        if (!await Ui.CheckScope(XamlRoot, root, Status)) return;
        ListActions.SaveScope("Duplicates", root);
        var min = (long)(MinMb.Value * (1 << 20));
        _cts = new CancellationTokenSource();
        Busy.Visibility = Visibility.Visible;
        var progress = new Progress<string>(p => Status.Text = "Comparing " + p);
        try
        {
            var groups = await Task.Run(() => Duplicates.Find(root, Math.Max(1, min), progress, _cts.Token));
            // One row per file. Every copy except the first of each group starts ticked, so one copy always stays.
            var items = await Task.Run(() => groups.SelectMany((g, gi) => g.Paths.Select((p, i) => new GridItem
            {
                Key = GridItem.KeyOf(p), Name = Path.GetFileName(p), Type = "file", Path = p, Size = g.Size,
                Extra = $"Group {gi + 1}" + (i == 0 ? " · kept" : ""), Modified = File.GetLastWriteTime(p), Checked = i > 0, Tag = g,
            })).ToList());
            Table.SetItems(items);
            Status.Text = $"{groups.Count} groups under {root}. {Fmt.Size(groups.Sum(g => g.Wasted))} is taken by extra copies.";
        }
        catch (OperationCanceledException) { Status.Text = "Cancelled."; }
        catch (Exception ex) { Status.Text = ListActions.Stopped(ex, root); }
        finally { Busy.Visibility = Visibility.Collapsed; }
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e) => _cts?.Cancel();
    private void OpenBtn_Click(object sender, RoutedEventArgs e) => ListActions.Show(Table, i => i.Path);

    private async void RecycleBtn_Click(object sender, RoutedEventArgs e) => await Recycle(null);

    private async Task Recycle(IReadOnlyList<GridItem>? only)
    {
        // Never remove every copy of a group, whatever is ticked.
        var rows = only ?? Table.SelectedVisible.Concat(Table.SelectedHidden).ToList();
        var all = rows.GroupBy(i => (DuplicateGroup)i.Tag!).Where(g => g.Count() >= g.Key.Paths.Count(File.Exists)).ToList();
        if (all.Count > 0) { Status.Text = $"{all.Count} groups would lose every copy. Keep at least one copy in each group."; return; }
        await ListActions.Remove(this, Table, Status, Busy, i => new[] { i.Path }, Method.Recycle, Category.UserFile, "Duplicates", "Move copies to Recycle Bin?", "Recycle", only, isDirectory: false);
    }
}
