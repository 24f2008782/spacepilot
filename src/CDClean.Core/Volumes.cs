using System.Runtime.InteropServices;

namespace CDClean.Core;

/// <summary>One drive as the drive pickers show it (report §4.9.1).</summary>
public sealed record Volume(string Root, string Label, string FileSystem, DriveType Type, long Total, long Free, long ClusterBytes, bool HardLinks)
{
    public string Letter => Root.TrimEnd('\\');
    public bool IsSystem => string.Equals(Root, Path.GetPathRoot(Environment.SystemDirectory), StringComparison.OrdinalIgnoreCase);
    public string Display => $"{Letter} {(Label.Length > 0 ? Label : TypeName)} · {FmtGb(Free)} free of {FmtGb(Total)}";
    public string TypeName => Type switch { DriveType.Removable => "Removable", DriveType.Network => "Network", DriveType.CDRom => "CD/DVD", _ => "Local disk" };
    private static string FmtGb(long b) => $"{b / 1073741824.0:0} GB";
    public override string ToString() => Display; // what pickers and screen readers show
}

public static class Volumes
{
    /// <summary>Ready fixed and removable drives. Network and optical drives are left out of the pickers.</summary>
    public static List<Volume> List()
    {
        var list = new List<Volume>();
        foreach (var d in DriveInfo.GetDrives())
        {
            try
            {
                if (!d.IsReady || d.DriveType is not (DriveType.Fixed or DriveType.Removable)) continue;
                long cluster = 0;
                if (GetDiskFreeSpaceW(d.RootDirectory.FullName, out var spc, out var bps, out _, out _)) cluster = (long)spc * bps;
                var hl = GetVolumeInformationW(d.RootDirectory.FullName, null, 0, out _, out _, out var flags, null, 0) && (flags & 0x00400000) != 0;
                list.Add(new Volume(d.RootDirectory.FullName, d.VolumeLabel, d.DriveFormat, d.DriveType, d.TotalSize, d.TotalFreeSpace, cluster, hl));
            }
            catch { /* drive went away while listing */ }
        }
        return list;
    }

    /// <summary>True if the folder's drive is connected and the folder exists.</summary>
    public static bool Available(string path) =>
        !string.IsNullOrWhiteSpace(path) && Path.GetPathRoot(path) is { Length: > 0 } r && new DriveInfo(r) is { IsReady: true } && Directory.Exists(path);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetDiskFreeSpaceW(string root, out uint sectorsPerCluster, out uint bytesPerSector, out uint freeClusters, out uint totalClusters);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetVolumeInformationW(string root, char[]? name, int nameSize, out uint serial, out uint maxComponent, out uint flags, char[]? fs, int fsSize);
}
