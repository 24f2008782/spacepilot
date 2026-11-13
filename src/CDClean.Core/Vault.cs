using System.Security.Cryptography;
using CDClean.Core.Native;

namespace CDClean.Core;

/// <summary>
/// The backup vault: files are copied here, hash-checked, and only then removed from their original place.
/// Same folder layout and manifest.jsonl as 1.0, so old backup runs still list and restore.
/// </summary>
public static class Vault
{
    /// <summary>Backup location for a file: same folder structure, drive letter dropped.</summary>
    public static string BackupPath(string runDir, string original)
    {
        var full = Path.GetFullPath(original);
        var rel = full.Substring(Path.GetPathRoot(full)!.Length);
        return Path.Combine(runDir, "files", rel);
    }

    /// <summary>Creates a new job folder with a unique id (yyyy-MM-dd_HHmmss, plus _2, _3 on collisions).</summary>
    public static string NewJob(string root)
    {
        var id = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
        var dir = Path.Combine(root, id);
        for (var n = 2; Directory.Exists(dir); n++) dir = Path.Combine(root, $"{id}_{n}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "README.txt"),
            "CDClean backup job.\r\n\r\n" +
            "manifest.jsonl  one line per file: original path, backup path, size, SHA-256.\r\n" +
            "files\\          the files, in their original folder structure (drive letter dropped).\r\n" +
            "verify.txt      what was checked when the job finished.\r\n\r\n" +
            "To restore: open CDClean > Backups, pick this job, Restore.\r\n" +
            "By hand: copy each file from 'files' back to the path in manifest.jsonl.\r\n");
        return dir;
    }

    /// <summary>Copies the file into the job folder and checks the copy's hash. Does not touch the original.</summary>
    public static ManifestEntry Stage(string runDir, FileFacts f, string source)
    {
        var dest = BackupPath(runDir, f.Path);
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        string hash;
        using (var src = new FileStream(f.Path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 1 << 20))
        using (var dst = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20))
        using (var h = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
        {
            var buf = new byte[1 << 20];
            int n;
            while ((n = src.Read(buf, 0, buf.Length)) > 0) { h.AppendData(buf, 0, n); dst.Write(buf, 0, n); }
            dst.Flush(true);
            hash = Convert.ToHexString(h.GetHashAndReset());
        }
        if (Sha256(dest) != hash) throw new IOException("backup copy does not match the original (hash check failed)");
        return new ManifestEntry(f.Path, dest, f.Logical, source, hash, (int)f.Attributes, f.LastWriteUtc, f.CreationUtc);
    }

    public static string Sha256(string path)
    {
        using var s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 20);
        return Convert.ToHexString(SHA256.HashData(s));
    }

    /// <summary>True when the vault and the path are on the same drive, so staging frees no space there.</summary>
    public static bool SameDrive(string vaultRoot, string path) =>
        string.Equals(Path.GetPathRoot(Path.GetFullPath(vaultRoot)), Path.GetPathRoot(Path.GetFullPath(path)), StringComparison.OrdinalIgnoreCase);
}
