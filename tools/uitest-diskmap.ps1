# UI checks for batch 2.2 (Disk map). Drives the real app with UI Automation.
# BACKGROUND only: the window is parked off-screen and no keyboard input is sent, so nothing steals focus.
# Keyboard-only steps (Alt+Up, double-click zoom, Shift+F10 menu) are not run here; see the playbook for what is unverified.
# Scans only the fixture folder under %TEMP%\cdclean-ui-test\diskmap. Never scans a whole drive.
param([string]$Exe = "publish\spacepilot\CDClean.exe")
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
$A = [System.Windows.Automation.AutomationElement]; $D = [System.Windows.Automation.TreeScope]::Descendants
$CT = [System.Windows.Automation.ControlType]
$ALL = [System.Windows.Automation.Condition]::TrueCondition
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int cx, int cy, uint f);' -Name DmBg -Namespace CDCleanDiskMapTest

function Cond($prop, $v) { New-Object System.Windows.Automation.PropertyCondition($prop, $v) }
function Find($name) { $script:root.FindFirst($D, (Cond $A::NameProperty $name)) }
function FindId($id) { $script:root.FindFirst($D, (Cond $A::AutomationIdProperty $id)) }
function FindType($type, $pred) { @($script:root.FindAll($D, (Cond $A::ControlTypeProperty $type)) | Where-Object $pred) }
function AllNamed($pred) { @($script:root.FindAll($D, $ALL) | Where-Object { $_.Current.Name -and (& $pred $_.Current.Name) }) }
function SelectItem($e) { $e.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select(); Start-Sleep -Milliseconds 800 }
function Invoke($e) { $e.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke(); Start-Sleep -Milliseconds 900 }
function SetText($e, $text) { $e.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($text); Start-Sleep -Milliseconds 700 }
function Nav($name) { SelectItem (Find $name); Start-Sleep 1 }
function WaitFor($pred, $sec = 60) { for ($i = 0; $i -lt $sec * 4; $i++) { $r = & $pred; if ($r) { return $r }; Start-Sleep -Milliseconds 250 }; $null }
function TreemapTiles { (AllNamed { param($n) $n -like 'Treemap, *' } | Select-Object -First 1).Current.Name }
function StatusText { (FindType $CT::Text { $_.Current.Name -like '*in scanned files*' } | Select-Object -First 1).Current.Name }
# Tree rows carry the folder name as the item's own automation name (the tree exposes no Text children).
function TreeRowNamed($name) {
    foreach ($item in @($script:root.FindAll($D, (Cond $A::ControlTypeProperty $CT::TreeItem)))) {
        if ($item.Current.Name -eq $name) { return $item }
    }
    $null
}
function Check($ok, $msg) { if ($ok) { "PASS  $msg" } else { "FAIL  $msg"; $script:fail++ } }
$fail = 0

# Fixture: known sizes, a nested folder, a hard link, a junction, and an ACL-denied folder.
$base = Join-Path $env:TEMP 'cdclean-ui-test\diskmap'
if (Test-Path $base) {
    Get-ChildItem $base -Force -ErrorAction SilentlyContinue | Where-Object { $_.Attributes -match 'ReparsePoint' } | ForEach-Object { cmd /c "rmdir `"$($_.FullName)`"" }
    icacls "$base\denied" /reset 2>$null | Out-Null
    Remove-Item $base -Recurse -Force
}
New-Item -ItemType Directory -Path "$base\sub\deep", "$base\sub\nested\x", "$base\denied", "$base\outside" -Force | Out-Null
[IO.File]::WriteAllBytes("$base\a.bin", (New-Object byte[] 1048576))
[IO.File]::WriteAllBytes("$base\sub\b.bin", (New-Object byte[] 307200))
[IO.File]::WriteAllBytes("$base\sub\deep\c.bin", (New-Object byte[] 102400))
[IO.File]::WriteAllBytes("$base\sub\nested\x\y.bin", (New-Object byte[] 51200))
[IO.File]::WriteAllBytes("$base\outside\big.bin", (New-Object byte[] 2097152))
New-Item -ItemType HardLink -Path "$base\sub\h.bin" -Target "$base\a.bin" | Out-Null
cmd /c "mklink /J `"$base\j`" `"$base\outside`"" | Out-Null
[IO.File]::WriteAllBytes("$base\denied\secret.bin", (New-Object byte[] 4096))
icacls "$base\denied" /deny "$($env:USERNAME):(RX)" | Out-Null

$crashLog = Join-Path $env:LOCALAPPDATA "CDClean\logs\crash.log"
$crashBefore = if (Test-Path $crashLog) { (Get-Item $crashLog).Length } else { -1 }

$p = Start-Process $Exe -PassThru
for ($t = 0; $t -lt 60 -and $p.MainWindowHandle -eq 0; $t++) { Start-Sleep -Milliseconds 500; $p.Refresh() }
Start-Sleep 2
$h = $p.MainWindowHandle
[CDCleanDiskMapTest.DmBg]::SetWindowPos($h, [IntPtr]::Zero, -32000, -32000, 1240, 840, 0x14) | Out-Null
$root = $A::FromHandle($h)

Nav 'Disk map'
Check ([bool](FindId 'PathBox')) 'Disk map page opens'
SetText (FindId 'PathBox') $base
Invoke (Find 'Scan path')
$status = WaitFor { StatusText }
Check ([bool]$status) "scan finishes and reports totals: $status"
Check ($status -like '*6 files*') 'file count is 6 (a.bin, b.bin, h.bin, big.bin, c.bin, y.bin)'
Check ($status -like '*could not be read*') 'denied folder is reported as unreadable'

# Root view: a.bin, sub and outside are drawn; the junction and the empty denied folder are not.
$tiles = TreemapTiles
Check ($tiles -eq 'Treemap, 3 tiles') "root view draws 3 tiles (got: $tiles)"

# Select sub in the tree: the map zooms in and the breadcrumb shows it.
$subRow = TreeRowNamed 'sub'
if ($subRow) { SelectItem $subRow } else { Check $false 'tree row for sub exists' }
Check ([bool](FindType $CT::Hyperlink { $_.Current.Name -eq 'sub' } | Select-Object -First 1)) 'breadcrumb shows sub after selecting it'
Check ((TreemapTiles) -eq 'Treemap, 3 tiles') "inside sub the map draws 3 tiles, b.bin, deep and nested (got: $(TreemapTiles))"
Check ([bool](AllNamed { param($n) $n -like '*\sub*' -and $n -like '*452 KB*' } | Select-Object -First 1)) 'selection bar shows the selected folder and its size'

# Size mode: files-only must change the map, so use a folder whose total differs from its direct files.
$mode = FindId 'SizeMode'; if (-not $mode) { $mode = Find 'Size shown' }
if ($mode) {
    $toggle = $mode.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern)
    $toggle.Toggle(); Start-Sleep -Milliseconds 800
    Check ((TreemapTiles) -eq 'Treemap, 2 tiles') "files-only inside sub draws 2 tiles, b.bin and deep (got: $(TreemapTiles))"
    $toggle.Toggle(); Start-Sleep -Milliseconds 800
    Check ((TreemapTiles) -eq 'Treemap, 3 tiles') "switching back to total restores 3 tiles (got: $(TreemapTiles))"
} else { Check $false 'size mode toggle exists' }

# Back to the root through the breadcrumb.
$crumbRoot = FindType $CT::Hyperlink { $_.Current.Name -eq $base } | Select-Object -First 1
if ($crumbRoot) { Invoke $crumbRoot; Check ((TreemapTiles) -eq 'Treemap, 3 tiles') 'breadcrumb returns to the root view' } else { Check $false 'root breadcrumb exists' }

# Tabs.
SelectItem (FindType $CT::TabItem { $_.Current.Name -eq 'Largest files' } | Select-Object -First 1)
Check ([bool](Find 'big.bin')) 'Largest files lists big.bin'

# Find box filters the list.
SetText (FindId 'FindBox') 'a.bin'
Start-Sleep -Milliseconds 700
Check ([bool](Find 'a.bin')) 'find box keeps a.bin'
Check (-not (Find 'big.bin')) 'find box hides big.bin'
SetText (FindId 'FindBox') ''

SelectItem (FindType $CT::TabItem { $_.Current.Name -eq 'Unreadable' } | Select-Object -First 1)
Check ([bool](Find 'denied')) 'Unreadable lists the denied folder'

SelectItem (FindType $CT::TabItem { $_.Current.Name -eq 'Ledger' } | Select-Object -First 1)
Check ([bool](Find 'Used on the drive (as Windows reports it)')) 'Ledger shows the used-on-drive line'
Check ([bool](Find 'Not attributed')) 'Ledger shows the Not attributed line'

# Export: CSV and JSON are covered by Core tests. PNG and file dialogs are not automated (they open on screen).

Check (-not $p.HasExited) ("app still running after all checks (exited=$($p.HasExited))")
$crashAfter = if (Test-Path $crashLog) { (Get-Item $crashLog).Length } else { -1 }
Check ($crashAfter -eq $crashBefore) 'no new crash.log entries'
if (-not $p.HasExited) { Stop-Process $p -Force }
cmd /c "rmdir `"$base\j`"" 2>$null | Out-Null   # remove the junction first, so the recursive delete never follows it
icacls "$base\denied" /reset 2>$null | Out-Null # drop the deny ACE, or the recursive delete cannot enter the folder
Remove-Item $base -Recurse -Force -ErrorAction SilentlyContinue
if ($fail) { "$fail FAILED"; exit 1 } else { "ALL DISK MAP UI CHECKS PASSED" }
