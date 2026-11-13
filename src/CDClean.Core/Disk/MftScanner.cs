using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace CDClean.Core.Disk;

/// <summary>
/// One entry recovered from the NTFS Master File Table. Sizes are the allocated size of the default $DATA
/// stream; directories have no data size of their own. Parent is the parent directory's MFT record number.
/// </summary>
public sealed record MftEntry(ulong Ref, ulong Parent, string Name, long Size, long AllocatedSize,
    bool IsDirectory, DateTime Modified, uint Attributes);

/// <summary>
/// Direct NTFS $MFT reader. Reads MFT records sequentially instead of opening each file individually.
/// Requires administrator rights to open the volume.
/// The parser (`ParseRecord`) is pure and unit-testable on synthetic records; `ReadVolume` needs elevation.
/// </summary>
public static class MftScanner
{
    private const uint FSCTL_GET_NTFS_VOLUME_DATA = 0x00090064;
    private const int FILE_SHARE_READ = 1, FILE_SHARE_WRITE = 2;
    private const uint GENERIC_READ = 0x80000000;
    private const uint OPEN_EXISTING = 3;
    private const uint FILE_FLAG_SEQUENTIAL_SCAN = 0x08000000;

    // Attribute types.
    private const uint AttrStandardInformation = 0x10;
    private const uint AttrFileName = 0x30;
    private const uint AttrData = 0x80;
    private const uint AttrEnd = 0xFFFFFFFF;

    private const int MftRecordHeaderSize = 48; // "FILE" + usn + lsn + seq + links + attrOffset + flags + ...

    /// <summary>Writes MFT entries to a stream in a compact binary format (much faster than JSON for millions of records).</summary>
    public static void WriteEntries(Stream stream, List<MftEntry> entries)
    {
        using var w = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        w.Write(entries.Count);
        foreach (var e in entries)
        {
            w.Write(e.Ref);
            w.Write(e.Parent);
            var nameBytes = Encoding.UTF8.GetBytes(e.Name);
            w.Write(nameBytes.Length);
            w.Write(nameBytes);
            w.Write(e.Size);
            w.Write(e.AllocatedSize);
            w.Write(e.IsDirectory);
            w.Write(e.Modified.ToBinary());
            w.Write(e.Attributes);
        }
    }

    /// <summary>Reads MFT entries from a compact binary stream produced by WriteEntries.</summary>
    public static List<MftEntry> ReadEntries(Stream stream)
    {
        using var r = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        var count = r.ReadInt32();
        var result = new List<MftEntry>(count);
        for (var i = 0; i < count; i++)
        {
            var ref_ = r.ReadUInt64();
            var parent = r.ReadUInt64();
            var nameLen = r.ReadInt32();
            var name = Encoding.UTF8.GetString(r.ReadBytes(nameLen));
            var size = r.ReadInt64();
            var alloc = r.ReadInt64();
            var isDir = r.ReadBoolean();
            var modified = DateTime.FromBinary(r.ReadInt64());
            var attrs = r.ReadUInt32();
            result.Add(new MftEntry(ref_, parent, name, size, alloc, isDir, modified, attrs));
        }
        return result;
    }

    /// <summary>True when the current process is elevated (admin). The fast scan needs this.</summary>
    public static bool IsElevated()
    {
        try
        {
            using var id = System.Security.Principal.WindowsIdentity.GetCurrent();
            var p = new System.Security.Principal.WindowsPrincipal(id);
            return p.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch { return false; }
    }

    /// <summary>
    /// Parses one MFT record (already fixup-applied) into an entry. Returns null for deleted records,
    /// metafiles ($MFT, $LogFile, ...) and records without a usable $FILE_NAME.
    /// </summary>
    public static MftEntry? ParseRecord(byte[] record, int recordSize)
    {
        if (record is null || record.Length < recordSize || recordSize < 512) return null;
        if (record[0] != (byte)'F' || record[1] != (byte)'I' || record[2] != (byte)'L' || record[3] != (byte)'E') return null;

        // Fixups are assumed already applied by the caller (ApplyFixups). Here we just read.
        var flags = BitConverter.ToUInt16(record, 0x16);
        if ((flags & 0x01) == 0) return null; // not in use

        var attrOffset = BitConverter.ToUInt16(record, 0x14);
        if (attrOffset < 0x30 || attrOffset >= recordSize) return null;

        ulong parent = 0;
        string? name = null;
        var namePriority = -1;
        long size = 0, allocated = 0, fileNameSize = 0, fileNameAllocated = 0;
        bool hasUnnamedData = false;
        DateTime modified = DateTime.MinValue;
        uint fileAttrs = 0;
        bool isDir = (flags & 0x02) != 0;

        var off = (int)attrOffset;
        while (off + 8 <= recordSize)
        {
            var type = BitConverter.ToUInt32(record, off);
            if (type == AttrEnd) break;
            var length = BitConverter.ToUInt32(record, off + 4);
            if (length == 0 || off + length > recordSize) break;
            var nonResident = record[off + 8];
            var nameLen = record[off + 9];

            if (type == AttrStandardInformation && nonResident == 0)
            {
                var contentSize = BitConverter.ToUInt32(record, off + 0x10);
                var contentOff = BitConverter.ToUInt16(record, off + 0x14);
                if (contentSize >= 40 && off + contentOff + 40 <= recordSize)
                {
                    var base_ = off + contentOff;
                    // Modified time is the 2nd FILETIME (offset 8 within STANDARD_INFORMATION).
                    var ft = BitConverter.ToInt64(record, base_ + 8);
                    try { modified = DateTime.FromFileTimeUtc(ft); } catch { modified = DateTime.MinValue; }
                    fileAttrs = BitConverter.ToUInt32(record, base_ + 0x20);
                    if ((fileAttrs & 0x10) != 0) isDir = true;
                }
            }
            else if (type == AttrFileName && nonResident == 0)
            {
                var contentSize = BitConverter.ToUInt32(record, off + 0x10);
                var contentOff = BitConverter.ToUInt16(record, off + 0x14);
                if (contentOff >= 0x18 && contentOff <= length && contentSize >= 66 &&
                    contentSize <= length - contentOff && off + contentOff + 66 <= recordSize)
                {
                    var base_ = off + contentOff;
                    var attrFlags = BitConverter.ToUInt64(record, base_ + 0x38);
                    var nlen = record[base_ + 0x40];
                    var ns = record[base_ + 0x41];
                    var priority = ns switch { 3 => 3, 1 => 2, 0 => 1, 2 => 0, _ => -1 };
                    if (priority > namePriority && nlen * 2 <= contentSize - 0x42)
                    {
                        name = Encoding.Unicode.GetString(record, base_ + 0x42, (int)nlen * 2);
                        parent = BitConverter.ToUInt64(record, base_) & 0x0000FFFFFFFFFFFFUL;
                        fileNameAllocated = BitConverter.ToInt64(record, base_ + 0x28);
                        fileNameSize = BitConverter.ToInt64(record, base_ + 0x30);
                        if ((attrFlags & 0x10) != 0) isDir = true;
                        namePriority = priority;
                    }
                }
            }
            else if (type == AttrData && nameLen == 0 && nonResident == 0)
            {
                var contentSize = BitConverter.ToUInt32(record, off + 0x10);
                var contentOff = BitConverter.ToUInt16(record, off + 0x14);
                if (contentOff >= 0x18 && contentOff <= length && contentSize <= length - contentOff)
                {
                    size = contentSize;
                    allocated = contentSize;
                    hasUnnamedData = true;
                }
            }
            else if (type == AttrData && nameLen == 0 && nonResident == 1)
            {
                // Non-resident $DATA: allocated size (8) at off+0x28, data size (8) at off+0x30.
                if (length >= 0x38)
                {
                    allocated = BitConverter.ToInt64(record, off + 0x28);
                    size = BitConverter.ToInt64(record, off + 0x30);
                    hasUnnamedData = true;
                }
            }

            off += (int)length;
        }

        if (!hasUnnamedData)
        {
            size = fileNameSize;
            allocated = fileNameAllocated;
        }
        if (name is null || name.Length == 0) return null;
        // A missing parent is attached to the root directory (MFT record 5).
        if (parent == 0 && name.Length > 0) parent = 5; // root directory FRN

        return new MftEntry(0, parent, name, size, allocated, isDir, modified, fileAttrs);
    }

    /// <summary>Applies the NTFS update-sequence (fixup) array to a record in place. Must be called before parsing.</summary>
    public static void ApplyFixups(byte[] record, int recordSize, int bytesPerSector)
    {
        if (record is null || record.Length < recordSize || bytesPerSector <= 0) return;
        var usnOffset = BitConverter.ToUInt16(record, 0x04);
        var usnSize = BitConverter.ToUInt16(record, 0x06);
        if (usnOffset == 0 || usnSize < 2 || usnOffset + usnSize * 2 > recordSize) return;
        var usn = BitConverter.ToUInt16(record, usnOffset);
        var sectors = recordSize / bytesPerSector;
        if (sectors > usnSize - 1) sectors = usnSize - 1;
        for (var i = 0; i < sectors; i++)
        {
            var sectorEnd = (i + 1) * bytesPerSector - 2;
            if (sectorEnd + 2 > recordSize) break;
            var saved = BitConverter.ToUInt16(record, usnOffset + 2 + i * 2);
            // Only fix if the sector's last word matches the USN.
            if (BitConverter.ToUInt16(record, sectorEnd) == usn)
            {
                record[sectorEnd] = (byte)(saved & 0xFF);
                record[sectorEnd + 1] = (byte)(saved >> 8);
            }
        }
    }

    /// <summary>
    /// Reads one volume's MFT and returns all entries. Requires administrator rights; throws
    /// UnauthorizedAccessException otherwise. Not covered by automated tests (needs elevation).
    /// </summary>
    public static List<MftEntry> ReadVolume(char driveLetter, CancellationToken cancellationToken = default)
    {
        if (!char.IsAsciiLetter(driveLetter)) throw new ArgumentException("A drive letter is required.", nameof(driveLetter));
        driveLetter = char.ToUpperInvariant(driveLetter);
        var entries = new List<MftEntry>();
        var volPath = $@"\\.\{driveLetter}:";
        using var h = CreateFileW(volPath, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING,
            FILE_FLAG_SEQUENTIAL_SCAN, IntPtr.Zero);
        if (h.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());

        // FSCTL_GET_NTFS_VOLUME_DATA supplies the authoritative MFT geometry and length.
        var volData = new byte[128];
        var inBuf = new byte[0];
        if (!DeviceIoControl(h, FSCTL_GET_NTFS_VOLUME_DATA, inBuf, 0, volData, (uint)volData.Length, out var returned, IntPtr.Zero))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not query NTFS volume data; refusing to scan a guessed MFT range.");
        // NTFS_VOLUME_DATA_BUFFER layout: BytesPerSector@40, BytesPerCluster@44,
        // BytesPerFileRecordSegment@48, ClustersPerFileRecordSegment@52,
        // MftValidDataLength@56, MftStartLcn@64.
        if (returned < 72) throw new InvalidDataException("NTFS returned an incomplete volume-data structure.");
        var bytesPerSectorValue = BitConverter.ToUInt32(volData, 40);
        var bytesPerClusterValue = BitConverter.ToUInt32(volData, 44);
        var recordSizeValue = BitConverter.ToUInt32(volData, 48);
        var mftValidDataLength = BitConverter.ToInt64(volData, 56);
        var mftStartLcn = BitConverter.ToInt64(volData, 64);
        if (bytesPerSectorValue == 0 || bytesPerSectorValue > ushort.MaxValue ||
            bytesPerClusterValue == 0 || bytesPerClusterValue > int.MaxValue ||
            recordSizeValue < 512 || recordSizeValue > int.MaxValue || mftValidDataLength <= 0 || mftStartLcn < 0)
            throw new InvalidDataException("NTFS returned invalid MFT length or record geometry.");
        var bytesPerSector = (int)bytesPerSectorValue;
        var bytesPerCluster = (int)bytesPerClusterValue;
        var recordSize = (int)recordSizeValue;
        var mftOffset = checked(mftStartLcn * bytesPerCluster);

        var requiredClusters = checked((mftValidDataLength + bytesPerCluster - 1) / bytesPerCluster);
        var record = new byte[recordSize];
        var firstRecordPos = new LARGE_INTEGER { QuadPart = mftOffset };
        if (!SetFilePointerEx(h, firstRecordPos, out _, 0)) throw new Win32Exception(Marshal.GetLastWin32Error());
        if (!ReadFile(h, record, recordSize, out var firstRecordBytes, IntPtr.Zero))
            throw new Win32Exception(Marshal.GetLastWin32Error());
        if (firstRecordBytes != recordSize) throw new IOException("Could not read the first MFT record to verify its runlist.");
        ApplyFixups(record, recordSize, bytesPerSector);
        var extents = GetMftExtents(record, recordSize, requiredClusters);
        if (!IsCompleteExtentMap(extents, requiredClusters) || extents[0].StartLcn != mftStartLcn)
            throw new InvalidDataException("The MFT runlist is incomplete or inconsistent with the volume geometry.");

        // Read multiple records per kernel call; one call per 1 KB record dominated full-volume scan time.
        const int ReadChunkBytes = 4 * 1024 * 1024;
        var chunk = new byte[Math.Max(recordSize, ReadChunkBytes - ReadChunkBytes % recordSize)];
        var recordsRemaining = mftValidDataLength / recordSize;
        if (recordsRemaining <= 0) return entries;

        ulong recordIndex = 0;
        var recordBytes = 0;
        void ConsumeRecords(byte[] buffer, int count)
        {
            var offset = 0;
            while (offset < count)
            {
                var copy = Math.Min(recordSize - recordBytes, count - offset);
                Buffer.BlockCopy(buffer, offset, record, recordBytes, copy);
                offset += copy;
                recordBytes += copy;
                if (recordBytes == recordSize)
                {
                    ApplyFixups(record, recordSize, bytesPerSector);
                    var entry = ParseRecord(record, recordSize);
                    // NTFS reserves file-record numbers 0–15 for metadata; $Extend's children are metadata too.
                    if (entry is not null && recordIndex > 15 && entry.Parent != 11)
                        entries.Add(entry with { Ref = recordIndex });
                    recordIndex++;
                    recordBytes = 0;
                }
            }
        }

        var bytesRemaining = mftValidDataLength;
        foreach (var extent in extents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (bytesRemaining == 0) break;
            var extentBytes = checked((extent.EndVcn - extent.StartVcn) * bytesPerCluster);
            var toReadInExtent = Math.Min(extentBytes, bytesRemaining);
            var extentPos = new LARGE_INTEGER { QuadPart = checked(extent.StartLcn * bytesPerCluster) };
            if (!SetFilePointerEx(h, extentPos, out _, 0)) throw new Win32Exception(Marshal.GetLastWin32Error());
            while (toReadInExtent > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var bytesToRead = (int)Math.Min(toReadInExtent, chunk.Length);
                if (!ReadFile(h, chunk, bytesToRead, out var bytesRead, IntPtr.Zero))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                if (bytesRead != bytesToRead)
                    throw new IOException($"MFT extent read ended early: received {bytesRead:N0} of {bytesToRead:N0} expected bytes.");
                ConsumeRecords(chunk, bytesRead);
                toReadInExtent -= bytesRead;
                bytesRemaining -= bytesRead;
            }
        }
        if (bytesRemaining != 0 || recordBytes != 0 || recordIndex != (ulong)(mftValidDataLength / recordSize))
            throw new IOException("The MFT extent map did not provide a complete set of file records.");
        return entries;
    }

    internal static bool IsCompleteExtentMap(
        IReadOnlyList<(long StartVcn, long EndVcn, long StartLcn)> extents, long requiredClusters)
    {
        if (extents.Count == 0 || requiredClusters <= 0 || extents[0].StartVcn != 0) return false;
        var nextVcn = 0L;
        foreach (var extent in extents)
        {
            if (extent.StartVcn != nextVcn || extent.StartLcn < 0 || extent.EndVcn <= extent.StartVcn) return false;
            nextVcn = extent.EndVcn;
            if (nextVcn >= requiredClusters) return true;
        }
        return false;
    }

    internal static List<(long StartVcn, long EndVcn, long StartLcn)> ParseRunlist(byte[] mappingPairs, long startVcn = 0)
    {
        if (mappingPairs is null || startVcn < 0) throw new InvalidDataException("Invalid MFT runlist.");
        var extents = new List<(long StartVcn, long EndVcn, long StartLcn)>();
        var pos = 0;
        var currentVcn = startVcn;
        long currentLcn = 0;
        while (pos < mappingPairs.Length)
        {
            var header = mappingPairs[pos++];
            if (header == 0) return extents;
            var lengthBytes = header & 0x0F;
            var offsetBytes = header >> 4;
            if (lengthBytes is 0 or > 8 || offsetBytes is 0 or > 8 || pos + lengthBytes + offsetBytes > mappingPairs.Length)
                throw new InvalidDataException("The MFT contains an invalid or sparse runlist entry.");

            ulong runLength = 0;
            for (var i = 0; i < lengthBytes; i++) runLength |= (ulong)mappingPairs[pos++] << (8 * i);
            if (runLength == 0 || runLength > long.MaxValue) throw new InvalidDataException("The MFT contains an invalid run length.");

            ulong rawDelta = 0;
            for (var i = 0; i < offsetBytes; i++) rawDelta |= (ulong)mappingPairs[pos++] << (8 * i);
            if (offsetBytes < 8 && (mappingPairs[pos - 1] & 0x80) != 0)
                rawDelta |= ulong.MaxValue << (8 * offsetBytes);
            currentLcn = checked(currentLcn + unchecked((long)rawDelta));
            if (currentLcn < 0) throw new InvalidDataException("The MFT contains an invalid physical extent.");
            var endVcn = checked(currentVcn + (long)runLength);
            extents.Add((currentVcn, endVcn, currentLcn));
            currentVcn = endVcn;
        }
        throw new InvalidDataException("The MFT runlist has no terminator.");
    }

    private static List<(long StartVcn, long EndVcn, long StartLcn)> GetMftExtents(byte[] record, int recordSize, long requiredClusters)
    {
        if (requiredClusters <= 0 || recordSize > record.Length || record[0] != 'F' || record[1] != 'I' ||
            record[2] != 'L' || record[3] != 'E')
            throw new InvalidDataException("The first MFT record is invalid.");
        var off = (int)BitConverter.ToUInt16(record, 0x14);
        if (off < MftRecordHeaderSize || off >= recordSize)
            throw new InvalidDataException("The first MFT record has an invalid attribute offset.");
        while (off + 8 <= recordSize)
        {
            var type = BitConverter.ToUInt32(record, off);
            if (type == AttrEnd) break;
            var length = BitConverter.ToUInt32(record, off + 4);
            if (length < 8 || off + length > recordSize) break;
            if (type == AttrData && length >= 0x40 && record[off + 8] == 1 && record[off + 9] == 0)
            {
                if (length < 0x40) throw new InvalidDataException("The MFT data attribute is incomplete.");
                var firstVcn = BitConverter.ToInt64(record, off + 0x10);
                var lastVcn = BitConverter.ToInt64(record, off + 0x18);
                var runlistOffset = BitConverter.ToUInt16(record, off + 0x20);
                if (firstVcn != 0 || lastVcn < requiredClusters - 1 || runlistOffset < 0x40 || runlistOffset >= length)
                    throw new InvalidDataException("The MFT runlist is incomplete or unsupported.");
                var runs = ParseRunlist(record.AsSpan(off + runlistOffset, (int)length - runlistOffset).ToArray());
                if (runs.Count == 0 || runs[^1].EndVcn < requiredClusters)
                    throw new InvalidDataException("The MFT runlist does not cover its valid data length.");
                return runs;
            }
            off += (int)length;
        }
        throw new InvalidDataException("The first MFT record does not contain a complete unnamed data runlist.");
    }

    /// <summary>Builds a DiskTree from MFT entries, linking children via parent FRN.</summary>
    public static DiskTree ToDiskTree(IEnumerable<MftEntry> entries, string rootPath, CancellationToken ct = default)
    {
        var tree = new DiskTree(rootPath);
        // Map parent FRN -> list of child entries. Root is record 5.
        var byParent = new Dictionary<ulong, List<MftEntry>>();
        foreach (var e in entries)
        {
            ct.ThrowIfCancellationRequested();
            // Skip the root directory itself (record 5) and self-referential entries: they create infinite loops.
            if (e.Ref == 5) continue;
            if (e.Parent == e.Ref) continue;
            if (string.IsNullOrEmpty(e.Name) || e.Name == ".") continue;
            // Orphaned entries (parent 0) attach to the root.
            var parent = e.Parent == 0 ? 5UL : e.Parent;
            if (!byParent.TryGetValue(parent, out var list)) byParent[parent] = list = new List<MftEntry>();
            list.Add(e with { Parent = parent });
        }

        // Iterative DFS: (parent node index, parent FRN, depth). Root FRN 5.
        var work = new Stack<(int ParentNode, ulong Frn)>();
        var visitedDirectories = new HashSet<ulong> { 5 };
        work.Push((0, 5UL));
        while (work.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var (parentNode, frn) = work.Pop();
            if (!byParent.TryGetValue(frn, out var children)) continue;
            foreach (var c in children)
            {
                ct.ThrowIfCancellationRequested();
                if (c.IsDirectory)
                {
                    if (!visitedDirectories.Add(c.Ref)) continue;
                    var idx = tree.Add(parentNode, c.Name, NodeKind.Dir, 0, 0, c.Modified);
                    if (idx >= 0) work.Push((idx, c.Ref));
                }
                else
                {
                    tree.Add(parentNode, c.Name, NodeKind.File, c.Size, c.AllocatedSize == 0 ? c.Size : c.AllocatedSize, c.Modified);
                }
            }
        }
        tree.Rollup();
        return tree;
    }

    // ---- P/Invoke ----

    [StructLayout(LayoutKind.Sequential)]
    private struct LARGE_INTEGER { public long QuadPart; }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFileW(string lpFileName, uint dwDesiredAccess, uint dwShareMode,
        IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadFile(SafeFileHandle hFile, [Out] byte[] lpBuffer, int nNumberOfBytesToRead,
        out int lpNumberOfBytesRead, IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetFilePointerEx(SafeFileHandle hFile, LARGE_INTEGER liDistanceToMove,
        out LARGE_INTEGER lpNewFilePointer, uint dwMoveMethod);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(SafeFileHandle hDevice, uint dwIoControlCode,
        [In] byte[] lpInBuffer, uint nInBufferSize, [Out] byte[] lpOutBuffer, uint nOutBufferSize,
        out uint lpBytesReturned, IntPtr lpOverlapped);
}
