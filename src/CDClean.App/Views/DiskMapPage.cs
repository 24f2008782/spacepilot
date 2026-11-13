using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using CDClean.Core;
using CDClean.Core.Disk;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage.Pickers;
using Windows.UI;

namespace CDClean.App.Views;

/// <summary>One row in a list under the map.</summary>
public sealed class MapRow
{
    public string Title { get; set; } = "";
    public string Detail { get; set; } = "";
    public string Size { get; set; } = "";
    public int Node { get; set; } = -1;
}

/// <summary>One row in the tree. ToString is the folder name, so the row has an accessible name for screen readers and UI tests.</summary>
public sealed class TreeNodeView
{
    public string Name { get; init; } = "";
    public string Size { get; init; } = "";
    public override string ToString() => Name;
}

/// <summary>
/// Disk map (tier2-plan batches 2.1–2.3). Read-only: it scans, draws and exports. Deletion stays in Clean and Large files.
/// </summary>
public sealed partial class DiskMapPage : Page
{
    private static readonly int[] Palette = { 0x2563EB, 0x0D9488, 0x7C3AED, 0xB45309, 0x0F766E, 0xBE185D, 0x4338CA, 0x15803D, 0xA16207, 0x0369A1 };
    private const int MaxTiles = 300;

    // Kept between visits, so coming back to the page shows the last scan instead of rescanning.
    private static DiskTree? _tree;
    private static string _scanRoot = "";
    private static bool _wholeDrive;
    private static DateTime _scannedAt;
    private static TimeSpan _took;
    private static int _focus, _selected = -1, _sizeMode;

    private CancellationTokenSource? _cts;
    private readonly Dictionary<int, TreeViewNode> _treeNodes = new();
    private readonly Dictionary<TreeViewNode, int> _nodeIndex = new();
    private readonly HashSet<int> _populated = new();
    private readonly Dictionary<int, Border> _tiles = new();
    private bool _syncing;
    private bool _settingMode;
    private bool _scanActive;
    private int _menuNode = -1;
    private MenuFlyout? _menu;
    private MenuFlyoutItem? _zoomItem;

    public DiskMapPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Start();
        Unloaded += (_, _) => _cts?.Cancel();
    }

    private void Start()
    {
        Ui.FillDrives(DrivePick, _scanRoot.Length > 0 ? _scanRoot : Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\");
        _settingMode = true;
        SizeMode.IsOn = _sizeMode == 1;
        _settingMode = false;
        if (_tree is null)
        {
            Status.Text = "Pick a drive or a folder, then press Scan.";
            UpdateSelection();
            return;
        }
        Show();
    }

    // ---- scanning -------------------------------------------------------------

    private async void ScanDriveBtn_Click(object sender, RoutedEventArgs e)
    {
        if (DrivePick.SelectedItem is Volume v) await ScanAt(v.Root);
    }

    private async void ScanFolderBtn_Click(object sender, RoutedEventArgs e)
    {
        var folder = await Ui.PickFolder();
        if (folder is not null) await ScanAt(folder);
    }

    private async void ScanPathBtn_Click(object sender, RoutedEventArgs e)
    {
        var p = PathBox.Text.Trim();
        if (p.Length == 0) { Status.Text = "Type a folder path first."; return; }
        // "C" and "C:" mean the root of drive C (a bare letter is not a valid relative path).
        if (p.Length == 1 && char.IsLetter(p[0])) p = p + ":\\";
        else if (p.Length == 2 && char.IsLetter(p[0]) && p[1] == ':') p += "\\";
        if (!Directory.Exists(p)) { Status.Text = $"{p} does not exist or is not a folder."; return; }
        await ScanAt(Path.GetFullPath(p));
    }

    private async void FastScanBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_scanActive) return;
        _scanActive = true;
        var drive = (DrivePick.SelectedItem as Volume)?.Root ?? @"C:\";
        var letter = drive.TrimEnd('\\').TrimEnd(':').Last();
        if (!char.IsLetter(letter)) letter = 'C';
        var cts = new CancellationTokenSource();
        _cts = cts;
        SetBusy(true);
        Status.Text = "Fast scan (MFT) starting...";
        var started = DateTime.Now;
        string? outFile = null;
        Process? child = null;
        try
        {
            List<MftEntry> entries;
            if (MftScanner.IsElevated())
            {
                entries = await Task.Run(() => MftScanner.ReadVolume(letter, cts.Token), cts.Token);
            }
            else
            {
                // Launch an elevated instance that writes compact MFT entries, then load them.
                outFile = Path.Combine(Path.GetTempPath(), $"cdclean-mft-{Guid.NewGuid():N}.bin");
                var errFile = outFile + ".err";
                try { if (File.Exists(outFile)) File.Delete(outFile); } catch { }
                try { if (File.Exists(errFile)) File.Delete(errFile); } catch { }
                Status.Text = "Requesting administrator permission (UAC)...";
                var psi = new ProcessStartInfo(Environment.ProcessPath!, $"--mft-scan {letter} \"{outFile}\"")
                { UseShellExecute = true, Verb = "runas", CreateNoWindow = true };
                try { child = Process.Start(psi); }
                catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
                { Status.Text = "Fast scan cancelled at the UAC prompt."; return; }
                if (child is null) { Status.Text = "Could not start the elevated scanner."; return; }
                Status.Text = "Scanning via MFT (admin)...";
                try { await child!.WaitForExitAsync(cts.Token); }
                catch (OperationCanceledException)
                {
                    try { if (!child!.HasExited) child.Kill(entireProcessTree: true); } catch { }
                    await child!.WaitForExitAsync();
                    throw;
                }
                if (File.Exists(errFile)) { Status.Text = "Fast scan failed: " + File.ReadAllText(errFile); return; }
                if (!File.Exists(outFile)) { Status.Text = "Fast scan produced no output."; return; }
                using (var fs = new FileStream(outFile, FileMode.Open, FileAccess.Read))
                    entries = MftScanner.ReadEntries(fs);
            }
            var tree = await Task.Run(() => MftScanner.ToDiskTree(entries, drive, cts.Token), cts.Token);
            _tree = tree;
            _scanRoot = tree.RootPath;
            _wholeDrive = true;
            _took = DateTime.Now - started;
            _scannedAt = DateTime.Now;
            _focus = 0; _selected = 0;
            Show();
        }
        catch (OperationCanceledException) { Status.Text = "Fast scan cancelled."; }
        catch (Exception ex) { Status.Text = "Fast scan failed: " + ex.Message; }
        finally
        {
            child?.Dispose();
            if (outFile is not null)
            {
                try { File.Delete(outFile); } catch { }
                try { File.Delete(outFile + ".count"); } catch { }
                try { File.Delete(outFile + ".err"); } catch { }
            }
            _scanActive = false;
            if (ReferenceEquals(_cts, cts)) _cts = null;
            cts.Dispose();
            SetBusy(false);
        }
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e) => _cts?.Cancel();

    private async Task ScanAt(string root)
    {
        if (_scanActive) return;
        if (!await Ui.CheckScope(XamlRoot, root, Status)) return;
        _scanActive = true;
        var cts = new CancellationTokenSource();
        _cts = cts;
        var progress = new Progress<long>(n => Status.Text = $"Scanning {root}: {n:N0} items so far. Cancel stops the scan.");
        // Note: Ui.CheckScope only warns for removable drives; typed paths on fixed drives go straight through.
        var started = DateTime.Now;
        SetBusy(true);
        try
        {
            var tree = await Task.Run(() => DiskScanner.Scan(root, cts.Token, progress), cts.Token);
            _tree = tree;
            _scanRoot = tree.RootPath;
            _wholeDrive = Volumes.List().Any(v => Same(v.Root, tree.RootPath));
            _took = DateTime.Now - started;
            _scannedAt = DateTime.Now;
            _focus = 0;
            _selected = 0;
            // Storage history: record a snapshot and show "what grew" if a previous one exists.
            try
            {
                var snap = new ScanSnapshot(DateTime.Now, tree.RootPath, tree.TotalAllocated, tree.FileCount,
                    new List<(string, long)> { (tree.RootPath, tree.TotalAllocated) });
                var prev = ScanHistory.List(tree.RootPath, 2).Skip(1).FirstOrDefault();
                ScanHistory.Record(snap);
                if (prev is not null)
                {
                    var diff = ScanHistory.Diff(prev, snap);
                    if (diff is { } d)
                    {
                        var sign = d.UsedDelta >= 0 ? "+" : "";
                        Status.Text += $"  (vs last scan: {sign}{Fmt.Size(d.UsedDelta)} used, {d.FilesDelta:+#;-#;0} files)";
                    }
                }
            }
            catch { }
            Show();
        }
        catch (OperationCanceledException) { Status.Text = "Scan cancelled. The previous result is still shown."; }
        catch (Exception ex) { Status.Text = "Scan failed: " + ex.Message; }
        finally
        {
            _scanActive = false;
            if (ReferenceEquals(_cts, cts)) _cts = null;
            cts.Dispose();
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        Busy.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
        CancelBtn.IsEnabled = busy;
        ScanDriveBtn.IsEnabled = !busy;
        ScanFolderBtn.IsEnabled = !busy;
        ScanPathBtn.IsEnabled = !busy;
        FastScanBtn.IsEnabled = !busy;
        PathBox.IsEnabled = !busy;
    }

    private static bool Same(string a, string b) =>
        string.Equals(a.TrimEnd('\\'), b.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);

    private static long DriveUsed()
    {
        var v = ScannedVolume();
        return v is null ? _tree!.TotalAllocated : v.Total - v.Free;
    }

    private static Volume? ScannedVolume() =>
        Volumes.List().FirstOrDefault(x => _scanRoot.StartsWith(x.Root, StringComparison.OrdinalIgnoreCase));

    /// <summary>Redraws everything from the current scan. Used after a scan and when the size mode changes.</summary>
    private void Show()
    {
        if (_tree is null) return;
        var volume = ScannedVolume();
        var space = volume is null ? "" : $" {volume.Letter} {Fmt.Size(volume.Total - volume.Free)} occupied, {Fmt.Size(volume.Free)} free of {Fmt.Size(volume.Total)} (Windows measurement).";
        Status.Text = $"{Fmt.Size(_tree.TotalAllocated)} in scanned files ({_tree.FileCount:N0} files).{space} Scanned in {_took.TotalSeconds:0.0} s on {_scannedAt:yyyy-MM-dd HH:mm}." +
                      (_tree.UnreadableCount > 0 ? $" {_tree.UnreadableCount:N0} item(s) could not be read." : "") +
                      (_tree.Truncated ? $" Map is partial: the {DiskTree.BudgetMb} MB scan budget was reached." : "");
        BuildTree();
        RebuildCrumbs();
        Redraw();
        RefreshLists();
        UpdateSelection();
    }

    private void SizeMode_Toggled(object sender, RoutedEventArgs e)
    {
        if (_settingMode) return;
        _sizeMode = SizeMode.IsOn ? 1 : 0;
        if (_tree is not null) Show();
    }

    // ---- sizes ----------------------------------------------------------------

    /// <summary>The size shown for an item: the folder total, or only the files directly inside it.</summary>
    private long SizeOf(int i)
    {
        var n = _tree!.Nodes[i];
        if (_sizeMode == 0 || n.Kind != NodeKind.Dir) return n.InclusiveAllocated;
        long s = 0;
        foreach (var c in _tree.Children(i))
            if (_tree.Nodes[c].Kind == NodeKind.File) s += _tree.Nodes[c].OwnAllocated;
        return s;
    }

    private List<int> SortedKids(int idx) => _tree!.Children(idx).OrderByDescending(SizeOf).ToList();

    /// <summary>The biggest children only, without sorting a folder that may hold 100k entries.</summary>
    private (List<int> Top, long Others, int OtherCount) TopKids(int idx, int count)
    {
        var top = new PriorityQueue<int, long>();
        long total = 0;
        var seen = 0;
        foreach (var c in _tree!.Children(idx))
        {
            var s = SizeOf(c);
            if (s <= 0) continue;
            total += s;
            seen++;
            top.Enqueue(c, s);
            if (top.Count > count) top.Dequeue();
        }
        var list = top.UnorderedItems.Select(x => x.Element).OrderByDescending(SizeOf).ToList();
        long topSum = 0;
        foreach (var i in list) topSum += SizeOf(i);
        return (list, total - topSum, seen - list.Count);
    }

    private string NameOf(int idx) => idx == 0 ? _tree!.RootPath : _tree!.Nodes[idx].Name;

    private string SizeText(int idx)
    {
        var n = _tree!.Nodes[idx];
        if (n.Kind == NodeKind.Unreadable) return "unknown";
        var s = Fmt.Size(SizeOf(idx));
        return _tree!.ErrorOf(idx) != null ? s + " *" : s;
    }

    // ---- tree (left) ----------------------------------------------------------

    private void BuildTree()
    {
        Tree.RootNodes.Clear();
        _treeNodes.Clear();
        _nodeIndex.Clear();
        _populated.Clear();
        var root = MakeNode(0);
        Tree.RootNodes.Add(root);
        Populate(root, 0);
        root.IsExpanded = true;
        RevealInTree(_selected);
    }

    private TreeViewNode MakeNode(int idx)
    {
        var n = _tree!.Nodes[idx];
        var node = new TreeViewNode
        {
            Content = new TreeNodeView { Name = NameOf(idx), Size = SizeText(idx) },
            HasUnrealizedChildren = n.Kind == NodeKind.Dir && _tree.HasChildren(idx),
        };
        _treeNodes[idx] = node;
        _nodeIndex[node] = idx;
        return node;
    }

    private void Populate(TreeViewNode parent, int idx)
    {
        if (!_populated.Add(idx)) return;
        foreach (var k in SortedKids(idx)) parent.Children.Add(MakeNode(k));
        parent.HasUnrealizedChildren = false;
    }

    private void Tree_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
    {
        if (args.Node is not null && _nodeIndex.TryGetValue(args.Node, out var idx)) Populate(args.Node, idx);
    }

    private void Tree_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
    {
        if (_syncing || _tree is null || sender.SelectedNode is not { } node || !_nodeIndex.TryGetValue(node, out var idx)) return;
        if (_tree.Nodes[idx].Kind == NodeKind.Dir) Focus(idx);
        else
        {
            var parent = _tree.Nodes[idx].Parent;
            if (parent != _focus) Focus(parent);
            Select(idx);
        }
    }

    /// <summary>The tree node for an item, expanding its parents so it can be seen.</summary>
    private TreeViewNode? NodeFor(int idx)
    {
        if (_treeNodes.TryGetValue(idx, out var n)) return n;
        var parent = _tree!.Nodes[idx].Parent;
        if (parent < 0) return null;
        var pn = NodeFor(parent);
        if (pn is null) return null;
        Populate(pn, parent);
        pn.IsExpanded = true;
        return _treeNodes.GetValueOrDefault(idx);
    }

    private void RevealInTree(int idx)
    {
        if (_tree is null || idx < 0) return;
        var node = NodeFor(idx);
        if (node is null) return;
        _syncing = true;
        try { Tree.SelectedNode = node; }
        finally { _syncing = false; }
    }

    // ---- map (right) ----------------------------------------------------------

    /// <summary>Makes a folder the map's top level (zoom in or out). A file focuses its folder.</summary>
    private void Focus(int idx)
    {
        if (_tree is null || idx < 0) return;
        if (_tree.Nodes[idx].Kind != NodeKind.Dir) idx = Math.Max(0, _tree.Nodes[idx].Parent);
        _focus = idx;
        _selected = idx;
        RebuildCrumbs();
        Redraw();
        RefreshLists();
        UpdateSelection();
        RevealInTree(idx);
    }

    /// <summary>Highlights an item everywhere it is shown. Does not change the map's top level.</summary>
    private void Select(int idx)
    {
        if (_tree is null || idx < 0) return;
        _selected = idx;
        RevealInTree(idx);
        HighlightTile();
        UpdateSelection();
    }

    private DispatcherQueueTimer? _resize;

    private void MapHost_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_tree is null) return;
        // A window resize fires this many times a second. Redraw once when it settles, not once per pixel.
        if (_resize is null)
        {
            _resize = DispatcherQueue.CreateTimer();
            _resize.Interval = TimeSpan.FromMilliseconds(80);
            _resize.IsRepeating = false;
            _resize.Tick += (_, _) => Redraw();
        }
        _resize.Stop();
        _resize.Start();
    }

    private void Redraw()
    {
        Map.Children.Clear();
        _tiles.Clear();
        if (_tree is null) return;
        var w = MapHost.ActualWidth;
        var h = MapHost.ActualHeight;
        if (w < 10 || h < 10) return;
        Map.Width = w;
        Map.Height = h;

        var (top, rest, restCount) = TopKids(_focus, MaxTiles);
        if (top.Count == 0)
        {
            var empty = new TextBlock { Text = "Nothing with size in this folder.", Opacity = 0.7 };
            Canvas.SetLeft(empty, 12);
            Canvas.SetTop(empty, 12);
            Map.Children.Add(empty);
            return;
        }

        var values = top.Select(SizeOf).ToList();
        if (rest > 0) values.Add(rest);

        var tiles = Treemap.Layout(values, new TileRect(0, 0, w, h));
        foreach (var (i, r) in tiles)
        {
            if (i < top.Count) AddTile(top[i], r);
            else AddOtherTile(restCount, rest, r);
        }
        AutomationProperties.SetName(Map, $"Treemap, {tiles.Count} tiles");
        HighlightTile();
    }

    private void AddTile(int idx, TileRect r)
    {
        var n = _tree!.Nodes[idx];
        var tile = new Border
        {
            Width = Math.Max(1, r.W - 2),
            Height = Math.Max(1, r.H - 2),
            Background = new SolidColorBrush(ColorOf(TileRgb(idx))),
            CornerRadius = new CornerRadius(2),
            Padding = new Thickness(4, 2, 4, 2),
        };
        if (r.W > 64 && r.H > 30)
        {
            var sp = new StackPanel();
            sp.Children.Add(new TextBlock { Text = NameOf(idx), Foreground = new SolidColorBrush(Colors.White), FontSize = 12, TextTrimming = TextTrimming.CharacterEllipsis });
            if (r.H > 46) sp.Children.Add(new TextBlock { Text = Fmt.Size(SizeOf(idx)), Foreground = new SolidColorBrush(Colors.White), FontSize = 11, Opacity = 0.85 });
            tile.Child = sp;
        }
        Canvas.SetLeft(tile, r.X + 1);
        Canvas.SetTop(tile, r.Y + 1);
        ToolTipService.SetToolTip(tile, new ToolTip { Content = TipOf(idx) });
        tile.Tapped += (_, e) => { Select(idx); e.Handled = true; };
        tile.DoubleTapped += (_, e) => { if (n.Kind == NodeKind.Dir) Focus(idx); e.Handled = true; };
        tile.RightTapped += (_, e) =>
        {
            Select(idx);
            _menuNode = idx;
            _menu ??= BuildMenu();
            if (_zoomItem is not null) _zoomItem.IsEnabled = n.Kind == NodeKind.Dir;
            _menu.ShowAt(tile, e.GetPosition(tile));
            e.Handled = true;
        };
        _tiles[idx] = tile;
        Map.Children.Add(tile);
    }

    private void AddOtherTile(int count, long bytes, TileRect r)
    {
        var tile = new Border
        {
            Width = Math.Max(1, r.W - 2),
            Height = Math.Max(1, r.H - 2),
            Background = new SolidColorBrush(Colors.Gray),
            CornerRadius = new CornerRadius(2),
            Padding = new Thickness(4, 2, 4, 2),
        };
        if (r.W > 64 && r.H > 30)
            tile.Child = new TextBlock { Text = $"{count:N0} smaller items", Foreground = new SolidColorBrush(Colors.White), FontSize = 12, TextTrimming = TextTrimming.CharacterEllipsis };
        Canvas.SetLeft(tile, r.X + 1);
        Canvas.SetTop(tile, r.Y + 1);
        ToolTipService.SetToolTip(tile, $"{count:N0} smaller items, {Fmt.Size(bytes)} together. See Largest files or the tree for them.");
        tile.Tapped += (_, e) => { Status.Text = $"{count:N0} smaller items ({Fmt.Size(bytes)}) are not drawn one by one. Use Largest files or the tree."; e.Handled = true; };
        Map.Children.Add(tile);
    }

    private void HighlightTile()
    {
        foreach (var (idx, tile) in _tiles)
        {
            tile.BorderThickness = new Thickness(idx == _selected ? 2 : 0);
            tile.BorderBrush = new SolidColorBrush(Colors.White);
        }
    }

    private int TileRgb(int idx)
    {
        var n = _tree!.Nodes[idx];
        if (_tree.ErrorOf(idx) != null) return 0x991B1B;
        if (n.Kind == NodeKind.Link) return 0x6B7280;
        if (n.Kind == NodeKind.File) return 0x475569;
        var h = 0;
        foreach (var c in NameOf(idx)) h = h * 31 + c;
        return Palette[(h & 0x7fffffff) % Palette.Length];
    }

    private static Color ColorOf(int rgb) => Color.FromArgb(255, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);

    private string TipOf(int idx)
    {
        var n = _tree!.Nodes[idx];
        var focusSize = SizeOf(_focus);
        var pct = focusSize > 0 ? 100.0 * SizeOf(idx) / focusSize : 0;
        var lines = new List<string>
        {
            _tree.PathOf(idx),
            $"{Fmt.Size(SizeOf(idx))}  ({pct:0.0}% of this folder)",
        };
        if (n.Kind == NodeKind.Dir) lines.Add($"{n.InclusiveFiles:N0} files inside");
        lines.Add($"Logical size {Fmt.Size(n.InclusiveLogical)}");
        if (n.Modified != DateTime.MinValue) lines.Add("Modified " + n.Modified.ToLocalTime().ToString("g"));
        if (n.Cloud) lines.Add("Cloud file: its data is not on this disk, so it counts 0.");
        if (n.RepeatHardLink) lines.Add("Second name for a file already counted, so it counts 0.");
        if (n.Kind == NodeKind.Link) lines.Add("Junction or symbolic link: not followed.");
        if (_tree.ErrorOf(idx) is { } err) lines.Add("Could not be read: " + err);
        return string.Join("\n", lines);
    }

    private MenuFlyout BuildMenu()
    {
        var m = new MenuFlyout();
        m.Items.Add(MenuItem("Open", () => Ui.Open(_tree!.PathOf(_menuNode))));
        m.Items.Add(MenuItem("Show in Explorer", () => Ui.ShowInExplorer(_tree!.PathOf(_menuNode))));
        m.Items.Add(MenuItem("Properties", () => Ui.Properties(_tree!.PathOf(_menuNode))));
        m.Items.Add(MenuItem("Copy path", () => Ui.Copy(_tree!.PathOf(_menuNode))));
        _zoomItem = MenuItem("Zoom into this folder", () => Focus(_menuNode));
        m.Items.Add(_zoomItem);
        return m;
    }

    private static MenuFlyoutItem MenuItem(string text, Action run)
    {
        var item = new MenuFlyoutItem { Text = text };
        item.Click += (_, _) => run();
        return item;
    }

    private void RebuildCrumbs()
    {
        Crumbs.Children.Clear();
        var chain = new List<int>();
        for (var i = _focus; i >= 0; i = _tree!.Nodes[i].Parent) chain.Add(i);
        chain.Reverse();
        for (var k = 0; k < chain.Count; k++)
        {
            var idx = chain[k];
            if (k > 0) Crumbs.Children.Add(new TextBlock { Text = "›", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 2, 0) });
            var b = new HyperlinkButton { Content = NameOf(idx), Padding = new Thickness(4, 2, 4, 2) };
            b.Click += (_, _) => Focus(idx);
            Crumbs.Children.Add(b);
        }
    }

    private void ZoomOut_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (_tree is not null && _focus > 0) Focus(_tree.Nodes[_focus].Parent);
        args.Handled = true;
    }

    // ---- lists and ledger (bottom) -------------------------------------------

    /// <summary>Every item at or below a folder.</summary>
    private static IEnumerable<int> Under(DiskTree t, int focus)
    {
        // Walks down from the focus folder, so the cost is the subtree, not the whole scan.
        var stack = new Stack<int>();
        stack.Push(focus);
        while (stack.Count > 0)
        {
            var i = stack.Pop();
            yield return i;
            foreach (var c in t.Children(i)) stack.Push(c);
        }
    }

    private int _refreshGen;

    /// <summary>
    /// Fills the tab that is showing, on a background thread. The old code walked the subtree three times and
    /// sorted every file on the UI thread, which froze the page after a large scan.
    /// </summary>
    private void RefreshLists()
    {
        if (_tree is null) return;
        var gen = ++_refreshGen;
        var tab = Math.Max(0, Tabs.SelectedIndex);
        var t = _tree;
        var focus = _focus;
        var q = FindBox.Text.Trim();
        var used = tab == 3 ? DriveUsed() : 0;
        var whole = _wholeDrive;
        Task.Run(() =>
        {
            var rows = tab switch
            {
                0 => BuildFiles(t, focus, q),
                1 => BuildTypes(t, focus),
                2 => BuildUnreadable(t, focus),
                _ => BuildLedger(t, used, whole),
            };
            DispatcherQueue.TryEnqueue(() =>
            {
                if (gen != _refreshGen) return; // a newer view replaced this one
                if (tab == 0) FilesList.ItemsSource = rows;
                else if (tab == 1) TypesList.ItemsSource = rows;
                else if (tab == 2) UnreadList.ItemsSource = rows;
                else LedgerList.ItemsSource = rows;
            });
        });
    }

    private static List<MapRow> BuildFiles(DiskTree t, int focus, string q)
    {
        // Keep the 300 largest without sorting the whole subtree.
        var top = new PriorityQueue<int, long>();
        foreach (var i in Under(t, focus))
        {
            var n = t.Nodes[i];
            if (n.Kind != NodeKind.File) continue;
            if (q.Length > 0 && !n.Name.Contains(q, StringComparison.OrdinalIgnoreCase)) continue;
            top.Enqueue(i, n.OwnAllocated);
            if (top.Count > 300) top.Dequeue();
        }
        return top.UnorderedItems.Select(x => x.Element)
            .OrderByDescending(i => t.Nodes[i].OwnAllocated)
            .Select(i => new MapRow { Title = t.Nodes[i].Name, Detail = t.PathOf(i), Size = Fmt.Size(t.Nodes[i].OwnAllocated), Node = i })
            .ToList();
    }

    private static List<MapRow> BuildTypes(DiskTree t, int focus)
    {
        var groups = new Dictionary<string, (long Bytes, int Count)>(StringComparer.OrdinalIgnoreCase);
        foreach (var i in Under(t, focus))
        {
            var n = t.Nodes[i];
            if (n.Kind != NodeKind.File) continue;
            var ext = ExtOf(n.Name);
            var cur = groups.TryGetValue(ext, out var g) ? g : (0L, 0);
            groups[ext] = (cur.Item1 + n.OwnAllocated, cur.Item2 + 1);
        }
        return groups.Select(x => (Ext: x.Key, x.Value.Bytes, x.Value.Count))
            .OrderByDescending(x => x.Bytes).Take(200)
            .Select(x => new MapRow { Title = x.Ext, Detail = $"{x.Count:N0} files", Size = Fmt.Size(x.Bytes) })
            .ToList();
    }

    private static string ExtOf(string name)
    {
        var e = Path.GetExtension(name).ToLowerInvariant();
        return e.Length == 0 ? "(no extension)" : e;
    }

    private static List<MapRow> BuildUnreadable(DiskTree t, int focus) =>
        Under(t, focus).Where(i => t.ErrorOf(i) != null).Take(500)
            .Select(i => new MapRow { Title = t.Nodes[i].Name, Detail = t.PathOf(i) + "\n" + t.ErrorOf(i), Size = "unknown", Node = i })
            .ToList();

    private static List<MapRow> BuildLedger(DiskTree t, long used, bool whole) =>
        Ledger.Build(t, used, whole).Select(l => new MapRow
        {
            Title = l.Name,
            Detail = l.Note + (l.InTotal ? "" : "  (not in the total)"),
            Size = (l.Bytes < 0 ? "-" : "") + Fmt.Size(Math.Abs(l.Bytes)),
        }).ToList();

    private void FindBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshLists();

    private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshLists();

    private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncing || sender is not ListView { SelectedItem: MapRow r } || r.Node < 0) return;
        Select(r.Node);
    }

    // ---- selection bar --------------------------------------------------------

    private bool HasSelection => _tree is not null && _selected >= 0;

    private void UpdateSelection()
    {
        var has = HasSelection;
        OpenBtn.IsEnabled = ExplorerBtn.IsEnabled = PropsBtn.IsEnabled = CopyBtn.IsEnabled = has;
        if (!has)
        {
            SelText.Text = "Nothing selected.";
            return;
        }
        var n = _tree!.Nodes[_selected];
        var what = n.Kind switch
        {
            NodeKind.Dir => $"{n.InclusiveFiles:N0} files inside",
            NodeKind.File => "file",
            NodeKind.Link => "junction or link (not followed)",
            _ => "unreadable",
        };
        SelText.Text = $"{_tree.PathOf(_selected)}   ·   {Fmt.Size(SizeOf(_selected))}   ·   {what}" +
                       (_tree.ErrorOf(_selected) is { } err ? $"   ·   could not be fully read: {err}" : "");
    }

    private void OpenBtn_Click(object sender, RoutedEventArgs e) { if (HasSelection) Ui.Open(_tree!.PathOf(_selected)); }
    private void ExplorerBtn_Click(object sender, RoutedEventArgs e) { if (HasSelection) Ui.ShowInExplorer(_tree!.PathOf(_selected)); }
    private void PropsBtn_Click(object sender, RoutedEventArgs e) { if (HasSelection) Ui.Properties(_tree!.PathOf(_selected)); }
    private void CopyBtn_Click(object sender, RoutedEventArgs e) { if (HasSelection) Ui.Copy(_tree!.PathOf(_selected)); }

    // ---- export ---------------------------------------------------------------

    private static async Task<string?> PickSave(string name, string ext, string label)
    {
        var p = new FileSavePicker { SuggestedFileName = name };
        p.FileTypeChoices.Add(label, new List<string> { ext });
        WinRT.Interop.InitializeWithWindow.Initialize(p, WinRT.Interop.WindowNative.GetWindowHandle(App.Main!));
        return (await p.PickSaveFileAsync())?.Path;
    }

    private static string Csv(string s) => $"\"{s.Replace("\"", "\"\"")}\"";

    private async void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        if (_tree is null) return;
        var path = await PickSave("cdclean-disk-tree", ".csv", "CSV file");
        if (path is null) return;
        var t = _tree;
        try
        {
            await Task.Run(() => DiskExport.WriteCsv(t, path));
            Status.Text = $"Saved {t.Nodes.Count:N0} rows to {path}.";
        }
        catch (Exception ex) { Status.Text = "Export failed: " + ex.Message; }
    }

    private async void ExportJson_Click(object sender, RoutedEventArgs e)
    {
        if (_tree is null) return;
        var path = await PickSave("cdclean-disk-summary", ".json", "JSON file");
        if (path is null) return;
        var t = _tree;
        var used = DriveUsed();
        var whole = _wholeDrive;
        var scannedAt = _scannedAt;
        try
        {
            var json = await Task.Run(() => DiskExport.SummaryJson(t, Ledger.Build(t, used, whole), scannedAt));
            await File.WriteAllTextAsync(path, json);
            Status.Text = $"Saved the summary to {path}.";
        }
        catch (Exception ex) { Status.Text = "Export failed: " + ex.Message; }
    }

    private async void ExportPng_Click(object sender, RoutedEventArgs e)
    {
        if (_tree is null) return;
        if (MapHost.ActualWidth < 10 || MapHost.ActualHeight < 10) { Redraw(); await Task.Delay(200); }
        var path = await PickSave("cdclean-treemap", ".png", "PNG image");
        if (path is null) return;
        try
        {
            var rtb = new RenderTargetBitmap();
            await rtb.RenderAsync(MapHost);
            if (rtb.PixelWidth == 0 || rtb.PixelHeight == 0) throw new InvalidOperationException("The map has no size to render. Scan and zoom first.");
            var pixels = (await rtb.GetPixelsAsync()).ToArray();
            using var stream = File.Open(path, FileMode.Create, FileAccess.ReadWrite);
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream.AsRandomAccessStream());
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied,
                (uint)rtb.PixelWidth, (uint)rtb.PixelHeight, 96, 96, pixels);
            await encoder.FlushAsync();
            Status.Text = $"Saved the treemap image to {path}.";
        }
        catch (Exception ex) { Status.Text = "Image export failed: " + ex.Message; }
    }
}

