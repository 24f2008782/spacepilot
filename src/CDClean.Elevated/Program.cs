using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Win32;

// Runs one admin task. Started by CDClean with the "runas" verb, so Windows shows UAC first.
// Writes lines to the log file: "PROGRESS 23.7", "LOG text", and finally "DONE ok" or "DONE fail".
// Usage: CDClean.Elevated.exe <task-id> <log-file>

if (args.Length < 2) return 2;
var task = args[0];
using var log = new StreamWriter(args[1], append: false) { AutoFlush = true };
void Log(string m) => log.WriteLine("LOG " + m);
void Progress(double p) => log.WriteLine("PROGRESS " + p.ToString("0.0", CultureInfo.InvariantCulture));

var percent = new Regex(@"(\d+(?:\.\d+)?)%");

// Runs a tool, sending % lines as progress and other lines to the log.
int Run(string exe, string arguments, bool throwOnFail = true)
{
    Log($"> {exe} {arguments}");
    var psi = new ProcessStartInfo(exe, arguments)
    {
        RedirectStandardOutput = true, RedirectStandardError = true,
        UseShellExecute = false, CreateNoWindow = true,
    };
    using var p = Process.Start(psi)!;
    p.ErrorDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) Log(e.Data.Trim()); };
    p.BeginErrorReadLine();
    while (p.StandardOutput.ReadLine() is { } line) // ReadLine also splits on DISM's bare \r
    {
        var t = line.Trim();
        if (t.Length == 0) continue;
        var m = percent.Match(t);
        if (t.StartsWith('[') && m.Success) Progress(double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture));
        else Log(t);
    }
    p.WaitForExit();
    if (p.ExitCode != 0 && throwOnFail) throw new Exception($"{exe} exited with code {p.ExitCode}");
    return p.ExitCode;
}

string Capture(string exe, string arguments)
{
    var psi = new ProcessStartInfo(exe, arguments) { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
    using var p = Process.Start(psi)!;
    var output = p.StandardOutput.ReadToEnd();
    p.WaitForExit();
    return output;
}

long FreeC() => new DriveInfo("C").AvailableFreeSpace;

try
{
    var before = FreeC();
    switch (task)
    {
        case "winsxs":
            Run("dism.exe", "/Online /Cleanup-Image /StartComponentCleanup");
            break;
        case "winsxs-deep":
            Run("dism.exe", "/Online /Cleanup-Image /StartComponentCleanup /ResetBase");
            break;
        case "reserved-off":
            Run("dism.exe", "/Online /Set-ReservedStorageState /State:Disabled");
            break;
        case "reserved-on":
            Run("dism.exe", "/Online /Set-ReservedStorageState /State:Enabled");
            break;
        case "drivers":
        {
            // Keep the newest version of each driver package (by original INF name).
            // No /force, so packages a device is using are refused by Windows and kept.
            var text = Capture("pnputil.exe", "/enum-drivers");
            var blocks = Regex.Split(text, @"(?=Published Name:)").Where(b => b.Contains("Published Name"));
            var drivers = blocks.Select(b => new
            {
                Pub = Regex.Match(b, @"Published Name:\s+(\S+)").Groups[1].Value,
                Orig = Regex.Match(b, @"Original Name:\s+(\S+)").Groups[1].Value,
                Date = DateTime.TryParseExact(Regex.Match(b, @"Driver Version:\s+(\S+)").Groups[1].Value,
                    "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : DateTime.MinValue,
                Ver = Version.TryParse(Regex.Match(b, @"Driver Version:\s+\S+\s+(\S+)").Groups[1].Value, out var v) ? v : new Version(0, 0),
            }).Where(x => x.Pub.Length > 0 && x.Orig.Length > 0).ToList();
            var groups = drivers.GroupBy(x => x.Orig, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).ToList();
            if (groups.Count == 0) Log("No duplicate driver packages found.");
            var i = 0; var total = groups.Sum(g => g.Count() - 1);
            foreach (var g in groups)
            {
                var keep = g.OrderByDescending(x => x.Date).ThenByDescending(x => x.Ver).First();
                foreach (var old in g.Where(x => x.Pub != keep.Pub))
                {
                    var code = Run("pnputil.exe", $"/delete-driver {old.Pub}", throwOnFail: false);
                    Log($"{old.Pub} ({old.Orig} {old.Ver}): {(code == 0 ? "removed" : "kept (in use)")}");
                    Progress(++i * 100.0 / total);
                }
            }
            break;
        }
        case "wu-cache":
        {
            Run("net.exe", "stop wuauserv", throwOnFail: false);
            Run("net.exe", "stop bits", throwOnFail: false);
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"SoftwareDistribution\Download");
            var items = Directory.Exists(dir) ? Directory.GetFileSystemEntries(dir) : Array.Empty<string>();
            var n = 0;
            foreach (var item in items)
            {
                try { if (Directory.Exists(item)) Directory.Delete(item, true); else File.Delete(item); }
                catch (Exception e) { Log($"skipped {item}: {e.Message}"); }
                Progress(++n * 100.0 / Math.Max(1, items.Length));
            }
            Run("net.exe", "start bits", throwOnFail: false);
            Run("net.exe", "start wuauserv", throwOnFail: false);
            break;
        }
        case "do-cache":
            Run("powershell.exe", "-NoProfile -Command Delete-DeliveryOptimizationCache -Force");
            break;
        case "cleanmgr":
        {
            // Tick every Disk Cleanup item for profile 1, then run it silently.
            using (var root = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches", writable: true))
                foreach (var name in root?.GetSubKeyNames() ?? Array.Empty<string>())
                    try { using var k = root!.OpenSubKey(name, writable: true); k?.SetValue("StateFlags0001", 2, RegistryValueKind.DWord); }
                    catch (Exception e) { Log($"could not select {name}: {e.Message}"); }
            Log("Disk Cleanup is running. This can take several minutes.");
            Run("cleanmgr.exe", "/sagerun:1", throwOnFail: false);
            break;
        }
        case "hibernate-off":
            Run("powercfg.exe", "/h off");
            break;
        case "restore-oldest":
            Run("vssadmin.exe", "delete shadows /for=C: /oldest /quiet", throwOnFail: false);
            break;
        default:
            throw new ArgumentException($"Unknown task: {task}");
    }
    Progress(100);
    Log($"Freed on C: {(FreeC() - before) / 1073741824.0:0.00} GB");
    log.WriteLine("DONE ok");
    return 0;
}
catch (Exception e)
{
    Log("Error: " + e.Message);
    log.WriteLine("DONE fail");
    return 1;
}
