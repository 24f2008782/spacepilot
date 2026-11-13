namespace CDClean.Core;

/// <summary>Downloads triage (F-03): files in the Downloads folder older than a floor (default 90 days). Vault/recycle only.</summary>
public static class Downloads
{
    public static List<FileItem> Find(string root, int olderThanDays = 90, CancellationToken ct = default)
    {
        var cutoff = DateTime.Now.AddDays(-olderThanDays);
        var result = new List<FileItem>();
        if (!Directory.Exists(root)) return result;
        foreach (var f in Scanner.Files(root))
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var fi = new FileInfo(f);
                if (fi.LastWriteTime >= cutoff) continue;
                result.Add(new FileItem(f, fi.Length));
            }
            catch { }
        }
        return result.OrderByDescending(x => x.Size).ToList();
    }
}
