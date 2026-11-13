using System.Text;
using CDClean.Core.Disk;
using Xunit;

public class MftTests
{
    private static byte[] BuildRecord(string name, ulong parent, long size, bool isDir, long? cachedFileNameSize = null)
    {
        int recordSize = 1024;
        var rec = new byte[recordSize];
        // "FILE"
        rec[0] = (byte)'F'; rec[1] = (byte)'I'; rec[2] = (byte)'L'; rec[3] = (byte)'E';
        // usnOffset/usnSize = 0 (no fixups)
        BitConverter.GetBytes((ushort)0x38).CopyTo(rec, 0x14); // first attr offset
        ushort flags = 0x01; // in use
        if (isDir) flags |= 0x02;
        BitConverter.GetBytes(flags).CopyTo(rec, 0x16);
        BitConverter.GetBytes((uint)recordSize).CopyTo(rec, 0x18); // used
        BitConverter.GetBytes((uint)recordSize).CopyTo(rec, 0x1C); // allocated

        int off = 0x38;
        // $STANDARD_INFORMATION
        {
            int attrStart = off;
            BitConverter.GetBytes(0x10u).CopyTo(rec, attrStart);       // type
            int contentSize = 0x48;
            int contentOff = 0x18;
            int attrLen = contentOff + contentSize;
            // align to 8
            while (attrLen % 8 != 0) attrLen++;
            BitConverter.GetBytes((uint)attrLen).CopyTo(rec, attrStart + 4);
            rec[attrStart + 8] = 0; // resident
            rec[attrStart + 9] = 0; // nameLen
            BitConverter.GetBytes((ushort)0).CopyTo(rec, attrStart + 10); // nameOffset
            BitConverter.GetBytes((ushort)0).CopyTo(rec, attrStart + 12); // flags
            BitConverter.GetBytes((ushort)1).CopyTo(rec, attrStart + 14); // id
            BitConverter.GetBytes((uint)contentSize).CopyTo(rec, attrStart + 0x10);
            BitConverter.GetBytes((ushort)contentOff).CopyTo(rec, attrStart + 0x14);
            var base_ = attrStart + contentOff;
            // modified time at base+8
            BitConverter.GetBytes(DateTime.UtcNow.ToFileTimeUtc()).CopyTo(rec, base_ + 8);
            uint fa = isDir ? 0x10u : 0x20u;
            BitConverter.GetBytes(fa).CopyTo(rec, base_ + 0x20);
            off = attrStart + attrLen;
        }
        // $FILE_NAME
        {
            int attrStart = off;
            BitConverter.GetBytes(0x30u).CopyTo(rec, attrStart);
            int nlen = name.Length;
            int contentSize = 0x42 + nlen * 2;
            int contentOff = 0x18;
            int attrLen = contentOff + contentSize;
            while (attrLen % 8 != 0) attrLen++;
            BitConverter.GetBytes((uint)attrLen).CopyTo(rec, attrStart + 4);
            rec[attrStart + 8] = 0;
            rec[attrStart + 9] = 0;
            BitConverter.GetBytes((ushort)0).CopyTo(rec, attrStart + 10);
            BitConverter.GetBytes((ushort)0).CopyTo(rec, attrStart + 12);
            BitConverter.GetBytes((ushort)2).CopyTo(rec, attrStart + 14);
            BitConverter.GetBytes((uint)contentSize).CopyTo(rec, attrStart + 0x10);
            BitConverter.GetBytes((ushort)contentOff).CopyTo(rec, attrStart + 0x14);
            var base_ = attrStart + contentOff;
            BitConverter.GetBytes(parent).CopyTo(rec, base_); // parent (low 6 bytes; fine for test)
            // 4 times (8 each) at base+8..+39
            long ft = DateTime.UtcNow.ToFileTimeUtc();
            for (int t = 0; t < 4; t++) BitConverter.GetBytes(ft).CopyTo(rec, base_ + 8 + t * 8);
            var cachedSize = cachedFileNameSize ?? size;
            BitConverter.GetBytes(cachedSize).CopyTo(rec, base_ + 0x28); // allocated
            BitConverter.GetBytes(cachedSize).CopyTo(rec, base_ + 0x30); // data size
            ulong attrFlags = isDir ? 0x10UL : 0x20UL;
            BitConverter.GetBytes(attrFlags).CopyTo(rec, base_ + 0x38);
            // reparse tag
            BitConverter.GetBytes(0u).CopyTo(rec, base_ + 0x40 - 2); // placeholder
            rec[base_ + 0x40] = (byte)nlen; // name length (chars)
            rec[base_ + 0x41] = 3; // namespace: Win32&DOS
            Encoding.Unicode.GetBytes(name).CopyTo(rec, base_ + 0x42);
            off = attrStart + attrLen;
        }
        // $DATA (resident for files, empty for dirs)
        if (!isDir)
        {
            int attrStart = off;
            BitConverter.GetBytes(0x80u).CopyTo(rec, attrStart);
            if (size <= 100)
            {
                int contentSize = (int)size;
                int contentOff = 0x18;
                int attrLen = contentOff + Math.Max(contentSize, 1);
                while (attrLen % 8 != 0) attrLen++;
                BitConverter.GetBytes((uint)attrLen).CopyTo(rec, attrStart + 4);
                rec[attrStart + 8] = 0;
                rec[attrStart + 9] = 0;
                BitConverter.GetBytes((ushort)0).CopyTo(rec, attrStart + 10);
                BitConverter.GetBytes((ushort)0).CopyTo(rec, attrStart + 12);
                BitConverter.GetBytes((ushort)3).CopyTo(rec, attrStart + 14);
                BitConverter.GetBytes((uint)contentSize).CopyTo(rec, attrStart + 0x10);
                BitConverter.GetBytes((ushort)contentOff).CopyTo(rec, attrStart + 0x14);
                off = attrStart + attrLen;
            }
            else
            {
                const int attrLen = 0x48;
                BitConverter.GetBytes((uint)attrLen).CopyTo(rec, attrStart + 4);
                rec[attrStart + 8] = 1; // non-resident
                BitConverter.GetBytes((ushort)3).CopyTo(rec, attrStart + 14);
                BitConverter.GetBytes((ushort)0x40).CopyTo(rec, attrStart + 0x20); // runlist offset
                BitConverter.GetBytes(size).CopyTo(rec, attrStart + 0x28); // allocated
                BitConverter.GetBytes(size).CopyTo(rec, attrStart + 0x30); // data
                BitConverter.GetBytes(size).CopyTo(rec, attrStart + 0x38); // initialized
                rec[attrStart + 0x40] = 0x11; // one-cluster run
                rec[attrStart + 0x41] = 1;
                rec[attrStart + 0x42] = 1;
                rec[attrStart + 0x43] = 0; // runlist terminator
                off = attrStart + attrLen;
            }
        }
        // End marker
        BitConverter.GetBytes(0xFFFFFFFFu).CopyTo(rec, off);
        return rec;
    }

    private static void AppendFileName(byte[] rec, string name, ulong parent, byte nameSpace, long size)
    {
        var off = 0x38;
        while (BitConverter.ToUInt32(rec, off) != 0xFFFFFFFF)
            off += (int)BitConverter.ToUInt32(rec, off + 4);
        var attrLen = (0x18 + 0x42 + name.Length * 2 + 7) & ~7;
        BitConverter.GetBytes(0x30u).CopyTo(rec, off);
        BitConverter.GetBytes((uint)attrLen).CopyTo(rec, off + 4);
        BitConverter.GetBytes((uint)(0x42 + name.Length * 2)).CopyTo(rec, off + 0x10);
        BitConverter.GetBytes((ushort)0x18).CopyTo(rec, off + 0x14);
        BitConverter.GetBytes(parent).CopyTo(rec, off + 0x18);
        BitConverter.GetBytes(size).CopyTo(rec, off + 0x40);
        BitConverter.GetBytes(size).CopyTo(rec, off + 0x48);
        rec[off + 0x58] = (byte)name.Length;
        rec[off + 0x59] = nameSpace;
        Encoding.Unicode.GetBytes(name).CopyTo(rec, off + 0x5A);
        BitConverter.GetBytes(0xFFFFFFFFu).CopyTo(rec, off + attrLen);
    }

    [Fact]
    public void ParseRecord_returns_null_for_garbage()
    {
        var rec = new byte[1024];
        Assert.Null(MftScanner.ParseRecord(rec, 1024));
    }

    [Fact]
    public void ParseRecord_parses_a_file()
    {
        var rec = BuildRecord("hello.txt", 5, 1234, false);
        var e = MftScanner.ParseRecord(rec, 1024);
        Assert.NotNull(e);
        Assert.Equal("hello.txt", e!.Name);
        Assert.Equal(5UL, e.Parent);
        Assert.False(e.IsDirectory);
        Assert.Equal(1234, e.Size);
    }

    [Fact]
    public void ParseRecord_keeps_user_names_starting_with_dollar()
    {
        var entry = MftScanner.ParseRecord(BuildRecord("$Recycle.Bin", 5, 0, true), 1024);

        Assert.NotNull(entry);
        Assert.Equal("$Recycle.Bin", entry!.Name);
    }

    [Fact]
    public void ParseRecord_keeps_name_and_parent_from_the_same_file_name_attribute()
    {
        var record = BuildRecord("real.txt", 5, 100, false);
        AppendFileName(record, "REAL~1.TXT", 6, 2, 999);

        var entry = MftScanner.ParseRecord(record, 1024);

        Assert.NotNull(entry);
        Assert.Equal("real.txt", entry!.Name);
        Assert.Equal(5UL, entry.Parent);
        Assert.Equal(100, entry.Size);
    }

    [Fact]
    public void ParseRecord_prefers_unnamed_data_sizes_over_cached_file_name_sizes()
    {
        var rec = BuildRecord("large.bin", 5, 1234, false, cachedFileNameSize: 12);

        var entry = MftScanner.ParseRecord(rec, 1024);

        Assert.NotNull(entry);
        Assert.Equal(1234, entry!.Size);
        Assert.Equal(1234, entry.AllocatedSize);
    }

    [Fact]
    public void ParseRecord_parses_a_directory()
    {
        var rec = BuildRecord("MyFolder", 5, 0, true);
        var e = MftScanner.ParseRecord(rec, 1024);
        Assert.NotNull(e);
        Assert.Equal("MyFolder", e!.Name);
        Assert.True(e.IsDirectory);
    }

    [Fact]
    public void ToDiskTree_builds_a_tree_from_entries()
    {
        var entries = new[]
        {
            new MftEntry(6, 5, "sub", 0, 0, true, DateTime.MinValue, 0x10),
            new MftEntry(7, 6, "a.txt", 100, 100, false, DateTime.MinValue, 0x20),
        };
        var tree = MftScanner.ToDiskTree(entries, @"C:\", CancellationToken.None);
        Assert.Equal(3, tree.Nodes.Count); // root + sub + a.txt
        Assert.Equal(100, tree.TotalAllocated);
    }

    [Fact]
    public void ToDiskTree_does_not_expand_directory_cycles()
    {
        var entries = new[]
        {
            new MftEntry(6, 5, "a", 0, 0, true, DateTime.MinValue, 0x10),
            new MftEntry(7, 6, "b", 0, 0, true, DateTime.MinValue, 0x10),
            new MftEntry(6, 7, "a", 0, 0, true, DateTime.MinValue, 0x10),
        };

        var tree = MftScanner.ToDiskTree(entries, @"C:\");

        Assert.Equal(3, tree.Nodes.Count);
    }

    [Fact]
    public void IsCompleteExtentMap_accepts_fragmented_runs_and_rejects_logical_gaps()
    {
        Assert.True(MftScanner.IsCompleteExtentMap(new[] { (0L, 4L, 20L), (4L, 8L, 40L) }, 8));
        Assert.False(MftScanner.IsCompleteExtentMap(new[] { (0L, 4L, 20L), (5L, 8L, 40L) }, 8));
        Assert.False(MftScanner.IsCompleteExtentMap(new[] { (1L, 8L, 20L) }, 7));
        Assert.False(MftScanner.IsCompleteExtentMap(new[] { (0L, 4L, 20L) }, 8));
    }

    [Fact]
    public void ParseRunlist_reads_contiguous_and_fragmented_physical_runs()
    {
        var contiguous = MftScanner.ParseRunlist(new byte[] { 0x11, 8, 20, 0 });
        var fragmented = MftScanner.ParseRunlist(new byte[] { 0x11, 4, 20, 0x11, 4, 20, 0 });

        Assert.True(MftScanner.IsCompleteExtentMap(contiguous, 8));
        Assert.True(MftScanner.IsCompleteExtentMap(fragmented, 8));
    }

    [Fact]
    public void ParseRunlist_applies_signed_lcn_deltas()
    {
        var runs = MftScanner.ParseRunlist(new byte[] { 0x11, 4, 20, 0x11, 4, 0xFC, 0 });

        Assert.Equal(16, runs[1].StartLcn);
    }

    [Fact]
    public void ParseRunlist_rejects_sparse_and_unterminated_runs()
    {
        Assert.Throws<InvalidDataException>(() => MftScanner.ParseRunlist(new byte[] { 0x01, 4, 0 }));
        Assert.Throws<InvalidDataException>(() => MftScanner.ParseRunlist(new byte[] { 0x11, 4, 20 }));
    }

    [Fact]
    public void IsElevated_does_not_throw()
    {
        _ = MftScanner.IsElevated(); // must not throw
    }

    [Fact]
    public void ReadVolume_rejects_non_drive_letters_before_opening_a_volume()
    {
        Assert.Throws<ArgumentException>(() => MftScanner.ReadVolume(':'));
    }
}
