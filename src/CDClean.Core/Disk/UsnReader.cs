using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace CDClean.Core.Disk;

/// <summary>One name record from the NTFS change journal enumeration: a file or folder, its parent, and its attributes.</summary>
public readonly record struct UsnEntry(ulong Ref, ulong Parent, string Name, uint Attributes)
{
    public bool IsDirectory => (Attributes & 0x10) != 0;
}

/// <summary>
/// Reads every name on an NTFS volume in one pass with FSCTL_ENUM_USN_DATA (report §7.3.1, option 1).
/// This gives the folder structure quickly. It does not give file sizes; those come from per-file facts.
/// Needs administrator rights to open the volume. The parser and tree builder are testable without them.
/// </summary>
public static class UsnReader
{
    private const uint FSCTL_ENUM_USN_DATA = 0x900B3;   // CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 44, METHOD_NEITHER, FILE_ANY_ACCESS)
    private const int ERROR_HANDLE_EOF = 38;
    private const int ERROR_ACCESS_DENIED = 5;

    /// <summary>
    /// Decodes one output buffer from FSCTL_ENUM_USN_DATA. The first 8 bytes are the reference to continue from;
    /// the records follow (version 2 records only; version 3 records use 128-bit references and are skipped).
    /// </summary>
    /// <returns>The reference to continue from.</returns>
    public static ulong ParseBuffer(ReadOnlySpan<byte> buf, int dataLength, List<UsnEntry> into)
    {
        if (dataLength < 8) return 0;
        var next = BitConverter.ToUInt64(buf[..8]);
        var off = 8;
        while (off + 60 <= dataLength)
        {
            var recLen = BitConverter.ToUInt32(buf.Slice(off, 4));
            if (recLen == 0 || off + recLen > dataLength) break;
            var major = BitConverter.ToUInt16(buf.Slice(off + 4, 2));
            if (major == 2)
            {
                var frn = BitConverter.ToUInt64(buf.Slice(off + 8, 8));
                var parent = BitConverter.ToUInt64(buf.Slice(off + 16, 8));
                var attr = BitConverter.ToUInt32(buf.Slice(off + 52, 4));
                var nameLen = BitConverter.ToUInt16(buf.Slice(off + 56, 2));
                var nameOff = BitConverter.ToUInt16(buf.Slice(off + 58, 2));
                if (nameOff + nameLen <= recLen)
                {
                    var name = Encoding.Unicode.GetString(buf.Slice(off + nameOff, nameLen));
                    into.Add(new UsnEntry(frn, parent, name, attr));
                }
            }
            off += (int)recLen;
        }
        return next;
    }

    /// <summary>Reads every name on a volume such as "C". Throws Win32Exception if the volume cannot be opened (usually: not elevated).</summary>
    public static List<UsnEntry> EnumerateVolume(char letter)
    {
        var entries = new List<UsnEntry>();
        using var h = CreateFileW($@"\\.\{char.ToUpperInvariant(letter)}:", 0x80000000 /*GENERIC_READ*/,
            3 /*share read|write*/, IntPtr.Zero, 3 /*OPEN_EXISTING*/, 0, IntPtr.Zero);
        if (h.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot open the volume. Run as administrator to scan with the fast method.");

        var input = new byte[24];           // MFT_ENUM_DATA_V0: StartFileReferenceNumber, LowUsn, HighUsn
        var output = new byte[1 << 20];
        long highUsn = long.MaxValue;
        BitConverter.GetBytes(highUsn).CopyTo(input, 16);
        while (true)
        {
            if (!DeviceIoControl(h, FSCTL_ENUM_USN_DATA, input, input.Length, output, output.Length, out var returned, IntPtr.Zero))
            {
                var err = Marshal.GetLastWin32Error();
                if (err == ERROR_HANDLE_EOF) break;
                throw new Win32Exception(err);
            }
            if (returned <= 8) break;
            var next = ParseBuffer(output, (int)returned, entries);
            if (next == 0) break;
            BitConverter.GetBytes(next).CopyTo(input, 0);
        }
        return entries;
    }

    /// <summary>
    /// Builds a DiskTree for one folder from the volume's names. Only entries under rootRef are kept.
    /// Sizes are not known from names alone: every file is recorded with size 0 and must be filled in by the caller.
    /// </summary>
    public static DiskTree BuildTree(string rootPath, ulong rootRef, IEnumerable<UsnEntry> all)
    {
        var byRef = all.GroupBy(e => e.Ref).ToDictionary(g => g.Key, g => g.Last());
        var tree = new DiskTree(rootPath);
        var nodeOf = new Dictionary<ulong, int> { [rootRef] = 0 };

        // Keep only entries whose parent chain reaches rootRef. Parents are placed before children by walking up first.
        int Place(ulong r, int depth)
        {
            if (nodeOf.TryGetValue(r, out var n)) return n;
            if (depth > 512 || !byRef.TryGetValue(r, out var e)) return -1;
            var p = Place(e.Parent, depth + 1);
            if (p < 0) return -1;
            var kind = e.IsDirectory ? NodeKind.Dir : NodeKind.File;
            n = tree.Add(p, e.Name, kind, 0, 0, DateTime.MinValue);
            nodeOf[r] = n;
            return n;
        }
        foreach (var r in byRef.Keys) Place(r, 0);
        return tree;
    }

    /// <summary>Full path of an entry relative to rootRef, or null if it is outside that folder.</summary>
    public static string? RelativePath(ulong rootRef, ulong r, IReadOnlyDictionary<ulong, UsnEntry> byRef)
    {
        var parts = new List<string>();
        for (var depth = 0; depth < 512; depth++)
        {
            if (r == rootRef) { parts.Reverse(); return string.Join('\\', parts); }
            if (!byRef.TryGetValue(r, out var e)) return null;
            parts.Add(e.Name);
            r = e.Parent;
        }
        return null;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(string name, uint access, uint share, IntPtr sa, uint disposition, uint flags, IntPtr template);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(SafeFileHandle h, uint code, byte[] input, int inSize, byte[] output, int outSize, out uint returned, IntPtr overlapped);
}
