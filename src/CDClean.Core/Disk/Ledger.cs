namespace CDClean.Core.Disk;

/// <summary>One line of the ledger. InTotal lines add up to the drive's used space; the others are for information.</summary>
public sealed record LedgerLine(string Name, long Bytes, string Note, bool InTotal);

/// <summary>
/// Storage ledger (report F-05, §4.12.2): accounts for the drive's used space in named buckets.
/// The "Not attributed" bucket is whatever the scan could not name. It is shown, never hidden.
/// </summary>
public static class Ledger
{
    public static List<LedgerLine> Build(DiskTree tree, long driveUsed, bool wholeDrive)
    {
        long system = 0, cloudLogical = 0;
        for (var i = 0; i < tree.Nodes.Count; i++)
        {
            var n = tree.Nodes[i];
            if (n.Kind == NodeKind.File && n.SystemFile) system += n.OwnAllocated;
            if (n.Cloud) cloudLogical += n.OwnLogical;
        }
        var files = tree.TotalAllocated - system;   // system files are named on their own line
        var rest = driveUsed - files - system;
        var unreadable = tree.UnreadableCount;

        var restNote = rest < 0
            ? "The scan found more than the drive reports as used. Files changed during the scan, or a folder was reached through a link."
            : wholeDrive
                ? "NTFS metadata ($MFT, logs, bitmaps), shadow copies, cluster slack, and folders that could not be read."
                : "Everything else on this drive, outside the scanned folder.";

        return new List<LedgerLine>
        {
            new("Used on the drive (as Windows reports it)", driveUsed, "Total minus free, measured now.", true),
            new("Files and folders on disk", files, "Allocated size. Hard links counted once; cloud placeholders 0 on disk.", true),
            new("System files (pagefile, hiberfil, swapfile)", system, "Files with the system attribute, at any depth.", true),
            new("Not attributed", rest, restNote, true),
            new("Hard-link duplicates", tree.HardLinkSaved, "Extra names for data already counted. Not extra space, so not in the total.", false),
            new("Cloud placeholders", cloudLogical, "Logical size of cloud files. Not on this disk, so not in the total.", false),
            new("Unreadable folders", 0, unreadable == 0 ? "None." : $"{unreadable} item(s) could not be read, so their size is unknown. Their space is inside 'Not attributed'.", false),
        };
    }
}
