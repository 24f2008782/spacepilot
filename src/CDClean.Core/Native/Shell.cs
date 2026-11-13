using System.Runtime.InteropServices;

namespace CDClean.Core.Native;

public static class Shell
{
    /// <summary>
    /// Sends paths to the Recycle Bin in one call. Returns false if Windows reported any failure.
    /// shortcut: SHFileOperation does not take paths over 260 characters; those fail and are reported. Upgrade to IFileOperation if that matters.
    /// </summary>
    public static bool Recycle(IReadOnlyCollection<string> paths)
    {
        if (paths.Count == 0) return true;
        var op = new SHFILEOPSTRUCTW
        {
            wFunc = 3, // FO_DELETE
            pFrom = string.Join('\0', paths) + "\0\0",
            fFlags = 0x40 | 0x10 | 0x4 | 0x400, // ALLOWUNDO | NOCONFIRMATION | SILENT | NOERRORUI
        };
        return SHFileOperationW(ref op) == 0 && !op.fAnyOperationsAborted;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEOPSTRUCTW
    {
        public IntPtr hwnd;
        public uint wFunc;
        public string pFrom;
        public string? pTo;
        public ushort fFlags;
        public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        public string? lpszProgressTitle;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHFileOperationW(ref SHFILEOPSTRUCTW op);
}
