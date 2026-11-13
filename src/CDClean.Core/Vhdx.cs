using System.Diagnostics;

namespace CDClean.Core;

/// <summary>VHDX compactor (F-20): lists .vhdx files and compacts them. Refuses while the VM/container is running.</summary>
public sealed record VhdxFile(string Path, long SizeBytes, bool InUse);

public static class Vhdx
{
    public static List<VhdxFile> List(string root, CancellationToken ct = default)
    {
        var result = new List<VhdxFile>();
        if (!Directory.Exists(root)) return result;
        try
        {
            foreach (var f in Directory.EnumerateFiles(root, "*.vhdx", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var fi = new FileInfo(f);
                    result.Add(new VhdxFile(f, fi.Length, IsInUse(f)));
                }
                catch { }
            }
        }
        catch { }
        return result.OrderByDescending(v => v.SizeBytes).ToList();
    }

    public static bool IsInUse(string path)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            return false;
        }
        catch (IOException) { return true; }
        catch { return false; }
    }

    /// <summary>Compacts a VHDX. Returns (ok, message). Refuses if in use.</summary>
    public static (bool Ok, string Message) Compact(string path, CancellationToken ct = default)
    {
        if (IsInUse(path)) return (false, "The VHDX is in use. Stop the VM/container first.");
        try
        {
            // Prefer Hyper-V's Optimize-VHD.
            var psi = new ProcessStartInfo("powershell.exe",
                $"-NoProfile -Command \"Optimize-VHD -Path '{path}' -Mode Full\"")
            { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi);
            if (p is null) return (false, "Could not start PowerShell.");
            var out_ = p.StandardOutput.ReadToEnd();
            var err = p.StandardError.ReadToEnd();
            p.WaitForExit(600000);
            ct.ThrowIfCancellationRequested();
            if (p.ExitCode != 0) return (false, string.IsNullOrWhiteSpace(err) ? "Optimize-VHD failed (Hyper-V module may be missing)." : err.Trim());
            return (true, string.IsNullOrWhiteSpace(out_) ? "Compacted." : out_.Trim());
        }
        catch (Exception e) { return (false, e.Message); }
    }
}
