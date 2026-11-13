using System.Runtime.InteropServices;
using CDClean.Core;
using CDClean.Core.Selection;
using CommunityToolkit.WinUI.UI.Controls;
using CommunityToolkit.WinUI.UI.Controls.Primitives;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;

namespace CDClean.App.Controls;

/// <summary>
/// The shared table used by Old files, Leftovers, Apps, Large files and Duplicates (playbook-v2 §5, batch 1.1).
/// Two-level search, identity-keyed selection that survives filtering and sorting, a warning when selected
/// rows are hidden, sortable/resizable/reorderable columns, and a saved layout per page.
/// </summary>
public sealed partial class ItemsGrid : UserControl
{
    public static readonly DependencyProperty ActionsProperty = DependencyProperty.Register(
        nameof(Actions), typeof(object), typeof(ItemsGrid),
        new PropertyMetadata(null, (d, e) => ((ItemsGrid)d).ActionsHost.Content = e.NewValue));

    /// <summary>The page's own buttons (Recycle, Archive...), shown at the right of the selection bar.</summary>
    public object? Actions { get => GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }

    /// <summary>Comma-separated column keys, optionally with a header: "Name,App:Publisher,Size".</summary>
    public string ColumnSpec { get; set; } = "Name,Path,Size,Modified";

    /// <summary>Key under which this table's layout is saved in settings.</summary>
    public string LayoutKey { get; set; } = "";

    public string EmptyText { get; set; } = "Nothing to show yet.";

    /// <summary>Raised on double-click or Enter on a row.</summary>
    public event EventHandler<GridItem>? RowInvoked;
    public event EventHandler? SelectionChanged;

    private readonly SelectionSet _sel = new();
    private List<GridItem> _all = new();
    private List<GridItem> _view = new();
    private readonly List<(string Key, bool Desc)> _sort = new();
    private bool _built;
    private int _stale;

    private sealed record ColDef(string Header, string Bind, bool Right = false, double Width = 120);

    private static readonly Dictionary<string, ColDef> Defs = new()
    {
        ["Name"] = new("Name", nameof(GridItem.Name), Width: 260),
        ["Type"] = new("Type", nameof(GridItem.Type), Width: 80),
        ["Path"] = new("Path", nameof(GridItem.Path), Width: 360),
        ["Size"] = new("Size", nameof(GridItem.SizeText), Right: true, Width: 100),
        ["Count"] = new("Files", nameof(GridItem.CountText), Right: true, Width: 90),
        ["Age"] = new("Age (months)", nameof(GridItem.AgeText), Right: true, Width: 110),
        ["Depth"] = new("Depth", nameof(GridItem.DepthText), Right: true, Width: 70),
        ["Modified"] = new("Modified", nameof(GridItem.ModifiedText), Width: 110),
        ["LastUsed"] = new("Last used", nameof(GridItem.LastUsed), Width: 110),
        ["App"] = new("App", nameof(GridItem.App), Width: 180),
        ["Source"] = new("Source", nameof(GridItem.Source), Width: 140),
        ["Extra"] = new("Details", nameof(GridItem.Extra), Width: 150),
        ["Risk"] = new("Risk", "", Width: 100),
        ["Action"] = new("Action", nameof(GridItem.Action), Width: 110),
    };

    public ItemsGrid()
    {
        InitializeComponent();
        ColumnsBtn.Flyout = ColumnMenu();
        Table.ColumnHeaderStyle = new Style(typeof(DataGridColumnHeader))
        {
            Setters = { new Setter(UIElement.ContextFlyoutProperty, ColumnMenu()) },
        };
        Accel(VirtualKey.A, VirtualKeyModifiers.Control, () => SelectVisible_Click(this, new RoutedEventArgs()));
        Accel(VirtualKey.A, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, () => Clear_Click(this, new RoutedEventArgs()));
        Accel(VirtualKey.I, VirtualKeyModifiers.Control, () => Invert_Click(this, new RoutedEventArgs()));
        Accel(VirtualKey.F, VirtualKeyModifiers.Control, () => Search.Focus(FocusState.Keyboard));
        Accel(VirtualKey.F, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, () => Within.Focus(FocusState.Keyboard));
        Loaded += (_, _) => { EnsureColumns(); Refresh(); };
        Table.SelectionChanged += (_, _) => CurrentChanged?.Invoke(this, Table.SelectedItem as GridItem);
        Table.ContextRequested += Table_ContextRequested;
        FiltersFlyout.Opening += (_, _) => FillDriveBox();
        SizeChanged += (_, e) => FitColumns(e.NewSize.Width);
        Unloaded += (_, _) => SaveLayout();
    }

    // ---------------------------------------------------------------- right-click menu (report §4.8, §8.12)

    /// <summary>A page action offered in the row menu. Run gets the rows it applies to.</summary>
    private sealed record RowCommand(string Text, Func<IReadOnlyList<GridItem>, Task> Run, bool Destructive);

    private readonly List<RowCommand> Commands = new();

    /// <summary>Adds a page action to the row menu (Archive, Recycle, Uninstall...). Destructive ones are separated and never offered on protected rows.</summary>
    public void AddCommand(string text, Func<IReadOnlyList<GridItem>, Task> run, bool destructive = false) => Commands.Add(new RowCommand(text, run, destructive));

    /// <summary>True when rows are programs, not files: file verbs are replaced by app verbs.</summary>
    public bool AppRows { get; set; }

    private static readonly HashSet<string> Runnable = new(StringComparer.OrdinalIgnoreCase)
    { ".exe", ".msi", ".bat", ".cmd", ".ps1", ".vbs", ".js", ".lnk", ".scr", ".com", ".reg" };

    private void Table_ContextRequested(UIElement sender, ContextRequestedEventArgs e)
    {
        var hasPoint = e.TryGetPosition(Table, out var pt);
        var row = (e.OriginalSource as FrameworkElement)?.DataContext as GridItem;
        if (row is null && !hasPoint) row = Table.SelectedItem as GridItem; // keyboard: Shift+F10 / menu key
        if (row is null) return; // header or empty space
        Table.SelectedItem = row;
        // A ticked row acts with every ticked visible row; an unticked row acts alone. Never anything hidden.
        var targets = _sel.Contains(row.Key) ? SelectedVisible : new List<GridItem> { row };
        var menu = BuildMenu(row, targets);
        if (hasPoint) menu.ShowAt(Table, new Microsoft.UI.Xaml.Controls.Primitives.FlyoutShowOptions { Position = pt });
        else menu.ShowAt(Table);
        e.Handled = true;
    }

    private MenuFlyout BuildMenu(GridItem row, IReadOnlyList<GridItem> targets)
    {
        var m = new MenuFlyout();
        var many = targets.Count > 1 ? $" ({targets.Count} selected)" : "";
        void Add(string text, Action run, IList<MenuFlyoutItemBase>? into = null)
        {
            var i = new MenuFlyoutItem { Text = text };
            i.Click += (_, _) => run();
            (into ?? m.Items).Add(i);
        }
        void Sep() { if (m.Items.Count > 0 && m.Items[^1] is not MenuFlyoutSeparator) m.Items.Add(new MenuFlyoutSeparator()); }

        var path = row.Path;
        var isFile = !AppRows && File.Exists(path);
        if (AppRows)
        {
            if (path.Length > 0) Add("Open install folder", () => Ui.ShowInExplorer(path));
        }
        else
        {
            if (isFile && !Runnable.Contains(Path.GetExtension(path))) Add("Open", () => Ui.Open(path));
            if (Directory.Exists(path)) Add("Open", () => Ui.ShowInExplorer(path));
            if (isFile) Add("Open with…", () => Ui.OpenWith(path));
            Add("Show in File Explorer", () => Ui.ShowInExplorer(path));
            Sep();
            Add("Check if in use…", async () => await Ui.Info(XamlRoot, "In use?", await Task.Run(() => Ui.InUseReport(path))));
        }
        Sep();
        var copy = new MenuFlyoutSubItem { Text = "Copy" };
        Add("Path" + many, () => Ui.Copy(string.Join("\r\n", targets.Select(t => t.Path))), copy.Items);
        Add("Name" + many, () => Ui.Copy(string.Join("\r\n", targets.Select(t => t.Name))), copy.Items);
        Add("Details (TSV)" + many, () => Ui.Copy(string.Join("\r\n", targets.Select(t =>
            string.Join('\t', t.Name, t.Path, t.Size, t.ModifiedText, t.RiskText, t.App, t.Extra)))), copy.Items);
        m.Items.Add(copy);

        var safe = Commands.Where(c => !c.Destructive).ToList();
        if (safe.Count > 0) { Sep(); foreach (var c in safe) Add(c.Text + many, async () => await c.Run(targets)); }

        var destructive = Commands.Where(c => c.Destructive).ToList();
        if (destructive.Count > 0)
        {
            Sep();
            if (targets.Any(t => t.Risk == Core.Safety.RiskClass.Protected))
                Add("Why is this protected?", async () => await Ui.Info(XamlRoot, "Protected", ProtectedReason(targets.First(t => t.Risk == Core.Safety.RiskClass.Protected).Path)));
            else foreach (var c in destructive) Add(c.Text + many, async () => await c.Run(targets));
        }

        Sep();
        if (!AppRows && path.Length > 0) Add("Add to protected folders…", async () => await Ui.AddProtected(XamlRoot, path));
        if (AppRows) Add("Details", async () => await Ui.Info(XamlRoot, row.Name, $"Publisher: {row.App}\nVersion: {row.Extra}\nInstalled: {row.ModifiedText}\nSize: {row.SizeText}\nLocation: {(path.Length > 0 ? path : "not recorded")}\n\nIts settings live on the system drive (registry and AppData) even if it is installed on another drive."));
        else Add("Properties", () => Ui.Properties(path));
        return m;
    }

    private static string ProtectedReason(string path)
    {
        var mine = State.Settings.ProtectedPaths.FirstOrDefault(p => path.StartsWith(p.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase));
        return mine != null
            ? $"{path}\n\nIt is inside a folder you protected: {mine}. Remove it from the list in settings.json (ProtectedPaths) to allow actions."
            : $"{path}\n\nIt is part of Windows, a system file, the backup folder, or browser sign-in data (cookies, passwords, history). CDClean never offers to delete these.";
    }

    // ---------------------------------------------------------------- filters: size, age, drive

    private long _minBytes;
    private int _minMonths;
    private string _drive = "";

    private void FillDriveBox()
    {
        var drives = _all.Select(i => Path.GetPathRoot(i.Path) ?? "").Where(d => d.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(d => d).ToList();
        DriveBox.ItemsSource = new[] { "All drives" }.Concat(drives).ToList();
        DriveBox.SelectedItem = _drive.Length > 0 && drives.Contains(_drive) ? _drive : "All drives";
        MinSizeBox.Value = _minBytes / (1 << 20);
        MinAgeBox.Value = _minMonths;
    }

    private void ApplyFilters_Click(object sender, RoutedEventArgs e)
    {
        _minBytes = (long)(double.IsNaN(MinSizeBox.Value) ? 0 : MinSizeBox.Value) << 20;
        _minMonths = (int)(double.IsNaN(MinAgeBox.Value) ? 0 : MinAgeBox.Value);
        _drive = DriveBox.SelectedItem as string is { } d && d != "All drives" ? d : "";
        FiltersFlyout.Hide();
        Refresh();
    }

    private void ClearFilters_Click(object sender, RoutedEventArgs e)
    {
        _minBytes = 0; _minMonths = 0; _drive = "";
        FiltersFlyout.Hide();
        Refresh();
    }

    private bool PassesFilters(GridItem i) =>
        (_minBytes == 0 || i.Size >= _minBytes)
        && (_minMonths == 0 || (i.AgeMonths ?? -1) >= _minMonths)
        && (_drive.Length == 0 || i.Path.StartsWith(_drive, StringComparison.OrdinalIgnoreCase));

    // ---------------------------------------------------------------- narrow windows: hide low-priority columns

    private static readonly string[] LowPriority = { "Path", "Extra", "Depth", "LastUsed", "Type", "Count", "Age" };
    private readonly HashSet<string> _autoHidden = new();

    private void FitColumns(double width)
    {
        if (!_built) return;
        var narrow = width < 900;
        foreach (var c in DataColumns)
        {
            var key = (string)c.Tag;
            if (narrow && LowPriority.Contains(key) && c.Visibility == Visibility.Visible) { c.Visibility = Visibility.Collapsed; _autoHidden.Add(key); }
            else if (!narrow && _autoHidden.Remove(key)) c.Visibility = Visibility.Visible;
        }
    }

    private void Accel(VirtualKey key, VirtualKeyModifiers mods, Action run)
    {
        var a = new KeyboardAccelerator { Key = key, Modifiers = mods };
        a.Invoked += (_, e) =>
        {
            // Inside a text box, Ctrl+A selects text as usual.
            if (FocusManager.GetFocusedElement(XamlRoot) is TextBox && key == VirtualKey.A) return;
            run();
            e.Handled = true;
        };
        KeyboardAccelerators.Add(a);
    }

    // ---------------------------------------------------------------- public API used by pages

    public IReadOnlyList<GridItem> Items => _all;
    public int SelectedCount => _sel.Count;
    public int StaleCount => _stale;

    /// <summary>Selected rows that the user can currently see. Actions use only these.</summary>
    public List<GridItem> SelectedVisible => _view.Where(i => _sel.Contains(i.Key)).ToList();
    public int HiddenSelectedCount => SelectedHidden.Count;

    /// <summary>Selected rows hidden by the search. Destructive dialogs list them and leave them out by default.</summary>
    public List<GridItem> SelectedHidden
    {
        get { var vis = _view.ToHashSet(); return _all.Where(i => _sel.Contains(i.Key) && !vis.Contains(i)).ToList(); }
    }

    /// <summary>Clears both searches so hidden selections become visible.</summary>
    public void ShowHidden() { _minBytes = 0; _minMonths = 0; _drive = ""; Search.Text = ""; Within.Text = ""; Refresh(); }

    /// <summary>Raised when the focused row changes (for detail panels).</summary>
    public event EventHandler<GridItem?>? CurrentChanged;

    /// <summary>
    /// Replaces the rows. With keepSelection, selections survive a rescan by file identity, and selected
    /// items that disappeared are reported as missing instead of being silently dropped.
    /// Rows created with Checked = true start selected.
    /// </summary>
    public void SetItems(IEnumerable<GridItem> items, bool keepSelection = false)
    {
        EnsureColumns();
        _all = items.ToList();
        if (!keepSelection) _sel.Clear();
        foreach (var i in _all)
        {
            i.Owner = this;
            if (i.Checked) _sel.Add(i.Key, i.Rec.Bytes);
        }
        _stale = keepSelection ? _sel.Reconcile(_all.Select(i => i.Rec).ToList(), null).Stale : 0;
        foreach (var i in _all) i.SetChecked(_sel.Contains(i.Key));
        Refresh();
    }

    /// <summary>Removes rows (for example after they were deleted) and their selection.</summary>
    public void RemoveWhere(Func<GridItem, bool> pred)
    {
        foreach (var i in _all.Where(pred)) _sel.Remove(i.Key);
        _all = _all.Where(i => !pred(i)).ToList();
        Refresh();
    }

    /// <summary>Why an action cannot run now, or null. Missing selections block destructive actions until reviewed.</summary>
    public string? BlockReason() =>
        _stale > 0 ? $"{_stale} selected items no longer exist. Unselect them first." :
        SelectedVisible.Count == 0 ? "Tick at least one visible row first." : null;

    /// <summary>One line for confirmation dialogs about hidden selections being left out.</summary>
    public string HiddenNote() =>
        HiddenSelectedCount > 0 ? $"{HiddenSelectedCount} selected items are hidden by the search and are left out.\n\n" : "";

    /// <summary>Targets for a destructive action: visible selections, plus hidden ones flagged so the review leaves them out by default.</summary>
    public List<Target> Targets(Func<GridItem, IEnumerable<Target>> make) =>
        SelectedVisible.SelectMany(make).Concat(SelectedHidden.SelectMany(i => make(i).Select(t => t with { Hidden = true }))).ToList();

    // ---------------------------------------------------------------- view

    private bool Primary(GridItem i) => i.Matches(Search.Text.Trim());
    private bool Visible(GridItem i) => Primary(i) && i.Matches(Within.Text.Trim()) && PassesFilters(i);

    private void Refresh()
    {
        IEnumerable<GridItem> v = _all.Where(Visible);
        if (_sort.Count > 0) v = v.OrderBy(x => x, Comparer<GridItem>.Create(CompareRows)); // OrderBy is stable
        _view = v.ToList();
        Table.ItemsSource = _view;
        Empty.Text = _all.Count == 0 ? EmptyText : "No rows match the search. Clear the search boxes to see everything.";
        Empty.Visibility = _view.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        UpdateChips();
        UpdateBar();
    }

    private void UpdateBar()
    {
        var st = _sel.ApplyView(_all.Select(i => i.Rec).ToList(), r => Visible((GridItem)r.Tag!));
        SelText.Text = st.SelectedHidden > 0
            ? $"{st.SelectedTotal:N0} selected · {Fmt.Size(st.SelectedTotalBytes)}  ({st.SelectedVisible:N0} visible, {st.SelectedHidden:N0} hidden)"
            : $"{st.SelectedTotal:N0} selected · {Fmt.Size(st.SelectedTotalBytes)}";
        HiddenBar.IsOpen = st.SelectedHidden - _stale > 0;
        HiddenBar.Message = $"{st.SelectedHidden - _stale:N0} selected items are hidden by the search ({Fmt.Size(st.SelectedHiddenBytes)}). Actions apply to the {st.SelectedVisible:N0} visible ones; hidden ones are left out.";
        StaleBar.IsOpen = _stale > 0;
        StaleBar.Message = $"{_stale:N0} selected items no longer exist after the rescan. Actions are blocked until you unselect them.";
        HeaderCheck.IsChecked = st.VisibleCount == 0 || st.SelectedVisible == 0 ? false
            : st.SelectedVisible == st.VisibleCount ? true : null;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SyncChecks()
    {
        foreach (var i in _all) i.SetChecked(_sel.Contains(i.Key));
        UpdateBar();
    }

    internal void OnRowChecked(GridItem item, bool value)
    {
        if (value) _sel.Add(item.Key, item.Rec.Bytes); else _sel.Remove(item.Key);
        UpdateBar();
    }

    private void UpdateChips()
    {
        Chips.Children.Clear();
        void Chip(string text, Action clear)
        {
            var b = new Button { Content = text + "  ✕", Padding = new Thickness(10, 2, 10, 2), CornerRadius = new CornerRadius(12) };
            b.Click += (_, _) => clear();
            Chips.Children.Add(b);
        }
        if (Search.Text.Trim().Length > 0) Chip($"Search: {Search.Text.Trim()}", () => Search.Text = "");
        if (Within.Text.Trim().Length > 0) Chip($"Within: {Within.Text.Trim()}", () => Within.Text = "");
        if (_minBytes > 0) Chip($"Size ≥ {_minBytes >> 20} MB", () => { _minBytes = 0; Refresh(); });
        if (_minMonths > 0) Chip($"Age ≥ {_minMonths} months", () => { _minMonths = 0; Refresh(); });
        if (_drive.Length > 0) Chip($"Drive {_drive.TrimEnd('\\')}", () => { _drive = ""; Refresh(); });
        if (Chips.Children.Count > 1) Chip("Clear all", () => { _minBytes = 0; _minMonths = 0; _drive = ""; Search.Text = ""; Within.Text = ""; Refresh(); });
        Chips.Visibility = Chips.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Search_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args) => Refresh();

    // ---------------------------------------------------------------- selection commands

    private List<SelRecord> Recs() => _all.Select(i => i.Rec).ToList();

    private void SelectVisible_Click(object sender, RoutedEventArgs e)
    {
        foreach (var i in _view) _sel.Add(i.Key, i.Rec.Bytes);
        SyncChecks();
    }

    private void SelectMatching_Click(object sender, RoutedEventArgs e)
    {
        _sel.SelectAllMatching(Recs(), r => Primary((GridItem)r.Tag!));
        SyncChecks();
    }

    private void Invert_Click(object sender, RoutedEventArgs e)
    {
        _sel.InvertVisible(Recs(), r => Visible((GridItem)r.Tag!));
        SyncChecks();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _sel.Clear();
        _stale = 0;
        SyncChecks();
    }

    private void HeaderCheck_Click(object sender, RoutedEventArgs e)
    {
        if (_view.Count > 0 && _view.All(i => _sel.Contains(i.Key))) foreach (var i in _view) _sel.Remove(i.Key);
        else foreach (var i in _view) _sel.Add(i.Key, i.Rec.Bytes);
        SyncChecks();
    }

    private void ShowHidden_Click(object sender, RoutedEventArgs e) => ShowHidden();

    private void DropStale_Click(object sender, RoutedEventArgs e)
    {
        var present = _all.Select(i => i.Key).ToHashSet();
        foreach (var k in _sel.Keys.Where(k => !present.Contains(k)).ToList()) _sel.Remove(k);
        _stale = 0;
        UpdateBar();
    }

    private void Table_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (Table.SelectedItem is GridItem i) RowInvoked?.Invoke(this, i);
    }

    private void Table_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (Table.SelectedItem is not GridItem i) return;
        if (e.Key == VirtualKey.Space) { i.Checked = !i.Checked; e.Handled = true; }
        else if (e.Key == VirtualKey.Enter) { RowInvoked?.Invoke(this, i); e.Handled = true; }
    }

    // ---------------------------------------------------------------- columns, sorting, layout

    private GridLayout Layout
    {
        get
        {
            var all = State.Settings.GridLayouts;
            if (!all.TryGetValue(LayoutKey, out var l)) all[LayoutKey] = l = new GridLayout();
            return l;
        }
    }

    private void EnsureColumns()
    {
        if (_built) return;
        _built = true;
        Table.Columns.Add(new DataGridTemplateColumn
        {
            Header = "", CellTemplate = (DataTemplate)Resources["CheckCell"], Tag = "Check",
            Width = new DataGridLength(48), CanUserSort = false, CanUserResize = false, CanUserReorder = false,
        });
        foreach (var part in ColumnSpec.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var bits = part.Split(':', 2);
            if (!Defs.TryGetValue(bits[0], out var d)) continue;
            var header = bits.Length > 1 ? bits[1] : d.Header;
            DataGridColumn col = bits[0] == "Risk"
                ? new DataGridTemplateColumn { CellTemplate = (DataTemplate)Resources["RiskCell"] }
                : new DataGridTextColumn
                {
                    Binding = new Binding { Path = new PropertyPath(d.Bind) },
                    ElementStyle = (Style)Resources[d.Right ? "RightText" : "LeftText"],
                };
            col.Header = header;
            col.Tag = bits[0];
            col.Width = new DataGridLength(d.Width);
            Table.Columns.Add(col);
        }
        RestoreLayout();
    }

    private IEnumerable<DataGridColumn> DataColumns => Table.Columns.Where(c => (string)c.Tag != "Check");

    private void RestoreLayout()
    {
        var l = Layout;
        foreach (var c in DataColumns)
        {
            var key = (string)c.Tag;
            if (l.Widths.TryGetValue(key, out var w) && w > 20) c.Width = new DataGridLength(w);
            c.Visibility = l.Hidden.Contains(key) ? Visibility.Collapsed : Visibility.Visible;
        }
        var order = l.Order.Where(k => DataColumns.Any(c => (string)c.Tag == k)).ToList();
        for (var n = 0; n < order.Count; n++)
            DataColumns.First(c => (string)c.Tag == order[n]).DisplayIndex = n + 1; // 0 stays the checkbox
        _sort.Clear();
        foreach (var s in l.Sort.Take(3))
        {
            var p = s.Split(':');
            if (DataColumns.FirstOrDefault(c => (string)c.Tag == p[0]) is { } col)
            {
                _sort.Add((p[0], p.Length > 1 && p[1] == "desc"));
                col.SortDirection = _sort[^1].Desc ? DataGridSortDirection.Descending : DataGridSortDirection.Ascending;
            }
        }
    }

    private void SaveLayout()
    {
        if (!_built || LayoutKey.Length == 0) return;
        var l = Layout;
        l.Widths = DataColumns.ToDictionary(c => (string)c.Tag, c => c.ActualWidth > 0 ? c.ActualWidth : c.Width.Value);
        l.Order = DataColumns.OrderBy(c => c.DisplayIndex).Select(c => (string)c.Tag).ToList();
        l.Hidden = DataColumns.Where(c => c.Visibility == Visibility.Collapsed && !_autoHidden.Contains((string)c.Tag)).Select(c => (string)c.Tag).ToList();
        l.Sort = _sort.Select(s => $"{s.Key}:{(s.Desc ? "desc" : "asc")}").ToList();
        try { State.Settings.Save(); } catch { /* layout is a convenience */ }
    }

    private MenuFlyout ColumnMenu()
    {
        var m = new MenuFlyout();
        m.Opening += (_, _) =>
        {
            m.Items.Clear();
            foreach (var c in DataColumns)
            {
                var item = new ToggleMenuFlyoutItem { Text = c.Header?.ToString() ?? "", IsChecked = c.Visibility == Visibility.Visible };
                var col = c;
                item.Click += (_, _) => { col.Visibility = item.IsChecked ? Visibility.Visible : Visibility.Collapsed; SaveLayout(); };
                m.Items.Add(item);
            }
            m.Items.Add(new MenuFlyoutSeparator());
            var reset = new MenuFlyoutItem { Text = "Reset table layout" };
            reset.Click += (_, _) => ResetLayout();
            m.Items.Add(reset);
        };
        return m;
    }

    private void ResetLayout()
    {
        State.Settings.GridLayouts.Remove(LayoutKey);
        foreach (var c in DataColumns)
        {
            c.Visibility = Visibility.Visible;
            c.Width = new DataGridLength(Defs[(string)c.Tag].Width);
            c.SortDirection = null;
        }
        var n = 1;
        foreach (var c in DataColumns.ToList()) c.DisplayIndex = n++;
        _sort.Clear();
        SaveLayout();
        Refresh();
    }

    /// <summary>Click sorts by one column; Ctrl+click adds up to three sort keys.</summary>
    private void Table_Sorting(object? sender, DataGridColumnEventArgs e)
    {
        var key = (string)e.Column.Tag;
        if (key == "Check") return;
        var ctrl = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
        var existing = _sort.FindIndex(s => s.Key == key);
        var desc = existing >= 0 ? !_sort[existing].Desc : key is "Size" or "Count" or "Age" or "Risk"; // big things first
        if (!ctrl)
        {
            _sort.Clear();
            foreach (var c in DataColumns) if (c != e.Column) c.SortDirection = null;
            _sort.Add((key, desc));
        }
        else if (existing >= 0) _sort[existing] = (key, desc);
        else if (_sort.Count < 3) _sort.Add((key, desc));
        e.Column.SortDirection = desc ? DataGridSortDirection.Descending : DataGridSortDirection.Ascending;
        Refresh();
        SaveLayout();
    }

    private int CompareRows(GridItem a, GridItem b)
    {
        foreach (var (key, desc) in _sort)
        {
            bool na = IsNull(a, key), nb = IsNull(b, key);
            if (na || nb) { if (na != nb) return na ? 1 : -1; continue; } // empty values always last
            var c = key switch
            {
                "Name" => StrCmpLogicalW(a.Name, b.Name),
                "Path" => StrCmpLogicalW(a.Path, b.Path),
                "Size" => a.Size.CompareTo(b.Size),
                "Count" => a.Count!.Value.CompareTo(b.Count!.Value),
                "Age" => b.Modified!.Value.CompareTo(a.Modified!.Value), // older = larger age
                "Modified" => a.Modified!.Value.CompareTo(b.Modified!.Value),
                "Depth" => a.Depth!.Value.CompareTo(b.Depth!.Value),
                "Risk" => a.Risk!.Value.CompareTo(b.Risk!.Value),
                "Type" => string.Compare(a.Type, b.Type, StringComparison.OrdinalIgnoreCase),
                "App" => StrCmpLogicalW(a.App, b.App),
                "Extra" => StrCmpLogicalW(a.Extra, b.Extra),
                "LastUsed" => string.Compare(a.LastUsed, b.LastUsed, StringComparison.OrdinalIgnoreCase),
                _ => string.Compare(a.Action, b.Action, StringComparison.OrdinalIgnoreCase),
            };
            if (c != 0) return desc ? -c : c;
        }
        return 0;
    }

    private static bool IsNull(GridItem i, string key) => key switch
    {
        "Size" => i.Size < 0,
        "Count" => i.Count is null,
        "Age" or "Modified" => i.Modified is null,
        "Depth" => i.Depth is null,
        "Risk" => i.Risk is null,
        "App" => i.App.Length == 0,
        "Extra" => i.Extra.Length == 0,
        "LastUsed" => i.LastUsed.Length == 0,
        _ => false,
    };

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    private static extern int StrCmpLogicalW(string a, string b);
}
