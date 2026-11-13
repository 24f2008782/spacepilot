using CDClean.Core;
using CDClean.Core.Safety;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;

namespace CDClean.App;

public enum ReviewChoice { Cancel, ShowHidden, Run }

/// <summary>The review dialogs every destructive or admin action goes through (report §4.5.2, §4.11, §8.6, §8.7).</summary>
public static class Review
{
    private static TextBlock Text(string s, bool strong = false, double size = 14) => new()
    {
        Text = s, TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true, FontSize = size,
        FontWeight = strong ? FontWeights.SemiBold : FontWeights.Normal,
    };

    private static Grid Row(string label, string value)
    {
        var g = new Grid { ColumnSpacing = 12 };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        g.Children.Add(new TextBlock { Text = label, Opacity = 0.7, TextWrapping = TextWrapping.Wrap });
        var v = Text(value);
        Grid.SetColumn(v, 1);
        g.Children.Add(v);
        return g;
    }

    private static string Free(IEnumerable<string> paths) => string.Join("   ", paths
        .Select(p => Path.GetPathRoot(p) ?? "").Where(r => r.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase)
        .Select(r => { try { return $"{r.TrimEnd('\\')} {Fmt.Size(new DriveInfo(r).AvailableFreeSpace)} free"; } catch { return ""; } }));

    private static string Grouped(IEnumerable<PlanItem> items) => string.Join("\n", items.SelectMany(i => i.Reasons)
        .GroupBy(r => r).OrderByDescending(g => g.Count()).Select(g => $"• {g.Count():N0} {Gate.Text(g.Key)}"));

    /// <summary>
    /// Shows the delete review. Hidden selections are excluded unless the user ticks the box; items with warnings
    /// can be unticked one by one; blocked items are listed and never run. Returns the plan to execute.
    /// </summary>
    public static async Task<(ReviewChoice Choice, JobPlan? Plan)> Delete(XamlRoot root, JobPlan plan, string title, string verb)
    {
        var s = State.Settings;
        var act = plan.Actionable.ToList();
        var hidden = act.Where(i => i.Target.Hidden).ToList();
        var warned = act.Where(i => i.Verdict == Verdict.Warn).ToList();
        var blocked = plan.Items.Where(i => i.Verdict == Verdict.Block).ToList();
        var excluded = new HashSet<PlanItem>();
        var includeHidden = false;
        var ack = new CheckBox { Content = "I have reviewed the items with warnings", Visibility = warned.Count > 0 ? Visibility.Visible : Visibility.Collapsed };
        var dlg = new ContentDialog
        {
            XamlRoot = root, Title = title, DefaultButton = ContentDialogButton.Close, CloseButtonText = "Cancel",
            SecondaryButtonText = hidden.Count > 0 ? $"Review {hidden.Count:N0} hidden" : "",
        };

        var body = new StackPanel { Spacing = 10, MaxWidth = 640 };
        var head = Text("", strong: true, size: 16);
        body.Children.Add(head);
        body.Children.Add(Text("Free space now (measured): " + Free(plan.Items.Select(i => i.Facts.Path).Append(s.BackupRoot)), size: 13));

        CheckBox? hiddenBox = null;
        if (hidden.Count > 0)
        {
            body.Children.Add(Text($"{hidden.Count:N0} selected items are hidden by the search ({Fmt.Size(hidden.Sum(i => i.Facts.Allocated))}). They are left out unless you include them here."));
            hiddenBox = new CheckBox { Content = $"Also include the {hidden.Count:N0} hidden items" };
            body.Children.Add(hiddenBox);
        }

        var methods = act.Select(i => i.Target.Method).Distinct().ToList();
        body.Children.Add(Row("Reversible", string.Join("\n", methods.Select(m => m switch
        {
            Method.Vault => $"Copied to {s.BackupRoot}, hash-checked, then removed. Restore from Backups (pruned after {s.KeepDays} days if you prune)."
                + (act.Any(i => i.Target.Method == Method.Vault && Vault.SameDrive(s.BackupRoot, i.Facts.Path)) ? " The backup is on the same drive, so no space is freed there until it is deleted." : ""),
            Method.Recycle => "Recycle Bin: restore from there. Space is freed only when the Recycle Bin is emptied.",
            _ => "Permanent. Used only for caches that rebuild themselves.",
        }))));
        body.Children.Add(Row("Privilege", "Standard user. No admin rights needed."));
        if (act.Count > 0)
        {
            var top = act.MaxBy(i => i.Risk)!;
            body.Children.Add(Row("Highest risk", $"{top.Risk}: {top.Facts.Path}"));
        }

        if (warned.Count > 0)
        {
            body.Children.Add(Text($"Warnings on {warned.Count:N0} items. Untick any you want to keep:", strong: true));
            body.Children.Add(Text(Grouped(warned), size: 13));
            var list = new StackPanel { Spacing = 2 };
            foreach (var w in warned.Take(300))
            {
                var cb = new CheckBox
                {
                    IsChecked = true, FontSize = 12,
                    Content = new TextBlock { Text = $"{w.Facts.Path}  ({string.Join(", ", w.Reasons.Select(Gate.Text))})", TextWrapping = TextWrapping.Wrap, FontSize = 12 },
                };
                var item = w;
                cb.Click += (_, _) => { if (cb.IsChecked == true) excluded.Remove(item); else excluded.Add(item); Update(); };
                list.Children.Add(cb);
            }
            if (warned.Count > 300) list.Children.Add(Text($"...and {warned.Count - 300:N0} more with the same kinds of warning.", size: 12));
            body.Children.Add(new Expander { Header = "Items with warnings", Content = list, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch });
        }

        if (blocked.Count > 0)
        {
            body.Children.Add(Text($"{blocked.Count:N0} items will NOT be touched:", strong: true));
            body.Children.Add(Text(Grouped(blocked), size: 13));
        }

        body.Children.Add(Text("Will not touch: Windows, WindowsApps, System Volume Information, your protected folders, the backup folder, cookies, passwords and browser history.", size: 12));
        body.Children.Add(Text("Usage is judged from file dates. That cannot prove a file is unused.", size: 12));

        body.Children.Add(ack);
        dlg.Content = new ScrollViewer { Content = body, MaxHeight = 560 };

        List<PlanItem> Kept() => act.Where(i => !excluded.Contains(i) && (includeHidden || !i.Target.Hidden)).ToList();
        void Update()
        {
            includeHidden = hiddenBox?.IsChecked == true;
            var kept = Kept();
            head.Text = $"{kept.Count:N0} items · {Fmt.Size(kept.Sum(i => i.Facts.Allocated))} on disk"
                        + (hidden.Count > 0 ? $"   ({kept.Count(i => !i.Target.Hidden):N0} visible, {(includeHidden ? hidden.Count : 0):N0} hidden included)" : "");
            dlg.PrimaryButtonText = $"{verb} {kept.Count:N0} items";
            var needAck = kept.Any(i => i.Verdict == Verdict.Warn);
            dlg.IsPrimaryButtonEnabled = kept.Count > 0 && (!needAck || ack.IsChecked == true);
        }
        if (hiddenBox != null) hiddenBox.Click += (_, _) => Update();
        ack.Click += (_, _) => Update();
        Update();

        var result = await Ui.Show(dlg);
        if (result == ContentDialogResult.Secondary) return (ReviewChoice.ShowHidden, null);
        if (result != ContentDialogResult.Primary) return (ReviewChoice.Cancel, null);
        var keep = Kept().ToHashSet();
        return (ReviewChoice.Run, plan.Keep(i => keep.Contains(i) || i.Verdict == Verdict.Block)); // blocked stay in for the audit log
    }

    /// <summary>Review before an admin task (report §4.11): what runs, exactly, and what it costs. Irreversible tasks need a typed word.</summary>
    public static async Task<bool> Admin(XamlRoot root, AdminTask t)
    {
        var body = new StackPanel { Spacing = 8, MaxWidth = 620 };
        body.Children.Add(Row("What it does", t.What));
        body.Children.Add(Row("Estimate", $"{t.Gain} (estimate). Free space on C: is measured before and after, and the measured number is shown when it finishes."));
        body.Children.Add(Row("Free now", Free(new[] { @"C:\" })));
        body.Children.Add(Row("Duration", t.Duration));
        body.Children.Add(Row("Privilege", "Administrator. Windows asks for permission (UAC) because it changes system files or settings."));
        body.Children.Add(Row("Reversible", t.Undo));
        body.Children.Add(Row("Restart", t.Restart));
        body.Children.Add(Row("Risks", t.Risks));
        body.Children.Add(Row("Not touched", t.Excluded + " No other drives. No registry keys besides what is listed."));

        var cmds = new TextBlock { Text = t.Commands, FontFamily = new FontFamily("Consolas"), FontSize = 12, TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true };
        var copy = new Button { Content = "Copy commands" };
        copy.Click += (_, _) => { var d = new DataPackage(); d.SetText(t.Commands); Clipboard.SetContent(d); };
        var cmdPanel = new StackPanel { Spacing = 6 };
        cmdPanel.Children.Add(cmds);
        cmdPanel.Children.Add(copy);
        body.Children.Add(new Expander { Header = "Exact commands", Content = cmdPanel, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch });

        var dlg = new ContentDialog
        {
            XamlRoot = root, Title = "Review: " + t.Title, DefaultButton = ContentDialogButton.Close, CloseButtonText = "Cancel",
            PrimaryButtonText = "Run and measure", Content = new ScrollViewer { Content = body, MaxHeight = 560 },
        };

        if (t.Class == ConfirmClass.Irreversible)
        {
            body.Children.Add(new Border
            {
                BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 0xC6, 0x28, 0x28)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(10),
                Child = Text($"This cannot be undone. Type {t.Token} to continue.", strong: true),
            });
            var box = new TextBox { PlaceholderText = t.Token };
            body.Children.Add(box);
            dlg.IsPrimaryButtonEnabled = false;
            box.TextChanged += (_, _) => dlg.IsPrimaryButtonEnabled = box.Text.Trim() == t.Token;
        }
        else if (t.Class == ConfirmClass.Elevated)
        {
            var ack = new CheckBox { Content = $"I understand: {t.Consequence}." };
            body.Children.Add(ack);
            dlg.IsPrimaryButtonEnabled = false;
            ack.Click += (_, _) => dlg.IsPrimaryButtonEnabled = ack.IsChecked == true;
        }
        return await Ui.Show(dlg) == ContentDialogResult.Primary;
    }
}
