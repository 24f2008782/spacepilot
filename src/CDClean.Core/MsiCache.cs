namespace CDClean.Core;

/// <summary>MSI patch / Installer cache auditor (F-18): analyses C:\Windows\Installer. MSI API only; vault before removal.</summary>
public static class MsiCache
{
    public sealed record CacheFile(string Path, long Bytes);

    public static List<CacheFile> Find(CancellationToken ct = default)
    {
        var result = new List<CacheFile>();
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Installer");
        if (!Directory.Exists(dir)) return result;
        try
        {
            foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
            {
                ct.ThrowIfCancellationRequested();
                try { result.Add(new CacheFile(f, new FileInfo(f).Length)); } catch { }
            }
        }
        catch { }
        return result.OrderByDescending(c => c.Bytes).ToList();
    }

    public static long TotalBytes(IEnumerable<CacheFile> files) => files.Sum(f => f.Bytes);
}
