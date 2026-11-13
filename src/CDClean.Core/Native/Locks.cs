using System.Runtime.InteropServices;

namespace CDClean.Core.Native;

/// <summary>Which programs hold a file open, via the Windows Restart Manager (report F-17). Read-only.</summary>
public static class Locks
{
    public static List<string> Holders(string path)
    {
        var names = new List<string>();
        var key = Guid.NewGuid().ToString();
        if (RmStartSession(out var session, 0, key) != 0) return names;
        try
        {
            if (RmRegisterResources(session, 1, new[] { path }, 0, null, 0, null) != 0) return names;
            uint needed = 0, count = 0;
            var r = RmGetList(session, out needed, ref count, null, out _);
            if (r != 234 /*ERROR_MORE_DATA*/ || needed == 0) return names;
            var info = new RM_PROCESS_INFO[needed];
            count = needed;
            if (RmGetList(session, out needed, ref count, info, out _) != 0) return names;
            for (var i = 0; i < count; i++)
                names.Add($"{info[i].strAppName} (process {info[i].Process.dwProcessId})");
        }
        finally { RmEndSession(session); }
        return names;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RM_UNIQUE_PROCESS { public int dwProcessId; public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct RM_PROCESS_INFO
    {
        public RM_UNIQUE_PROCESS Process;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string strAppName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string strServiceShortName;
        public int ApplicationType;
        public uint AppStatus;
        public uint TSSessionId;
        [MarshalAs(UnmanagedType.Bool)] public bool bRestartable;
    }

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmStartSession(out uint session, int flags, string key);
    [DllImport("rstrtmgr.dll")]
    private static extern int RmEndSession(uint session);
    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmRegisterResources(uint session, uint nFiles, string[] files, uint nApps, RM_UNIQUE_PROCESS[]? apps, uint nServices, string[]? services);
    [DllImport("rstrtmgr.dll")]
    private static extern int RmGetList(uint session, out uint needed, ref uint count, [In, Out] RM_PROCESS_INFO[]? info, out uint rebootReasons);
}
