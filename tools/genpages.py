# Generates the shared-layout XAML pages. Edit the PAGES table, then rerun.
import os
OUT = os.path.join(os.path.dirname(__file__), "..", "src", "CDClean.App", "Views")
ROW = '''      <ListView.ItemTemplate>
        <DataTemplate>
          <Grid Padding="2,6" ColumnSpacing="12">
            <Grid.ColumnDefinitions>
              <ColumnDefinition Width="Auto"/><ColumnDefinition Width="*"/><ColumnDefinition Width="200"/><ColumnDefinition Width="100"/>
            </Grid.ColumnDefinitions>
            <CheckBox IsChecked="{Binding Selected, Mode=TwoWay}" MinWidth="0" Visibility="CHECKVIS"/>
            <StackPanel Grid.Column="1" VerticalAlignment="Center">
              <TextBlock Text="{Binding Title}" TextTrimming="CharacterEllipsis"/>
              <TextBlock Text="{Binding Detail}" FontSize="12" Foreground="{ThemeResource TextFillColorSecondaryBrush}" TextTrimming="CharacterEllipsis"/>
            </StackPanel>
            <Border Grid.Column="2" Height="8" CornerRadius="4" VerticalAlignment="Center" Background="{ThemeResource ControlStrokeColorDefaultBrush}">
              <Rectangle HorizontalAlignment="Left" Width="{Binding Bar}" RadiusX="4" RadiusY="4" Fill="{ThemeResource AccentFillColorDefaultBrush}"/>
            </Border>
            <TextBlock Grid.Column="3" Text="{Binding Size}" HorizontalAlignment="Right" VerticalAlignment="Center"/>
          </Grid>
        </DataTemplate>
      </ListView.ItemTemplate>'''
TEMPLATE = '''<Page x:Class="CDClean.App.Views.NAME"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:CDClean.App">
  <Grid Padding="28,20" RowSpacing="12">
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    <StackPanel Spacing="4">
      <TextBlock Text="TITLE" Style="{StaticResource TitleTextBlockStyle}"/>
      <TextBlock Text="DESC" TextWrapping="Wrap" Foreground="{ThemeResource TextFillColorSecondaryBrush}"/>
    </StackPanel>
    <StackPanel Grid.Row="1" Orientation="Horizontal" Spacing="8">
TOOLBAR
    </StackPanel>
    <StackPanel Grid.Row="2" Spacing="4">
      <ProgressBar x:Name="Busy" IsIndeterminate="True" Visibility="Collapsed"/>
      <TextBlock x:Name="Status" TextWrapping="Wrap" IsTextSelectionEnabled="True"/>
    </StackPanel>
EXTRA
    <ListView Grid.Row="4" x:Name="List" SelectionMode="None">
ROW
    </ListView>
  </Grid>
</Page>
'''
USERS = "C:" + chr(92) + "Users"

def btn(name, text, accent=False):
    style = ' Style="{StaticResource AccentButtonStyle}"' if accent else ''
    return f'      <Button x:Name="{name}" Content="{text}" Click="{name}_Click"{style}/>'
def box(name, text, width=320, header=None):
    return f'      <TextBox x:Name="{name}" Text="{text}" Width="{width}" VerticalAlignment="Center"/>'
def num(name, value, header, mn=1, mx=1000):
    return f'      <NumberBox x:Name="{name}" Value="{value}" Header="{header}" Minimum="{mn}" Maximum="{mx}" SpinButtonPlacementMode="Compact" VerticalAlignment="Bottom"/>'

PAGES = {
 "ScanPage": ("Scan", "Measures every rule in rules.toml. Nothing is changed. Tick rules and press Clean selected to clean only those.",
   [btn("ScanBtn","Scan now",True), btn("CleanSelectedBtn","Clean selected"), btn("CancelBtn","Cancel")], "", True),
 "OrganizerPage": ("Organize folder", "Sorts the loose files in one folder into Images, Videos, Audio, Documents, Archives, Installers, Code and Other. Subfolders are not touched. Every move can be undone.",
   [box("Root","",320), btn("BrowseBtn","Browse"), btn("PreviewBtn","Preview",True), btn("ApplyBtn","Organize"), btn("UndoBtn","Undo last")], "", False),
 "BackupsPage": ("Backups", "Archive runs on your backup drive. Restore never overwrites a file that already exists.",
   [btn("LoadBtn","Refresh",True), btn("RestoreBtn","Restore ticked"), btn("DeleteBtn","Delete ticked"), num("KeepDays",30,"Keep days",1,3650), btn("PruneBtn","Prune older")], "", True),
}
for name,(title,desc,tools,extra,check) in PAGES.items():
    x = (TEMPLATE.replace("NAME",name).replace("TITLE",title).replace("DESC",desc)
         .replace("TOOLBAR","\n".join(tools)).replace("EXTRA",extra)
         .replace("ROW",ROW.replace("CHECKVIS","Visible" if check else "Collapsed")))
    open(os.path.join(OUT,name+".xaml"),"w",encoding="utf-8").write(x)
print("generated", len(PAGES))
