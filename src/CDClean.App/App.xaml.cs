using CDClean.Core;
using CDClean.Core.Disk;
using Microsoft.UI.Xaml;

namespace CDClean.App;

public partial class App : Application
{
    public static MainWindow? Main { get; private set; }

    public App()
    {
        InitializeComponent();
        // Write UI crashes to a log so they can be diagnosed (Windows only records "stowed exception").
        UnhandledException += (_, e) =>
        {
            try
            {
                Directory.CreateDirectory(AppSettings.LogDir);
                File.AppendAllText(Path.Combine(AppSettings.LogDir, "crash.log"), $"{DateTime.Now:s} {e.Message}\n{e.Exception}\n\n");
            }
            catch { }
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var cli = Environment.GetCommandLineArgs();
        // Elevated MFT scan: runs as admin, writes compact MFT entries to a temporary file, then exits.
        var mftIdx = Array.IndexOf(cli, "--mft-scan");
        if (mftIdx >= 0 && mftIdx + 2 < cli.Length)
        {
            var drive = cli[mftIdx + 1];
            var outFile = cli[mftIdx + 2];
            try
            {
                var entries = MftScanner.ReadVolume(drive[0]);
                if (entries.Count == 0) throw new InvalidOperationException("MFT scan returned 0 entries. The volume may not be NTFS or the record size/offset is wrong.");
                using (var fs = new FileStream(outFile, FileMode.Create, FileAccess.Write))
                    MftScanner.WriteEntries(fs, entries);
                File.WriteAllText(outFile + ".count", entries.Count.ToString());
            }
            catch (Exception e)
            {
                try { File.WriteAllText(outFile + ".err", e.ToString()); } catch { }
            }
            Exit();
            return;
        }
        // Scheduled runs: clean the auto rules without opening a window.
        if (cli.Contains("--clean-safe"))
        {
            try
            {
                var s = State.Settings;
                var results = Scanner.ScanAll(State.LoadRules());
                Cleaner.RunScheduled(results, State.IsAutoRule, s.BackupRoot, policy: s.Policy());
            }
            catch (Exception e)
            {
                Directory.CreateDirectory(AppSettings.LogDir);
                File.AppendAllText(Path.Combine(AppSettings.LogDir, "scheduled.log"), $"{DateTime.Now:s} {e}\n");
            }
            Exit();
            return;
        }
        Main = new MainWindow();
        Main.Activate();
        // Review queue from a previous scheduled run: show on next launch.
        if (Automation.HasPending)
        {
            _ = Main.DispatcherQueue.TryEnqueue(async () =>
            {
                var items = Automation.Pending();
                await Ui.Info(Main.Content.XamlRoot, "Review queue",
                    $"{items.Count} item(s) from the last scheduled run need review:\n" +
                    string.Join("\n", items.Take(20).Select(i => $"{i.Rule}: {i.Path} ({Fmt.Size(i.Bytes)})")) +
                    (items.Count > 20 ? $"\n...and {items.Count - 20} more." : ""));
            });
        }
    }
}
