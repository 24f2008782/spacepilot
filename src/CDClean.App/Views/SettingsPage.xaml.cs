using CDClean.Core;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace CDClean.App.Views;

[Microsoft.UI.Xaml.Data.Bindable]
public sealed class SettingsResult
{
    public string Category { get; set; } = "";
    public string Name { get; set; } = "";
    public string Detail { get; set; } = "";
}

public sealed partial class SettingsPage : Page
{
    // Decision (approved 2026-11-12): one ADVANCED unlock per app session. Restarting the app requires typing it again.
    private static bool _advancedUnlocked;

    private string _selected = SettingsSearch.Categories[0];

    public SettingsPage()
    {
        InitializeComponent();
        CategoryList.ItemsSource = SettingsSearch.Categories;
        CategoryList.SelectedIndex = 0;

        if (AppSettings.LastLoadNotice is { } notice)
        {
            SettingsNotice.Message = notice;
            SettingsNotice.IsOpen = true;
        }
        SettingsNotice.Closed += (_, _) => AppSettings.ClearLoadNotice();

        BuildPanel(_selected);
    }

    private static Brush Secondary => (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
    private static Style Accent => (Style)Application.Current.Resources["AccentButtonStyle"];

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var query = SearchBox.Text.Trim();
        if (query.Length == 0)
        {
            SearchResultsList.SelectedItem = null;
            SearchResultsList.Visibility = Visibility.Collapsed;
            CategoryList.Visibility = Visibility.Visible;
            CategoryList.ItemsSource = SettingsSearch.Categories;
            CategoryList.SelectedItem = _selected;
            BuildPanel(_selected);
            return;
        }

        var results = SettingsSearch.Find(query)
            .Select(s => new SettingsResult
            {
                Category = s.Category,
                Name = s.Name,
                Detail = s.Dangerous ? s.Category + " · Advanced risk" : s.Category,
            })
            .ToList();
        SearchResultsList.ItemsSource = results;
        SearchResultsList.Visibility = Visibility.Visible;
        CategoryList.Visibility = Visibility.Collapsed;

        ContentPanel.Children.Clear();
        AddTitle("Search settings");
        AddBody($"{results.Count:N0} setting(s) match “{query}”. Select a result on the left.");
        if (results.Count == 0)
            AddBody("Try a different word, such as backup, vault, protected, rules, or low space.");
    }

    private void CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryList.SelectedItem is string category)
        {
            _selected = category;
            BuildPanel(category);
        }
    }

    private void SearchResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SearchResultsList.SelectedItem is SettingsResult result)
        {
            _selected = result.Category;
            BuildPanel(result.Category);
        }
    }

    private void BuildPanel(string category)
    {
        ContentPanel.Children.Clear();
        AddTitle(category);

        switch (category)
        {
            case "Cleanup rules":
                BuildCleanupRulesPanel();
                break;
            case "Exclusions & protected":
                BuildProtectedPanel();
                break;
            case "Backups & recovery":
                BuildBackupsPanel();
                break;
            case "Notifications":
                BuildNotificationsPanel();
                break;
            case "Privacy & diagnostics":
                BuildPrivacyPanel();
                break;
            case "Advanced":
                BuildAdvancedPanel();
                break;
            case "About & updates":
                BuildAboutPanel();
                break;
            default:
                BuildPlaceholder(category);
                break;
        }
    }

    private void BuildPlaceholder(string category)
    {
        AddCard("No editable settings in this batch",
            $"{category} is a placeholder. Batch 3.1 only exposes settings that already have a backing field.",
            new TextBlock { Text = "Nothing to change here yet.", Foreground = Secondary, TextWrapping = TextWrapping.Wrap });
    }

    private void BuildCleanupRulesPanel()
    {
        var s = State.Settings;

        var current = new TextBlock { Text = s.ResolvedRulesPath, TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true };
        var change = new Button { Content = "Change in Advanced" };
        change.Click += (_, _) => GoToCategory("Advanced");
        var rulesPanel = new StackPanel { Spacing = 6 };
        rulesPanel.Children.Add(current);
        rulesPanel.Children.Add(change);
        AddCard("Rules file", "Empty uses rules.toml next to the app. A custom path is an advanced risk setting.", rulesPanel);

        var enabledText = new TextBlock
        {
            Text = s.EnabledRules.Count == 0 ? "All safe rules are considered." : $"{s.EnabledRules.Count:N0} rule(s) ticked on the Rules page.",
            TextWrapping = TextWrapping.Wrap,
        };
        var openRules = new Button { Content = "Open Rules page" };
        openRules.Click += (_, _) => Frame.Navigate(typeof(RulesPage));
        var enabledPanel = new StackPanel { Spacing = 6 };
        enabledPanel.Children.Add(enabledText);
        enabledPanel.Children.Add(openRules);
        AddCard("Enabled rules", "Scheduled runs execute only Safe rules. Manual rules are queued for review instead of being deleted.", enabledPanel);
    }

    private void BuildProtectedPanel()
    {
        var s = State.Settings;
        var list = new StackPanel { Spacing = 6 };
        if (s.ProtectedPaths.Count == 0)
        {
            list.Children.Add(new TextBlock { Text = "No extra protected paths.", Foreground = Secondary, TextWrapping = TextWrapping.Wrap });
        }
        else
        {
            foreach (var path in s.ProtectedPaths.ToList())
            {
                var row = new Grid { ColumnSpacing = 8 };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.Children.Add(new TextBlock { Text = path, TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true, VerticalAlignment = VerticalAlignment.Center });
                var remove = new Button { Content = "Remove" };
                remove.Click += async (_, _) => await RemoveProtectedPath(path);
                Grid.SetColumn(remove, 1);
                row.Children.Add(remove);
                list.Children.Add(row);
            }
        }

        var input = new TextBox { PlaceholderText = "Folder or file path", Width = 420 };
        var browse = new Button { Content = "Browse" };
        browse.Click += async (_, _) => { if (await Ui.PickFolder() is { } p) input.Text = p; };
        var add = new Button { Content = "Add" };
        var status = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Foreground = Secondary };
        add.Click += (_, _) => AddProtectedPath(input, status);

        var addRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        addRow.Children.Add(input);
        addRow.Children.Add(browse);
        addRow.Children.Add(add);

        var content = new StackPanel { Spacing = 8 };
        content.Children.Add(list);
        content.Children.Add(addRow);
        content.Children.Add(status);
        AddCard("Protected folders", "These paths are never offered for deletion. Clearing the whole list is an advanced action.", content);

        var advanced = new Button { Content = "Clear all in Advanced" };
        advanced.Click += (_, _) => GoToCategory("Advanced");
        AddCard("Clear the list", "Removing every extra protection is dangerous, so it is behind the ADVANCED unlock.", advanced);
    }

    private async Task RemoveProtectedPath(string path)
    {
        if (!await Ui.Confirm(XamlRoot, "Remove protected path?", $"{path}\n\nSpacePilot may offer this path for deletion after removal.", "Remove")) return;
        var before = SettingsStore.Clone(State.Settings);
        State.Settings.ProtectedPaths.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
        if (IsDangerousChange(before, State.Settings) && !await EnsureAdvancedUnlocked())
        {
            SettingsStore.CopyFrom(before, State.Settings);
            return;
        }
        State.Settings.Save();
        BuildPanel(_selected);
    }

    private void AddProtectedPath(TextBox input, TextBlock status)
    {
        var text = input.Text.Trim();
        if (text.Length == 0) { status.Text = "Enter a path."; return; }
        string path;
        try { path = Path.GetFullPath(text); }
        catch { status.Text = "That path is not valid."; return; }
        if (State.Settings.ProtectedPaths.Contains(path, StringComparer.OrdinalIgnoreCase)) { status.Text = "Already protected."; return; }
        State.Settings.ProtectedPaths.Add(path);
        State.Settings.Save();
        BuildPanel(_selected);
    }

    private void BuildBackupsPanel()
    {
        var s = State.Settings;
        var root = new TextBox { Header = "Backup folder (use another drive)", Text = s.BackupRoot, Width = 420 };
        var browse = new Button { Content = "Browse", VerticalAlignment = VerticalAlignment.Bottom };
        browse.Click += async (_, _) => { if (await Ui.PickFolder() is { } p) root.Text = p; };
        var rootRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        rootRow.Children.Add(root);
        rootRow.Children.Add(browse);
        AddCard("Backup folder", "The vault stores archived files here. Keep it on another drive so C: can free space.",
            FieldRow(rootRow, nameof(AppSettings.BackupRoot)));

        var keep = new NumberBox
        {
            Header = "Keep backups for (days)", Value = s.KeepDays, Minimum = 1, Maximum = 3650,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact, Width = 200, HorizontalAlignment = HorizontalAlignment.Left,
        };
        AddCard("Keep backups for", "Older vault runs are pruned after this many days when you prune.",
            FieldRow(keep, nameof(AppSettings.KeepDays)));

        var capText = new TextBlock { Text = s.VaultCapGb == 0 ? "No cap." : $"{s.VaultCapGb:N0} GB.", TextWrapping = TextWrapping.Wrap };
        var capButton = new Button { Content = "Change in Advanced" };
        capButton.Click += (_, _) => GoToCategory("Advanced");
        var capPanel = new StackPanel { Spacing = 6 };
        capPanel.Children.Add(capText);
        capPanel.Children.Add(capButton);
        AddCard("Vault size cap", "0 means no cap. Removing the cap is an advanced risk setting.", capPanel);

        var save = new Button { Content = "Save", Style = Accent };
        var status = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Foreground = Secondary };
        save.Click += (_, _) =>
        {
            var folder = root.Text.Trim();
            if (folder.Length == 0) { status.Text = "Choose a backup folder."; return; }
            if (folder.StartsWith("C:", StringComparison.OrdinalIgnoreCase))
            {
                status.Text = "Pick a folder on another drive, so backups don't use C:.";
                return;
            }
            var days = double.IsNaN(keep.Value) ? 30 : (int)Math.Clamp(keep.Value, 1.0, 3650.0);
            State.Settings.BackupRoot = folder;
            State.Settings.KeepDays = days;
            State.Settings.Save();
            status.Text = "Saved.";
        };
        AddCard("Save backup settings", "Saves the folder and retention together using the atomic settings store.", SaveRow(save, status));
    }

    private void BuildNotificationsPanel()
    {
        var low = new NumberBox
        {
            Header = "Warn when C: free space is below (GB)", Value = State.Settings.LowSpaceGb, Minimum = 1, Maximum = 2000,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact, Width = 300, HorizontalAlignment = HorizontalAlignment.Left,
        };
        AddCard("Low-space warning", "The Dashboard warns when C: free space falls below this value.",
            FieldRow(low, nameof(AppSettings.LowSpaceGb)));

        var save = new Button { Content = "Save", Style = Accent };
        var status = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Foreground = Secondary };
        save.Click += (_, _) =>
        {
            State.Settings.LowSpaceGb = double.IsNaN(low.Value) ? 60 : (int)Math.Clamp(low.Value, 1.0, 2000.0);
            State.Settings.Save();
            status.Text = "Saved.";
        };
        AddCard("Save notification settings", "The value is stored locally and checked when the Dashboard loads.", SaveRow(save, status));
    }

    private void BuildPrivacyPanel()
    {
        var redact = new CheckBox
        {
            Content = "Hide paths in the local audit log",
            IsChecked = State.Settings.RedactAudit,
        };
        AddCard("Audit log", "The audit log stays on this PC. Redaction removes full paths before entries are written.",
            FieldRow(redact, nameof(AppSettings.RedactAudit)));

        var save = new Button { Content = "Save", Style = Accent };
        var status = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Foreground = Secondary };
        save.Click += (_, _) =>
        {
            State.Settings.RedactAudit = redact.IsChecked == true;
            Audit.Redact = State.Settings.RedactAudit;
            State.Settings.Save();
            status.Text = "Saved.";
        };
        AddCard("Save privacy settings", "Changes apply to new audit entries.", SaveRow(save, status));

        var openAudit = new Button { Content = "Open audit folder" };
        openAudit.Click += (_, _) =>
        {
            var dir = Path.Combine(AppSettings.DataDir, "audit");
            Directory.CreateDirectory(dir);
            Ui.ShowInExplorer(dir);
        };
        AddCard("Diagnostics", "No telemetry is sent. Crash reporting is opt-in and shown before sending.", openAudit);
    }

    private void BuildAdvancedPanel()
    {
        var s = State.Settings;
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock { Text = "Advanced risk settings", FontWeight = FontWeights.SemiBold, FontSize = 16 });
        panel.Children.Add(new TextBlock
        {
            Text = "These changes can make SpacePilot delete more. They are intentionally separated from everyday settings.",
            TextWrapping = TextWrapping.Wrap, Foreground = Secondary,
        });

        if (!_advancedUnlocked)
        {
            var box = new TextBox { PlaceholderText = "Type ADVANCED", Width = 260, IsTextPredictionEnabled = false };
            var unlock = new Button { Content = "Unlock", IsEnabled = false };
            box.TextChanged += (_, _) => unlock.IsEnabled = box.Text.Trim() == "ADVANCED";
            unlock.Click += (_, _) =>
            {
                if (box.Text.Trim() != "ADVANCED") return;
                _advancedUnlocked = true;
                BuildPanel("Advanced");
            };
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            row.Children.Add(box);
            row.Children.Add(unlock);
            panel.Children.Add(row);
            panel.Children.Add(new TextBlock
            {
                Text = "One unlock lasts for this app session. Restarting requires typing it again.",
                TextWrapping = TextWrapping.Wrap, Foreground = Secondary, FontSize = 12,
            });
        }
        else
        {
            panel.Children.Add(new TextBlock { Text = "Unlocked for this app session.", Foreground = Secondary });

            var rules = new TextBox
            {
                Header = "Rules file (empty = rules.toml next to the app)", Text = s.RulesPath,
                Width = 520, HorizontalAlignment = HorizontalAlignment.Left,
            };
            var rulesSave = new Button { Content = "Save" };
            var rulesStatus = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Foreground = Secondary };
            rulesSave.Click += (_, _) =>
            {
                var path = rules.Text.Trim();
                if (path.Length > 0 && !File.Exists(path)) { rulesStatus.Text = "Rules file not found."; return; }
                State.Settings.RulesPath = path;
                State.Settings.Save();
                rulesStatus.Text = "Saved.";
            };
            panel.Children.Add(FieldRow(rules, nameof(AppSettings.RulesPath)));
            panel.Children.Add(SaveRow(rulesSave, rulesStatus));

            var cap = new NumberBox
            {
                Header = "Vault size cap (GB, 0 = no cap)", Value = s.VaultCapGb, Minimum = 0, Maximum = 10000,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact, Width = 200, HorizontalAlignment = HorizontalAlignment.Left,
            };
            var capSave = new Button { Content = "Save" };
            var capStatus = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Foreground = Secondary };
            capSave.Click += (_, _) =>
            {
                State.Settings.VaultCapGb = double.IsNaN(cap.Value) ? 0 : (int)Math.Clamp(cap.Value, 0.0, 10000.0);
                State.Settings.Save();
                capStatus.Text = State.Settings.VaultCapGb == 0 ? "Saved. No cap is now set." : "Saved.";
            };
            panel.Children.Add(FieldRow(cap, nameof(AppSettings.VaultCapGb)));
            panel.Children.Add(SaveRow(capSave, capStatus));

            var protectedText = new TextBlock
            {
                Text = s.ProtectedPaths.Count == 0 ? "No extra protected paths." : $"{s.ProtectedPaths.Count:N0} extra protected path(s).",
                TextWrapping = TextWrapping.Wrap,
            };
            var clear = new Button { Content = "Clear all protected paths" };
            var clearStatus = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Foreground = Secondary };
            clear.Click += async (_, _) =>
            {
                if (State.Settings.ProtectedPaths.Count == 0) { clearStatus.Text = "No extra protected paths."; return; }
                if (!await Ui.Confirm(XamlRoot, "Clear protected paths?", "This removes every extra protected path. Built-in Windows protections still apply.", "Clear all")) return;
                State.Settings.ProtectedPaths.Clear();
                State.Settings.Save();
                BuildPanel("Advanced");
            };
            panel.Children.Add(protectedText);
            panel.Children.Add(clear);
            panel.Children.Add(clearStatus);
        }

        ContentPanel.Children.Add(new Border
        {
            BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 0xC6, 0x28, 0x28)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Child = panel,
        });
        AddBody("Every other category remains safe. Only the controls in this box can change the three dangerous values.");
    }

    private void BuildAboutPanel()
    {
        var version = typeof(App).Assembly.GetName().Version?.ToString(3) ?? "1.6.0";
        AddCard("Version", "Installed SpacePilot build.",
            new TextBlock { Text = $"SpacePilot {version}", FontSize = 16, FontWeight = FontWeights.SemiBold, IsTextSelectionEnabled = true });

        var paths = new TextBlock
        {
            Text = $"App folder: {AppContext.BaseDirectory}\nData and logs: {AppSettings.DataDir}",
            TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true, Foreground = Secondary,
        };
        var logs = new Button { Content = "Open logs folder" };
        logs.Click += (_, _) => { Directory.CreateDirectory(AppSettings.LogDir); Ui.ShowInExplorer(AppSettings.LogDir); };
        var data = new Button { Content = "Open data folder" };
        data.Click += (_, _) => Ui.ShowInExplorer(AppSettings.DataDir);
        var pathRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        pathRow.Children.Add(logs);
        pathRow.Children.Add(data);
        var pathPanel = new StackPanel { Spacing = 6 };
        pathPanel.Children.Add(paths);
        pathPanel.Children.Add(pathRow);
        AddCard("Data and logs", "Everything stays on this PC.", pathPanel);

        var notices = new Button { Content = "Third-party notices" };
        notices.Click += (_, _) => Ui.ShowInExplorer(Path.Combine(AppContext.BaseDirectory, "THIRD-PARTY-NOTICES.md"));
        var privacy = new Button { Content = "Privacy policy" };
        privacy.Click += (_, _) => Ui.ShowInExplorer(Path.Combine(AppContext.BaseDirectory, "PRIVACY.md"));
        var eula = new Button { Content = "Safety notice" };
        eula.Click += (_, _) => Ui.ShowInExplorer(Path.Combine(AppContext.BaseDirectory, "EULA.md"));
        var noticeRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        noticeRow.Children.Add(notices);
        noticeRow.Children.Add(privacy);
        noticeRow.Children.Add(eula);
        AddCard("Notices", "The bundled notices sit next to the executable.", noticeRow);

        var releases = new Button { Content = "Open GitHub releases" };
        releases.Click += (_, _) => Ui.Open("https://github.com/24f2008782/spacepilot/releases");
        var copy = new Button { Content = "Copy diagnostics" };
        copy.Click += (_, _) => Ui.Copy(
            $"SpacePilot {version}\nApp: {AppContext.BaseDirectory}\nData: {AppSettings.DataDir}\n" +
            $"Settings schema: {State.Settings.SchemaVersion}\nSettings file: {SettingsStore.FilePath()}");
        var updateRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        updateRow.Children.Add(releases);
        updateRow.Children.Add(copy);
        AddCard("Updates", "Updates are manual. Download the latest release from GitHub.", updateRow);
    }

    private async void ResetCategory_Click(object sender, RoutedEventArgs e)
    {
        var category = _selected;
        if (!await Ui.Confirm(XamlRoot, $"Reset {category}?", "Only settings in this category return to their defaults.", "Reset")) return;

        var before = SettingsStore.Clone(State.Settings);
        if (!SettingsStore.ResetCategory(State.Settings, category))
        {
            await Ui.Info(XamlRoot, "Nothing to reset", $"{category} has no editable settings in this batch.");
            return;
        }
        if (IsDangerousChange(before, State.Settings) && !await EnsureAdvancedUnlocked())
        {
            SettingsStore.CopyFrom(before, State.Settings);
            return;
        }
        State.Settings.Save();
        BuildPanel(category);
    }

    private async void ResetAll_Click(object sender, RoutedEventArgs e)
    {
        if (!await Ui.Confirm(XamlRoot, "Reset all settings?", "Every setting returns to its default, including advanced risk settings. This cannot be undone.", "Continue")) return;
        if (!await Ui.Confirm(XamlRoot, "Reset all settings?", "Second confirmation: reset every setting now?", "Reset all")) return;

        var before = SettingsStore.Clone(State.Settings);
        SettingsStore.ResetAll(State.Settings);
        if (IsDangerousChange(before, State.Settings) && !await EnsureAdvancedUnlocked())
        {
            SettingsStore.CopyFrom(before, State.Settings);
            return;
        }
        State.Settings.Save();
        BuildPanel(_selected);
    }

    private async Task ResetField(string field)
    {
        var before = SettingsStore.Clone(State.Settings);
        if (!SettingsStore.ResetField(State.Settings, field)) return;
        if (IsDangerousChange(before, State.Settings) && !await EnsureAdvancedUnlocked())
        {
            SettingsStore.CopyFrom(before, State.Settings);
            return;
        }
        State.Settings.Save();
        BuildPanel(_selected);
    }

    private async Task<bool> EnsureAdvancedUnlocked()
    {
        if (_advancedUnlocked) return true;
        var ok = await Ui.TypedConfirm(XamlRoot, "Advanced risk settings",
            "This change can make SpacePilot delete more. Type ADVANCED to unlock advanced settings for this app session.", "ADVANCED");
        if (ok) _advancedUnlocked = true;
        return ok;
    }

    private static bool IsDangerousChange(AppSettings before, AppSettings after)
    {
        if (!string.IsNullOrWhiteSpace(after.RulesPath)
            && !string.Equals(before.RulesPath.Trim(), after.RulesPath.Trim(), StringComparison.OrdinalIgnoreCase))
            return true;
        if (before.ProtectedPaths.Count > 0 && after.ProtectedPaths.Count == 0)
            return true;
        if (before.VaultCapGb > 0 && after.VaultCapGb == 0)
            return true;
        return false;
    }

    private void GoToCategory(string category)
    {
        _selected = category;
        SearchBox.Text = "";
        SearchResultsList.SelectedItem = null;
        SearchResultsList.Visibility = Visibility.Collapsed;
        CategoryList.Visibility = Visibility.Visible;
        CategoryList.ItemsSource = SettingsSearch.Categories;
        CategoryList.SelectedItem = category;
        BuildPanel(category);
    }

    private void AddTitle(string text) => ContentPanel.Children.Add(new TextBlock
    {
        Text = text,
        Style = (Style)Application.Current.Resources["TitleTextBlockStyle"],
    });

    private void AddBody(string text, bool secondary = true)
    {
        var block = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, FontSize = 13 };
        if (secondary) block.Foreground = Secondary;
        ContentPanel.Children.Add(block);
    }

    private void AddCard(string title, string description, FrameworkElement content)
    {
        var panel = new StackPanel { Spacing = 6 };
        panel.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
        if (!string.IsNullOrWhiteSpace(description))
            panel.Children.Add(new TextBlock { Text = description, TextWrapping = TextWrapping.Wrap, Foreground = Secondary, FontSize = 12 });
        panel.Children.Add(content);

        ContentPanel.Children.Add(new Border
        {
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Child = panel,
        });
    }

    private FrameworkElement FieldRow(FrameworkElement control, string? resetField = null)
    {
        if (resetField is null) return control;

        var row = new Grid { ColumnSpacing = 8 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.Children.Add(control);

        var reset = new Button { Content = "Reset", VerticalAlignment = VerticalAlignment.Bottom };
        reset.Click += async (_, _) => await ResetField(resetField);
        Grid.SetColumn(reset, 1);
        row.Children.Add(reset);
        return row;
    }

    private static FrameworkElement SaveRow(Button save, TextBlock status)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        row.Children.Add(save);
        row.Children.Add(status);
        return row;
    }
}
