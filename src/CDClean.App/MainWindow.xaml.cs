using CDClean.App.Views;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using Microsoft.UI.Xaml.Controls;

namespace CDClean.App;

public sealed partial class MainWindow : Window
{
    private static readonly Dictionary<string, Type> Pages = new()
    {
        ["Dashboard"] = typeof(DashboardPage),
        ["DiskMap"] = typeof(DiskMapPage),
        ["Scan"] = typeof(ScanPage),
        ["Clean"] = typeof(CleanPage),
        ["System"] = typeof(SystemPage),
        ["Apps"] = typeof(AppsPage),
        ["Leftovers"] = typeof(LeftoversPage),
        ["OldFiles"] = typeof(OldFilesPage),
        ["LargeFiles"] = typeof(LargeFilesPage),
        ["Duplicates"] = typeof(DuplicatesPage),
        ["Organizer"] = typeof(OrganizerPage),
        ["Backups"] = typeof(BackupsPage),
        ["Schedule"] = typeof(SchedulePage),
        ["Rules"] = typeof(RulesPage),
        ["Settings"] = typeof(SettingsPage),
    };

    private const int MinW = 1024, MinH = 640; // report §4.15: usable minimum, in view pixels

    public MainWindow()
    {
        InitializeComponent();
        Title = $"SpacePilot {typeof(App).Assembly.GetName().Version?.ToString(3)}";
        var icon = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
        if (File.Exists(icon)) AppWindow.SetIcon(icon);
        RestorePlacement();
        AppWindow.Changed += (_, e) => { if (e.DidSizeChange) KeepMinimumSize(); };
        AppWindow.Closing += (_, _) => SavePlacement();
        Nav.SelectedItem = Nav.MenuItems[0];
    }

    private double Scale => GetDpiForWindow(WinRT.Interop.WindowNative.GetWindowHandle(this)) / 96.0;

    private void KeepMinimumSize()
    {
        if (AppWindow.Presenter is OverlappedPresenter { State: not OverlappedPresenterState.Restored }) return;
        int w = (int)(MinW * Scale), h = (int)(MinH * Scale);
        var s = AppWindow.Size;
        if (s.Width < w || s.Height < h) AppWindow.Resize(new SizeInt32(Math.Max(s.Width, w), Math.Max(s.Height, h)));
    }

    /// <summary>Restores the last position and size if they still fit a connected screen; otherwise a default size.</summary>
    private void RestorePlacement()
    {
        if (State.Settings.WindowRect is [var x, var y, var w, var h] && w > 0 && h > 0)
        {
            var area = DisplayArea.GetFromRect(new RectInt32(x, y, w, h), DisplayAreaFallback.None);
            if (area is not null)
            {
                var wa = area.WorkArea;
                w = Math.Min(w, wa.Width); h = Math.Min(h, wa.Height);
                x = Math.Clamp(x, wa.X, wa.X + wa.Width - w); y = Math.Clamp(y, wa.Y, wa.Y + wa.Height - h);
                AppWindow.MoveAndResize(new RectInt32(x, y, w, h));
                return;
            }
        }
        AppWindow.Resize(new SizeInt32((int)(1240 * Scale), (int)(840 * Scale)));
    }

    private void SavePlacement()
    {
        if (AppWindow.Presenter is OverlappedPresenter { State: not OverlappedPresenterState.Restored }) return;
        State.Settings.WindowRect = new[] { AppWindow.Position.X, AppWindow.Position.Y, AppWindow.Size.Width, AppWindow.Size.Height };
        try { State.Settings.Save(); } catch { }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    public void Go(string tag)
    {
        foreach (var item in Nav.MenuItems.OfType<NavigationViewItem>())
            if ((string)item.Tag == tag) { Nav.SelectedItem = item; return; }
    }

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var tag = args.IsSettingsSelected ? "Settings" : (args.SelectedItem as NavigationViewItem)?.Tag as string;
        if (tag is not null && Pages.TryGetValue(tag, out var page) && ContentFrame.CurrentSourcePageType != page)
            ContentFrame.Navigate(page);
    }
}
