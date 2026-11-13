using System.Diagnostics;
using CDClean.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace CDClean.App;

/// <summary>One list row. Every list in the app uses this shape.</summary>
[Microsoft.UI.Xaml.Data.Bindable]
public sealed class Row
{
    public string Title { get; set; } = "";
    public string Detail { get; set; } = "";
    public string Size { get; set; } = "";
    public double Bar { get; set; }
    public bool Selected { get; set; }
    public object? Tag { get; set; }

    /// <summary>Bar width in pixels (0-200) for a value relative to the largest.</summary>
    public static double BarOf(long value, long max) => max <= 0 ? 0 : Math.Max(2, 200.0 * value / max);
}

public static class Fmt
{
    public static string Size(long b) =>
        b >= 1L << 30 ? $"{b / 1073741824.0:0.00} GB" :
        b >= 1L << 20 ? $"{b / 1048576.0:0.0} MB" :
        $"{b / 1024.0:0} KB";
}

/// <summary>App-wide state: settings and the last scan.</summary>
public static class State
{
    public static AppSettings Settings { get; } = Init(AppSettings.Load());

    private static AppSettings Init(AppSettings s)
    {
        Audit.Redact = s.RedactAudit;
        try { Audit.Rotate(); } catch { /* log housekeeping must never stop the app */ }
        return s;
    }
    public static List<RuleResult> LastScan { get; set; } = new();

    /// <summary>Rules the Clean preview and "Quick clean" consider: safe ones, or the ones ticked in Rules. Scheduled runs still filter to Safe.</summary>
    public static bool IsAutoRule(Rule r) =>
        Settings.EnabledRules.Count == 0 ? r.Safe : Settings.EnabledRules.Contains(r.Name);

    public static List<Rule> LoadRules() => Rules.Load(Settings.ResolvedRulesPath);
}

public static class Ui
{
    private static readonly SemaphoreSlim DialogGate = new(1, 1);

    /// <summary>WinUI allows one ContentDialog at a time and throws on a second. Dialogs wait their turn instead.</summary>
    public static async Task<ContentDialogResult> Show(ContentDialog d)
    {
        await DialogGate.WaitAsync();
        try { return await d.ShowAsync(); }
        finally { DialogGate.Release(); }
    }

    public static async Task<bool> Confirm(XamlRoot root, string title, string text, string yes = "Continue")
    {
        var d = new ContentDialog
        {
            XamlRoot = root, Title = title, Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap },
            PrimaryButtonText = yes, CloseButtonText = "Cancel", DefaultButton = ContentDialogButton.Close,
        };
        return await Show(d) == ContentDialogResult.Primary;
    }

    /// <summary>Typed confirmation for a dangerous action. The primary button stays disabled until the token matches.</summary>
    public static async Task<bool> TypedConfirm(XamlRoot root, string title, string text, string token, string yes = "Unlock")
    {
        var box = new TextBox { PlaceholderText = token, IsTextPredictionEnabled = false };
        var body = new StackPanel { Spacing = 8 };
        body.Children.Add(new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap });
        body.Children.Add(box);
        var d = new ContentDialog
        {
            XamlRoot = root, Title = title, Content = body, PrimaryButtonText = yes,
            CloseButtonText = "Cancel", DefaultButton = ContentDialogButton.Close, IsPrimaryButtonEnabled = false,
        };
        box.TextChanged += (_, _) => d.IsPrimaryButtonEnabled = box.Text.Trim() == token;
        return await Show(d) == ContentDialogResult.Primary;
    }

    public static async Task Info(XamlRoot root, string title, string text)
    {
        var d = new ContentDialog
        {
            XamlRoot = root, Title = title, CloseButtonText = "OK",
            Content = new ScrollViewer { MaxHeight = 400, Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true } },
        };
        await Show(d);
    }

    public static async Task<string?> PickFolder()
    {
        var picker = new FolderPicker();
        picker.FileTypeFilter.Add("*");
        WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(App.Main!));
        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }

    /// <summary>Opens a file with its default program. Callers never pass programs or scripts.</summary>
    public static void Open(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); } catch { }
    }

    public static void OpenWith(string path)
    {
        try { Process.Start("rundll32.exe", "shell32.dll,OpenAs_RunDLL " + path); } catch { }
    }

    public static void Copy(string text)
    {
        var d = new Windows.ApplicationModel.DataTransfer.DataPackage();
        d.SetText(text);
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(d);
    }

    /// <summary>Plain-language answer to "is this in use, and by what?" using the Restart Manager.</summary>
    public static string InUseReport(string path)
    {
        var files = File.Exists(path) ? new[] { path } : Scanner.Files(path).Take(2000).ToArray();
        var holders = new SortedSet<string>();
        var locked = 0;
        foreach (var f in files)
        {
            if (!Core.Native.Files.IsLocked(f)) continue;
            locked++;
            foreach (var h in Core.Native.Locks.Holders(f)) holders.Add(h);
        }
        if (locked == 0) return $"{path}\n\nNot in use. No program holds {(files.Length == 1 ? "it" : $"any of the {files.Length:N0} files checked")} open.";
        return $"{path}\n\n{locked:N0} file(s) are in use" + (holders.Count > 0 ? " by:\n" + string.Join("\n", holders) : " (Windows did not say by which program).") +
               "\n\nClose those programs, then try again. CDClean skips files that are in use.";
    }

    public static async Task AddProtected(XamlRoot root, string path)
    {
        if (!await Confirm(root, "Protect this?", $"{path}\n\nCDClean will never offer to remove anything here. You can undo this in settings.json (ProtectedPaths).", "Protect")) return;
        if (!State.Settings.ProtectedPaths.Contains(path, StringComparer.OrdinalIgnoreCase)) State.Settings.ProtectedPaths.Add(path);
        State.Settings.Save();
    }

    public static void Properties(string path)
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Main!);
        SHObjectProperties(hwnd, 0x2 /*SHOP_FILEPATH*/, path, null);
    }

    [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern bool SHObjectProperties(IntPtr hwnd, uint type, string name, string? page);

    /// <summary>
    /// Fills a drive picker. Selects the drive of `current` if connected; never falls back silently to another drive.
    /// </summary>
    public static void FillDrives(ComboBox box, string current)
    {
        var vols = Volumes.List();
        box.ItemsSource = vols;
        box.DisplayMemberPath = nameof(Volume.Display);
        box.SelectedItem = vols.FirstOrDefault(v => current.StartsWith(v.Root, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks a scan folder before a long scan: the drive must be connected (no silent switch to another drive),
    /// and removable drives get a warning first. Returns false to stop.
    /// </summary>
    public static async Task<bool> CheckScope(XamlRoot root, string folder, TextBlock status)
    {
        if (!Volumes.Available(folder))
        {
            status.Text = $"{folder} is not available. Its drive may be disconnected. Pick another folder; CDClean will not switch drives on its own.";
            return false;
        }
        var vol = Volumes.List().FirstOrDefault(v => folder.StartsWith(v.Root, StringComparison.OrdinalIgnoreCase));
        if (vol?.Type == DriveType.Removable)
            return await Confirm(root, "Scan a removable drive?", $"{vol.Display}\n\nScanning a USB drive can take a long time. If you unplug it during the scan, the scan stops and shows what it found so far.", "Scan");
        return true;
    }

    public static void ShowInExplorer(string path)
    {
        if (File.Exists(path)) Process.Start("explorer.exe", $"/select,\"{path}\"");
        else if (Directory.Exists(path)) Process.Start("explorer.exe", $"\"{path}\"");
    }

    /// <summary>
    /// The only way pages remove files: plan (safety checks), show what will happen, then run through the Executor.
    /// Confirming the dialog accepts the warnings it lists. Returns null if the user cancelled or nothing could be removed.
    /// </summary>
    public static async Task<JobReport?> RunJob(XamlRoot root, IReadOnlyList<Target> targets, string title, string verb,
        Action? showHidden = null, IProgress<CleanProgress>? progress = null, CancellationToken ct = default)
    {
        var s = State.Settings;
        var plan = await Task.Run(() => Executor.Plan(targets, s.Policy(), ct));
        if (plan.Items.Count == plan.Blocked)
        {
            await Info(root, title, "Nothing here can be removed.\n\n" + plan.Describe(s.BackupRoot));
            return null;
        }
        var (choice, kept) = await Review.Delete(root, plan, title, verb);
        if (choice == ReviewChoice.ShowHidden) showHidden?.Invoke();
        if (choice != ReviewChoice.Run) return null;
        // Confirming the review accepts the warnings it listed for the items left ticked.
        return await Task.Run(() => Executor.Execute(kept!, s.Exec(proceedOnWarn: true), progress, ct));
    }
}
