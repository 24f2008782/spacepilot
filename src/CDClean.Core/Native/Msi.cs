using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace CDClean.Core.Native;

/// <summary>
/// Windows Installer API (msi.dll). Product codes let CDClean run a native MSI uninstall, and the
/// install location lets it match an MSI record to its install folder. Admin rights are needed for
/// a machine-wide remove; a per-user uninstall does not need them.
/// The P/Invoke layer is here; the inventory glue lives in Apps.
/// </summary>
public static class Msi
{
    private const string MsiDll = "msi.dll";
    private const uint ErrorSuccess = 0;
    private const uint ErrorMoreData = 234;
    private const uint ErrorNoMoreItems = 259;
    private const uint MsiInstallContextMachine = 2;
    private const uint MsiInstallContextUserManaged = 1;

    // Property names, as defined by the Installer API.
    public const string PropInstallLocation = "InstallLocation";
    public const string PropProductName = "ProductName";
    public const string PropVersionString = "VersionString";
    public const string PropPublisher = "Publisher";

    /// <summary>The product code (GUID) of every installed MSI product, machine and per-user.</summary>
    public static List<string> EnumerateProductCodes()
    {
        var codes = new List<string>();
        foreach (var context in new[] { MsiInstallContextMachine, MsiInstallContextUserManaged })
        {
            var index = 0u;
            while (true)
            {
                var buf = new StringBuilder(39); // {XXXXXXXX-....} + null
                var rc = MsiEnumProductsEx(null, null, context, index, buf, null, null, null);
                if (rc == ErrorNoMoreItems) break;
                if (rc != ErrorSuccess) break;
                var code = buf.ToString();
                if (code.Length > 0) codes.Add(code);
                index++;
            }
        }
        return codes;
    }

    /// <summary>One property of an installed product, or null when it is empty or unreadable.</summary>
    public static string? GetProductInfo(string productCode, string property)
    {
        if (string.IsNullOrWhiteSpace(productCode)) return null;
        var size = 512u;
        var sb = new StringBuilder((int)size);
        var rc = MsiGetProductInfo(productCode, property, sb, ref size);
        if (rc == ErrorSuccess) { var v = sb.ToString(); return v.Length > 0 ? v : null; }
        return null;
    }

    /// <summary>Install location for a product code, trimmed, or null when it is not recorded.</summary>
    public static string? InstallLocation(string productCode)
    {
        var v = GetProductInfo(productCode, PropInstallLocation);
        return string.IsNullOrWhiteSpace(v) ? null : v!.Trim().Trim('"');
    }

    /// <summary>
    /// Removes an MSI product through the Installer's own mechanism (REMOVE=ALL), which runs its
    /// UI and shows prompts. Returns the Installer result: 0 on success, otherwise a Windows error.
    /// The caller reports the code; nothing here closes or force-kills the uninstaller.
    /// </summary>
    public static uint Remove(string productCode)
    {
        if (string.IsNullOrWhiteSpace(productCode)) throw new ArgumentException("A product code is required.", nameof(productCode));
        return MsiConfigureProductEx(productCode, 0, 2, "REMOVE=ALL"); // level 0, INSTALLSTATE_ABSENT
    }

    /// <summary>False when msi.dll cannot be used here (non-Windows, Installer service disabled).</summary>
    public static bool Available()
    {
        try { _ = EnumerateProductCodes(); return true; }
        catch (DllNotFoundException) { return false; }
        catch (Exception e) when (e is InvalidOperationException or Win32Exception { NativeErrorCode: 2 or 126 })
        {
            return false;
        }
        catch { return false; }
    }

    [DllImport(MsiDll, CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint MsiEnumProductsEx(
        string? product, string? userSid, uint dwContext, uint dwIndex,
        StringBuilder? productCode, StringBuilder? userSidOut, StringBuilder? productName, StringBuilder? transform);

    [DllImport(MsiDll, CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint MsiGetProductInfo(string product, string property, StringBuilder valueBuf, ref uint valueBufSize);

    [DllImport(MsiDll, CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint MsiConfigureProductEx(string product, uint installLevel, int installState, string commandLine);
}
