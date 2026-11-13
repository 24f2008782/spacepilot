using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using CDClean.Core.Native;
using CDClean.Core.Safety;
using CDClean.Core.Selection;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace CDClean.App.Controls;

/// <summary>One row in the shared table. Selection lives in the table's SelectionSet, keyed by Key; Checked mirrors it.</summary>
[Microsoft.UI.Xaml.Data.Bindable]
public sealed class GridItem : INotifyPropertyChanged
{
    public FileKey Key { get; set; }
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Path { get; set; } = "";
    /// <summary>Bytes; -1 when unknown.</summary>
    public long Size { get; set; }
    public int? Count { get; set; }
    public DateTime? Modified { get; set; }
    public int? Depth { get; set; }
    public string LastUsed { get; set; } = "";
    public string App { get; set; } = "";
    public string Extra { get; set; } = "";
    /// <summary>Where the inventory came from: MSI, Registry 64-bit, Registry 32-bit, Registry (user), MSIX.</summary>
    public string Source { get; set; } = "";
    public RiskClass? Risk { get; set; }
    public string Action { get; set; } = "";
    public object? Tag { get; set; }

    public string SizeText => Size < 0 ? "?" : Fmt.Size(Size);
    public string CountText => Count?.ToString("N0") ?? "";
    public int? AgeMonths => Modified is DateTime m ? (int)((DateTime.Now - m).TotalDays / 30.44) : null;
    public string AgeText => AgeMonths?.ToString() ?? "";
    public string ModifiedText => Modified?.ToString("yyyy-MM-dd") ?? "";
    public string DepthText => Depth?.ToString() ?? "";
    public string RiskText => Risk switch { RiskClass.Safe => "Safe", RiskClass.Review => "Review", RiskClass.High => "High", RiskClass.Protected => "Protected", _ => "" };
    public Brush RiskBrush => new SolidColorBrush(Risk switch
    {
        RiskClass.Safe => ColorHelper.FromArgb(255, 0x2E, 0x9E, 0x6B),
        RiskClass.Review => ColorHelper.FromArgb(255, 0xC9, 0x8A, 0x00),
        RiskClass.High => ColorHelper.FromArgb(255, 0xD9, 0x58, 0x2B),
        RiskClass.Protected => ColorHelper.FromArgb(255, 0x7A, 0x88, 0x99), // grey: a lock, not a warning
        _ => Colors.Transparent,
    });

    internal ItemsGrid? Owner { get; set; }
    private SelRecord? _rec;
    internal SelRecord Rec => _rec ??= new SelRecord { Key = Key, Bytes = Math.Max(0, Size), Path = Path, Tag = this };

    private bool _checked;
    /// <summary>Bound to the row checkbox. Setting it from code before the row is added pre-selects the row.</summary>
    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value) return;
            _checked = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Checked)));
            Owner?.OnRowChecked(this, value);
        }
    }

    internal void SetChecked(bool v)
    {
        if (_checked == v) return;
        _checked = v;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Checked)));
    }

    internal bool Matches(string text) =>
        text.Length == 0 || Name.Contains(text, StringComparison.OrdinalIgnoreCase) || Path.Contains(text, StringComparison.OrdinalIgnoreCase)
        || App.Contains(text, StringComparison.OrdinalIgnoreCase) || Type.Contains(text, StringComparison.OrdinalIgnoreCase)
        || Extra.Contains(text, StringComparison.OrdinalIgnoreCase);

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The file's identity if it exists; otherwise a stable key made from the text.</summary>
    public static FileKey KeyOf(string path)
    {
        try { var f = Files.Read(path); if (f.Exists) return f.Key; } catch { }
        return IdKey(path);
    }

    /// <summary>Key for things that are not files (apps). Volume 0 never clashes with a real volume serial.</summary>
    public static FileKey IdKey(string id)
    {
        var h = SHA256.HashData(Encoding.UTF8.GetBytes(id.ToUpperInvariant()));
        return new FileKey(0, BitConverter.ToUInt64(h, 0), BitConverter.ToUInt64(h, 8));
    }

    /// <summary>Risk for a path, read without the lock check (that happens again before any action).</summary>
    public static RiskClass RiskOf(string path, Category cat, Policy pol)
    {
        try { var f = Files.Read(path); return f.Exists ? Core.Safety.Risk.Classify(f, cat, false, pol) : RiskClass.Review; }
        catch { return RiskClass.Review; }
    }
}
