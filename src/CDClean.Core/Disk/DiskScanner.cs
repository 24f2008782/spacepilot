using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using CDClean.Core.Native;
using Microsoft.Win32.SafeHandles;

namespace CDClean.Core.Disk;

/// <summary>
/// Fallback scanner (report backend B/C): works on any drive and without admin.
/// Default (exact): folders are listed in parallel. On NTFS, one call per folder returns names, sizes and file IDs
/// (backend B); elsewhere every file is opened once. Either way the allocated size is the same number the rest of the
/// app uses, and a file reached by a second hard link counts once.
/// Fast (fastMetadata: true): no file is opened; sizes come from the directory listing and the compressed-size API.
/// Allocated sizes can differ slightly from exact (resident tiny files), and hard links are not deduplicated.
/// Rules in both: links are recorded and never followed; cloud placeholders take no space on disk; a folder that
/// cannot be read is kept as an unreadable node.
/// Memory: folders are streamed straight into the tree, one entry at a time, so the scan never holds every listing.
/// The tree stops at a 250-300 MB budget and marks itself truncated rather than exhausting a 4 GB PC.
/// </summary>
public static class DiskScanner
{
    private const int MaxWorkers = 8;

    public static DiskTree Scan(string root, CancellationToken ct = default, IProgress<long>? progress = null, bool fastMetadata = false)
    {
        var full = Path.GetFullPath(root);
        var exact = !fastMetadata;
        var tree = new DiskTree(full);
        // Backend B (report §7.3): on NTFS, list each folder once and read sizes and file IDs from the same call.
        // It needs no admin and avoids opening every file, which is what makes the walk slow on large folders.
        var state = new ScanState(ct, progress, exact, tree) { UseIds = exact && IsNtfs(full) };
        if (state.UseIds) state.Ids = new IdSet();
        state.Work.Add((full, 0));
        var workers = new Task[Math.Clamp(Environment.ProcessorCount, 2, MaxWorkers)];
        for (var i = 0; i < workers.Length; i++)
            workers[i] = Task.Run(() => Worker(state), ct);
        try { Task.WaitAll(workers); }
        catch (AggregateException ae) when (ae.InnerExceptions.All(x => x is OperationCanceledException)) { throw new OperationCanceledException(ct); }

        state.Ids = null;   // the file IDs are only needed while listing; free them before the rollup pass
        state.Seen = null;
        tree.Rollup();
        progress?.Report(state.Visited);
        return tree;
    }

    private static void Worker(ScanState s)
    {
        foreach (var (dir, node) in s.Work.GetConsumingEnumerable(s.Ct))
        {
            try { List(dir, node, s); }
            finally { if (Interlocked.Decrement(ref s.Pending) == 0) s.Work.CompleteAdding(); }
        }
    }

    private static bool IsNtfs(string path)
    {
        try
        {
            var root = Path.GetPathRoot(path);
            return root is { Length: >= 3 } && new DriveInfo(root).DriveFormat.Equals("NTFS", StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    /// <summary>Lists one folder. Uses the one-call NTFS listing when it works, and the walk otherwise.</summary>
    private static void List(string dir, int parentNode, ScanState s)
    {
        s.Ct.ThrowIfCancellationRequested();
        if (s.Tree.Truncated) return;
        if (Volatile.Read(ref s.UseIds))
        {
            try { ListById(dir, parentNode, s); return; }
            catch (Win32Exception e) when (e.NativeErrorCode is 1 or 6 or 50 or 87)
            {
                // This file system or this folder does not support the call: stop trying and use the walk.
                Volatile.Write(ref s.UseIds, false);
                s.Ids = null;
            }
            catch (Exception e) when (e is UnauthorizedAccessException or IOException or Win32Exception)
            {
                s.Tree.SetError(parentNode, e.Message);
                return;
            }
        }
        ListByWalk(dir, parentNode, s);
    }

    /// <summary>Lists one folder by opening every entry. The fallback for FAT, exFAT, network paths and odd folders.</summary>
    private static void ListByWalk(string dir, int parentNode, ScanState s)
    {
        try
        {
            foreach (var fsi in new DirectoryInfo(dir).EnumerateFileSystemInfos())
            {
                s.Ct.ThrowIfCancellationRequested();
                if (s.Tree.Truncated) return;
                var seen = Interlocked.Increment(ref s.Visited);
                if (s.Progress != null && seen % 4000 == 0) s.Progress.Report(seen);
                ApplyEntry(dir, parentNode, Describe(dir, fsi, s), s);
            }
        }
        catch (Exception e) when (e is UnauthorizedAccessException or IOException)
        {
            s.Tree.SetError(parentNode, e.Message);
        }
    }

    // ---- backend B: FileIdBothDirectoryInfo ----------------------------------

    private const uint FILE_LIST_DIRECTORY = 0x0001, FILE_READ_ATTRIBUTES = 0x0080;
    private const uint SHARE_ALL = 7, OPEN_EXISTING = 3;
    private const uint BACKUP_SEMANTICS = 0x02000000, OPEN_REPARSE_POINT = 0x00200000;
    private const int FILE_ID_BOTH_DIR_INFO = 10, FILE_ID_BOTH_DIR_RESTART_INFO = 11;
    private const int ERROR_NO_MORE_FILES = 18, ERROR_MORE_DATA = 234;
    private const int NAME_OFFSET = 104; // FileName in FILE_ID_BOTH_DIR_INFO on x64

    /// <summary>
    /// Lists one folder with GetFileInformationByHandleEx(FileIdBothDirectoryInfo): names, allocated and logical
    /// sizes, attributes, timestamps and the 64-bit file ID all arrive in one call. No file is opened, and the file ID
    /// is used to count a hard-linked file once. Reparse points are opened one by one so links are still classified.
    /// </summary>
    private static void ListById(string dir, int parentNode, ScanState s)
    {
        using var h = CreateFileW(Files.Long(dir), FILE_LIST_DIRECTORY | FILE_READ_ATTRIBUTES, SHARE_ALL,
            IntPtr.Zero, OPEN_EXISTING, BACKUP_SEMANTICS | OPEN_REPARSE_POINT, IntPtr.Zero);
        if (h.IsInvalid) throw new Win32Exception(Marshal.GetLastWin32Error());

        var buf = new byte[64 * 1024];
        var cls = FILE_ID_BOTH_DIR_RESTART_INFO;
        while (true)
        {
            s.Ct.ThrowIfCancellationRequested();
            if (s.Tree.Truncated) return;
            var ok = GetFileInformationByHandleEx(h, cls, buf, (uint)buf.Length);
            var err = ok ? 0 : Marshal.GetLastWin32Error();
            if (!ok && err == ERROR_NO_MORE_FILES) break;
            if (!ok && err != ERROR_MORE_DATA) throw new Win32Exception(err);
            cls = FILE_ID_BOTH_DIR_INFO;

            var off = 0;
            while (off + NAME_OFFSET <= buf.Length)
            {
                if (s.Tree.Truncated) return;
                var next = BitConverter.ToUInt32(buf, off);
                var nameLen = BitConverter.ToInt32(buf, off + 60);
                if (nameLen <= 0 || off + NAME_OFFSET + nameLen > buf.Length) break;
                var name = Encoding.Unicode.GetString(buf, off + NAME_OFFSET, nameLen);
                if (name is not ("." or ".."))
                {
                    var seen = Interlocked.Increment(ref s.Visited);
                    if (s.Progress != null && seen % 4000 == 0) s.Progress.Report(seen);
                    ApplyEntry(dir, parentNode, DescribeById(dir, name, buf, off, s), s);
                }
                if (next == 0) break;
                off += (int)next;
            }
        }
    }

    private static Entry DescribeById(string dir, string name, byte[] buf, int off, ScanState s)
    {
        var attrs = (FileAttributes)BitConverter.ToUInt32(buf, off + 56);
        var modified = DateTime.FromFileTimeUtc(BitConverter.ToInt64(buf, off + 24));
        if ((attrs & FileAttributes.ReparsePoint) != 0)
        {
            // Links, cloud and dedup entries are rare; one open each is cheap and keeps the exact classification.
            var path = Path.Combine(dir, name);
            try
            {
                var f = Files.Read(path);
                if (!f.Exists) return Entry.GoneEntry();
                if (f.IsLink) return new Entry(NodeKind.Link, name, 0, 0, modified);
                if (f.IsDirectory) return new Entry(NodeKind.Dir, name, 0, 0, modified);
                return FileEntryById(name, f.Logical, f.IsCloudPlaceholder ? 0 : f.Allocated, modified,
                    f.IsCloudPlaceholder, f.IsSystem, f.Key.Lo, s);
            }
            catch (Exception e) when (e is UnauthorizedAccessException or IOException or Win32Exception)
            {
                return Entry.Unreadable(name, e.Message);
            }
        }
        if ((attrs & FileAttributes.Directory) != 0) return new Entry(NodeKind.Dir, name, 0, 0, modified);

        var logical = BitConverter.ToInt64(buf, off + 40);
        var allocated = BitConverter.ToInt64(buf, off + 48);
        var cloud = (attrs & (FileAttributes.Offline | (FileAttributes)0x400000)) != 0;
        var system = (attrs & FileAttributes.System) != 0;
        var fileId = (ulong)BitConverter.ToInt64(buf, off + 96);
        return FileEntryById(name, logical, allocated, modified, cloud, system, fileId, s);
    }

    private static Entry Describe(string dir, FileSystemInfo fsi, ScanState s)
    {
        var name = fsi.Name;
        var path = Path.Combine(dir, name);
        FileAttributes attrs;
        DateTime modified;
        try { attrs = fsi.Attributes; modified = fsi.LastWriteTimeUtc; }
        catch (Exception e) when (e is UnauthorizedAccessException or IOException or Win32Exception)
        {
            return Entry.Unreadable(name, e.Message);
        }

        // Reparse points are opened once, so links are never followed and cloud or dedupe files are classified exactly.
        if ((attrs & FileAttributes.ReparsePoint) != 0)
        {
            try
            {
                var f = Files.Read(path);
                if (!f.Exists) return Entry.GoneEntry();
                if (f.IsLink) return new Entry(NodeKind.Link, name, 0, 0, modified);
                if (f.IsDirectory) return new Entry(NodeKind.Dir, name, 0, 0, modified);
                return FileEntry(name, f.Logical, f.IsCloudPlaceholder ? 0 : f.Allocated, modified,
                    f.IsCloudPlaceholder, f.IsSystem, f.Key, s, f.IsHardLinked);
            }
            catch (Exception e) when (e is UnauthorizedAccessException or IOException or Win32Exception)
            {
                return MetadataFallback(name, path, e.Message);
            }
        }
        if ((attrs & FileAttributes.Directory) != 0) return new Entry(NodeKind.Dir, name, 0, 0, modified);

        var logical = fsi is FileInfo fi ? fi.Length : 0;
        var cloud = (attrs & (FileAttributes.Offline | (FileAttributes)0x400000)) != 0; // Offline or RECALL_ON_DATA_ACCESS
        var system = (attrs & FileAttributes.System) != 0;
        if (cloud) return new Entry(NodeKind.File, name, logical, 0, modified, cloud: true, system: system);
        if (s.Exact)
        {
            try
            {
                var f = Files.Read(path);
                if (!f.Exists) return Entry.GoneEntry();
                return FileEntry(name, f.Logical, f.Allocated, modified, false, f.IsSystem, f.Key, s, f.IsHardLinked);
            }
            catch (Exception e) when (e is UnauthorizedAccessException or IOException or Win32Exception)
            {
                return MetadataFallback(name, path, e.Message);
            }
        }
        return new Entry(NodeKind.File, name, logical, AllocatedOf(path, logical), modified, system: system);
    }

    private static Entry FileEntry(string name, long logical, long allocated, DateTime modified,
        bool cloud, bool system, FileKey key, ScanState s, bool hardLinked = false)
    {
        // Exact mode only: the first name of a file counts its size, later names count 0.
        var repeat = false;
        if (s.Exact && hardLinked && key != default)
        {
            s.Seen ??= new ConcurrentDictionary<FileKey, byte>();
            repeat = !s.Seen.TryAdd(key, 0);
        }
        return new Entry(NodeKind.File, name, logical, repeat || cloud ? 0 : allocated, modified,
            cloud: cloud, repeat: repeat, system: system, saved: repeat ? allocated : 0);
    }

    private static Entry FileEntryById(string name, long logical, long allocated, DateTime modified,
        bool cloud, bool system, ulong id, ScanState s)
    {
        var repeat = false;
        if (id != 0 && s.Ids is { } ids) repeat = !ids.Add(id);
        return new Entry(NodeKind.File, name, logical, repeat || cloud ? 0 : allocated, modified,
            cloud: cloud, repeat: repeat, system: system, saved: repeat ? allocated : 0);
    }

    /// <summary>A file that cannot be opened (in use, e.g. pagefile.sys) is still counted from its listing metadata.</summary>
    private static Entry MetadataFallback(string name, string path, string error)
    {
        try
        {
            var info = new FileInfo(path);
            var logical = info.Length;
            return new Entry(NodeKind.File, name, logical, AllocatedOf(path, logical), info.LastWriteTimeUtc,
                system: (info.Attributes & FileAttributes.System) != 0);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or Win32Exception)
        {
            return Entry.Unreadable(name, error);
        }
    }

    /// <summary>Allocated bytes from directory metadata, without opening the file.</summary>
    private static long AllocatedOf(string path, long logical)
    {
        var low = GetCompressedFileSizeW(path, out var high);
        if (low == 0xFFFFFFFF && Marshal.GetLastWin32Error() != 0) return logical;
        return ((long)high << 32) | low;
    }

    /// <summary>Adds one listed entry to the tree. Folders are queued with their new node index.</summary>
    private static void ApplyEntry(string dir, int parentNode, in Entry e, ScanState s)
    {
        if (e.Gone) return;
        if (e.Kind == NodeKind.Dir)
        {
            var child = s.Tree.Add(parentNode, e.Name, NodeKind.Dir, 0, 0, e.Modified);
            if (child < 0) return;
            Interlocked.Increment(ref s.Pending);
            s.Work.Add((Path.Combine(dir, e.Name), child));
            return;
        }
        var idx = s.Tree.Add(parentNode, e.Name, e.Kind, e.Logical, e.Allocated, e.Modified,
            cloud: e.Cloud, repeat: e.Repeat, error: e.Error, system: e.System);
        if (idx >= 0 && e.Saved > 0) s.Tree.AddHardLinkSaved(e.Saved);
    }

    private sealed class ScanState
    {
        public ScanState(CancellationToken ct, IProgress<long>? progress, bool exact, DiskTree tree)
        {
            Ct = ct; Progress = progress; Exact = exact; Tree = tree;
        }
        public CancellationToken Ct { get; }
        public IProgress<long>? Progress { get; }
        public bool Exact { get; }
        public DiskTree Tree { get; }
        public bool UseIds; // backend B; turned off for the rest of the scan if a folder refuses the call
        public IdSet? Ids;  // backend B file IDs, sharded so workers do not all wait on one lock
        public ConcurrentDictionary<FileKey, byte>? Seen; // fallback hard-link dedupe; allocated only if a link appears
        public BlockingCollection<(string Dir, int Node)> Work { get; } = new();
        public long Visited;
        public long Pending = 1; // the root folder
    }

    /// <summary>Sharded set of 64-bit file IDs. One lock per shard keeps parallel listing fast.</summary>
    private sealed class IdSet
    {
        private const int Shards = 16;
        private readonly HashSet<ulong>[] _sets = new HashSet<ulong>[Shards];
        private readonly object[] _locks = new object[Shards];

        public IdSet()
        {
            for (var i = 0; i < Shards; i++) { _sets[i] = new HashSet<ulong>(); _locks[i] = new object(); }
        }

        public bool Add(ulong id)
        {
            var i = (int)(id & (Shards - 1));
            lock (_locks[i]) return _sets[i].Add(id);
        }
    }

    private readonly struct Entry
    {
        public readonly string Name;
        public readonly NodeKind Kind;
        public readonly long Logical, Allocated, Saved;
        public readonly DateTime Modified;
        public readonly bool Cloud, Repeat, System, Gone;
        public readonly string? Error;

        public Entry(NodeKind kind, string name, long logical, long allocated, DateTime modified,
            bool cloud = false, bool repeat = false, bool system = false, long saved = 0, string? error = null, bool gone = false)
        {
            Kind = kind; Name = name; Logical = logical; Allocated = allocated; Modified = modified;
            Cloud = cloud; Repeat = repeat; System = system; Saved = saved; Error = error; Gone = gone;
        }

        public static Entry GoneEntry() => new(NodeKind.File, "", 0, 0, DateTime.MinValue, gone: true);
        public static Entry Unreadable(string name, string error) =>
            new(NodeKind.Unreadable, name, 0, 0, DateTime.MinValue, error: error);
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetCompressedFileSizeW(string name, out uint high);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(string name, uint access, uint share, IntPtr sa,
        uint disposition, uint flags, IntPtr template);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandleEx(SafeFileHandle h, int cls, [Out] byte[] buf, uint size);
}


