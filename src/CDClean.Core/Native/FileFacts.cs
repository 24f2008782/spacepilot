using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace CDClean.Core.Native;

/// <summary>Stable file identity: volume serial + 128-bit file ID. Paths are only for display.</summary>
public readonly record struct FileKey(ulong Volume, ulong Hi, ulong Lo)
{
    public override string ToString() => $"{Volume:X}:{Hi:X16}{Lo:X16}";
}

/// <summary>What the file system says about one file or folder, read without following links or recalling cloud files.</summary>
public sealed record FileFacts(
    string Path,
    string FinalPath,
    bool Exists,
    FileKey Key,
    long Logical,
    long Allocated,
    uint Links,
    FileAttributes Attributes,
    uint ReparseTag,
    DateTime LastWriteUtc,
    DateTime CreationUtc)
{
    public bool IsDirectory => (Attributes & FileAttributes.Directory) != 0;
    public bool IsSystem => (Attributes & FileAttributes.System) != 0;
    public bool IsHardLinked => !IsDirectory && Links > 1;

    /// <summary>Junction, symlink or app-exec link. Cloud and dedup reparse points are not links.</summary>
    public bool IsLink => ReparseTag is 0xA000000C or 0xA0000003 or 0x8000001B;

    /// <summary>OneDrive and other cloud files whose data may not be on this disk.</summary>
    public bool IsCloudPlaceholder =>
        ((uint)Attributes & (0x00040000u | 0x00400000u)) != 0 || (ReparseTag & 0xFFFF0FFF) == 0x9000001A;

    public static FileFacts Missing(string path) =>
        new(path, path, false, default, 0, 0, 0, 0, 0, DateTime.MinValue, DateTime.MinValue);
}

public static class Files
{
    private const uint FILE_READ_ATTRIBUTES = 0x80, DELETE = 0x10000;
    private const uint SHARE_ALL = 7, OPEN_EXISTING = 3;
    private const uint BACKUP_SEMANTICS = 0x02000000, OPEN_REPARSE_POINT = 0x00200000;
    private const int ERROR_SHARING_VIOLATION = 32;

    /// <summary>Reads identity, sizes, link count and attributes. Returns Missing if the path does not exist.</summary>
    public static FileFacts Read(string path, bool resolveFinalPath = true)
    {
        using var h = Open(path, FILE_READ_ATTRIBUTES);
        if (h.IsInvalid)
        {
            var err = Marshal.GetLastWin32Error();
            if (err is 2 or 3) return FileFacts.Missing(path); // file / path not found
            throw new Win32Exception(err);
        }
        return ReadHandle(h, path, resolveFinalPath);
    }

    /// <summary>Reads facts and tests deletability with one handle when possible; falls back to a read-only handle when locked.</summary>
    public static (FileFacts Facts, bool Locked) ReadWithLock(string path, bool resolveFinalPath = true)
    {
        using var h = Open(path, FILE_READ_ATTRIBUTES | DELETE);
        if (!h.IsInvalid) return (ReadHandle(h, path, resolveFinalPath), false);
        var err = Marshal.GetLastWin32Error();
        return (Read(path, resolveFinalPath), err == ERROR_SHARING_VIOLATION);
    }

    private static FileFacts ReadHandle(SafeFileHandle h, string path, bool resolveFinalPath)
    {
        // One basic/standard read covers dates, attributes, size, links and the 64-bit fallback key.
        if (!GetFileInformationByHandle(h, out BY_HANDLE_FILE_INFORMATION bh))
            throw new Win32Exception();
        if (!GetFileInformationByHandleEx(h, 1 /*FileStandardInfo*/, out FILE_STANDARD_INFO std, (uint)Marshal.SizeOf<FILE_STANDARD_INFO>()))
            throw new Win32Exception();

        FileKey key;
        if (GetFileInformationByHandleEx(h, 18 /*FileIdInfo*/, out FILE_ID_INFO id, (uint)Marshal.SizeOf<FILE_ID_INFO>()))
            key = new FileKey(id.VolumeSerialNumber, id.IdHi, id.IdLo);
        else
            key = new FileKey(bh.VolumeSerialNumber, 0, ((ulong)bh.IndexHigh << 32) | bh.IndexLow); // FAT/exFAT: 64-bit index only

        uint reparseTag = 0;
        if (((FileAttributes)bh.FileAttributes & FileAttributes.ReparsePoint) != 0
            && GetFileInformationByHandleEx(h, 9 /*FileAttributeTagInfo*/, out FILE_ATTRIBUTE_TAG_INFO tag, (uint)Marshal.SizeOf<FILE_ATTRIBUTE_TAG_INFO>()))
            reparseTag = tag.ReparseTag;

        return new FileFacts(path, resolveFinalPath ? FinalPath(h) ?? path : path, true, key,
            std.EndOfFile, std.AllocationSize, std.NumberOfLinks,
            (FileAttributes)bh.FileAttributes, reparseTag,
            DateTime.FromFileTimeUtc(((long)bh.W2 << 32) | bh.W1),
            DateTime.FromFileTimeUtc(((long)bh.C2 << 32) | bh.C1));
    }

    /// <summary>True when another process holds the file open in a way that stops it being deleted.</summary>
    public static bool IsLocked(string path)
    {
        using var h = Open(path, DELETE);
        return h.IsInvalid && Marshal.GetLastWin32Error() == ERROR_SHARING_VIOLATION;
    }

    /// <summary>Adds the \\?\ prefix so paths longer than 260 characters work with raw Win32 calls.</summary>
    public static string Long(string path)
    {
        if (path.StartsWith(@"\\?\")) return path;
        var full = System.IO.Path.GetFullPath(path);
        if (full.Length < 248) return full;
        return full.StartsWith(@"\\") ? @"\\?\UNC\" + full[2..] : @"\\?\" + full;
    }

    private static SafeFileHandle Open(string path, uint access) =>
        CreateFileW(Long(path), access, SHARE_ALL, IntPtr.Zero, OPEN_EXISTING, BACKUP_SEMANTICS | OPEN_REPARSE_POINT, IntPtr.Zero);

    /// <summary>Path with parent junctions and 8.3 short names resolved, used for the protected-path check.</summary>
    private static string? FinalPath(SafeFileHandle h)
    {
        var sb = new StringBuilder(1024);
        var n = GetFinalPathNameByHandleW(h, sb, (uint)sb.Capacity, 0);
        if (n == 0 || n >= sb.Capacity) return null;
        var p = sb.ToString();
        return p.StartsWith(@"\\?\UNC\") ? @"\\" + p[8..] : p.StartsWith(@"\\?\") ? p[4..] : p;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FILE_STANDARD_INFO { public long AllocationSize, EndOfFile; public uint NumberOfLinks; public byte DeletePending, Directory; }

    [StructLayout(LayoutKind.Sequential)]
    private struct FILE_ATTRIBUTE_TAG_INFO { public uint FileAttributes, ReparseTag; }

    [StructLayout(LayoutKind.Sequential)]
    private struct FILE_ID_INFO { public ulong VolumeSerialNumber, IdLo, IdHi; }

    [StructLayout(LayoutKind.Sequential)]
    private struct BY_HANDLE_FILE_INFORMATION
    {
        public uint FileAttributes;
        public uint C1, C2, A1, A2, W1, W2; // three FILETIMEs (4-byte aligned, so not longs)
        public uint VolumeSerialNumber, FileSizeHigh, FileSizeLow, NumberOfLinks, IndexHigh, IndexLow;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(string name, uint access, uint share, IntPtr sa, uint disposition, uint flags, IntPtr template);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandleEx(SafeFileHandle h, int cls, out FILE_STANDARD_INFO info, uint size);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandleEx(SafeFileHandle h, int cls, out FILE_ATTRIBUTE_TAG_INFO info, uint size);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandleEx(SafeFileHandle h, int cls, out FILE_ID_INFO info, uint size);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandle(SafeFileHandle h, out BY_HANDLE_FILE_INFORMATION info);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandleW(SafeFileHandle h, StringBuilder buf, uint size, uint flags);
}
