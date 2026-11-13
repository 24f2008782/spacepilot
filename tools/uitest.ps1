# UI checks for batches 1.2 and 1.3. Drives the real app with UI Automation.
# Default: BACKGROUND mode. The window is moved off-screen and no keyboard input is sent,
# so nothing steals focus while you are using the PC. Use -Foreground to also test keyboard steps.
# Only files this script creates under %TEMP%\cdclean-ui-test are ever removed (Large files -> Recycle).
param([string]$Exe = "publish\spacepilot\CDClean.exe", [switch]$Foreground)
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
$A = [System.Windows.Automation.AutomationElement]; $D = [System.Windows.Automation.TreeScope]::Descendants
$CT = [System.Windows.Automation.ControlType]
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int cx, int cy, uint f); [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd); [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r); [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr h); [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h); [DllImport("user32.dll")] public static extern bool IsZoomed(IntPtr h); public struct RECT { public int L,T,R,B; }' -Name UiBg -Namespace CDCleanUiTest
function Cond($prop, $v) { New-Object System.Windows.Automation.PropertyCondition($prop, $v) }
function FindInPopups($condition) {
    $appPid = $script:root.Current.ProcessId
    $main = $script:root.Current.NativeWindowHandle
    $wins = [System.Windows.Automation.AutomationElement]::RootElement.FindAll(
        [System.Windows.Automation.TreeScope]::Children, (Cond $A::ControlTypeProperty $CT::Window))
    foreach ($w in $wins) {
        if ($w.Current.ProcessId -ne $appPid -or $w.Current.NativeWindowHandle -eq $main) { continue }
        $e = $w.FindFirst($D, $condition)
        if ($e) { return $e }
    }
    $null
}
function Find($name) {
    $e = $script:root.FindFirst($D, (Cond $A::NameProperty $name))
    if ($e) { return $e }
    FindInPopups (Cond $A::NameProperty $name)
}
function FindId($id) {
    $e = $script:root.FindFirst($D, (Cond $A::AutomationIdProperty $id))
    if ($e) { return $e }
    FindInPopups (Cond $A::AutomationIdProperty $id)
}
function FindLike($prefix, $type = $CT::MenuItem) { $script:root.FindAll($D, (Cond $A::ControlTypeProperty $type)) | Where-Object { $_.Current.Name -like "$prefix*" } | Select-Object -First 1 }
function Wait($name, $sec = 20) { for ($i = 0; $i -lt $sec * 4; $i++) { $e = Find $name; if ($e) { return $e }; Start-Sleep -Milliseconds 250 }; $null }
function Click($e) { if (-not $e) { throw 'Click: element not found' }; $e.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke() }
function Toggle($e) { $e.GetCurrentPattern([System.Windows.Automation.TogglePattern]::Pattern).Toggle() }
function Nav($name) { (Find $name).GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select(); Start-Sleep 2 }
function SetText($id, $text) { (FindId $id).GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($text) }
function Rows { @($script:root.FindAll($D, (Cond $A::ControlTypeProperty $CT::DataItem))) }
function WaitRows($n, $sec = 10) { for ($i = 0; $i -lt $sec * 4; $i++) { $r = @(Rows); if ($r.Count -eq $n) { return $r }; Start-Sleep -Milliseconds 250 }; @(Rows) }
# ContentDialog is a Popup window nested under the main window's first child. Walking that short
# path avoids enumerating a huge preview table just to find the dialog's buttons.
function FindDialog($name) {
    $e = $script:root
    for ($i = 0; $i -lt 4 -and $e; $i++) {
        $win = $e.FindFirst([System.Windows.Automation.TreeScope]::Children,
            (New-Object System.Windows.Automation.AndCondition((Cond $A::NameProperty $name), (Cond $A::ControlTypeProperty $CT::Window))))
        if ($win) { return $win }
        $e = $e.FindFirst([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.Condition]::TrueCondition)
    }
    $null
}
function WaitDialog($name, $sec = 20) { for ($i = 0; $i -lt $sec * 4; $i++) { $e = FindDialog $name; if ($e) { return $e }; Start-Sleep -Milliseconds 250 }; $null }
function Check($ok, $msg) { if ($ok) { "PASS  $msg" } else { "FAIL  $msg"; $script:fail++ } }
function Skip($msg) { "SKIP  $msg" }
$fail = 0
$scale = 1.0
$X0 = if ($Foreground) { 50 } else { -32000 }   # background: window parked off-screen
$Y0 = if ($Foreground) { 50 } else { -32000 }
$SWP_NOZ_NOACT = 0x14                             # no z-order change, no activation

$dir = Join-Path $env:TEMP 'cdclean-ui-test'
Remove-Item $dir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory $dir -Force | Out-Null
foreach ($n in 'big-a.bin', 'big-b.bin') { $f = [IO.File]::Create("$dir\$n"); $f.SetLength(3MB); $f.Close() }

$p = Start-Process $Exe -PassThru
for ($t = 0; $t -lt 60 -and $p.MainWindowHandle -eq 0; $t++) { Start-Sleep -Milliseconds 500; $p.Refresh() }
Start-Sleep 2
$h = $p.MainWindowHandle
$scale = [CDCleanUiTest.UiBg]::GetDpiForWindow($h) / 96.0
[CDCleanUiTest.UiBg]::SetWindowPos($h, [IntPtr]::Zero, $X0, $Y0, [int](1240 * $scale), [int](840 * $scale), $SWP_NOZ_NOACT) | Out-Null
$root = $A::FromHandle($h)

# 1. Admin review: Run is disabled until the checkbox; irreversible needs the typed word.
Nav 'System (admin)'
$runs = $root.FindAll($D, (New-Object System.Windows.Automation.AndCondition((Cond $A::NameProperty 'Run'), (Cond $A::ControlTypeProperty $CT::Button))))
Click $runs[0]; $prim = Wait 'Run and measure'
Check ($prim -and -not $prim.Current.IsEnabled) 'Trim WinSxS review opens with Run disabled'
(Find 'Exact commands').GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand()
$cmd = $null
for ($i = 0; $i -lt 20 -and -not $cmd; $i++) { $cmd = Find 'dism.exe /Online /Cleanup-Image /StartComponentCleanup'; if (-not $cmd) { Start-Sleep -Milliseconds 250 } }
Check ([bool]$cmd) 'review lists the exact DISM command'
$cb = $root.FindAll($D, (Cond $A::ControlTypeProperty $CT::CheckBox)) | Where-Object { $_.Current.Name -like 'I understand*' } | Select-Object -First 1
Toggle $cb; Start-Sleep -Milliseconds 300
Check ((Find 'Run and measure').Current.IsEnabled) 'ticking "I understand" enables Run'
Click (Find 'Cancel'); Start-Sleep 1
Click $runs[1]; Start-Sleep 1
$box = $root.FindAll($D, (Cond $A::ControlTypeProperty $CT::Edit)) | Where-Object { $_.Current.Name -eq 'RESETBASE' } | Select-Object -First 1
if (-not $box) { $box = $root.FindAll($D, (Cond $A::ControlTypeProperty $CT::Edit)) | Where-Object { -not $_.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.IsReadOnly } | Select-Object -Last 1 }
$box.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('resetbase'); Start-Sleep -Milliseconds 500
Check (-not (Find 'Run and measure').Current.IsEnabled) 'ResetBase: wrong word keeps Run disabled'
$box.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('RESETBASE')
$prim = Wait 'Run and measure' 5
for ($i = 0; $i -lt 20 -and $prim -and -not $prim.Current.IsEnabled; $i++) { Start-Sleep -Milliseconds 250 }
Check ($prim -and $prim.Current.IsEnabled) 'ResetBase: typing RESETBASE enables Run'
Click (Find 'Cancel'); Start-Sleep 1

# 2. Clean preview: rows appear, nothing removed; the review shows a counted button and the "will not touch" list.
Nav 'Clean'
$statusEl = FindId 'Status'
$cleanBtn = Find 'Clean ticked'
$selEl = FindId 'SelText'
Click (Find 'Preview')
# Large real cache sets can take minutes to plan; 600s leaves room without hiding a hang.
for ($i = 0; $i -lt 1200 -and -not ($statusEl.Current.Name -match 'Nothing has been changed|failed'); $i++) { Start-Sleep -Milliseconds 500 }
$status = $statusEl.Current.Name
Check ($status -match 'Nothing has been changed') "preview finished: $status"
if ($status -match 'Nothing has been changed') {
    $n = ($selEl.Current.Name -split ' ')[0]
    Click $cleanBtn
    $dlg = WaitDialog 'Clean these files?' 90
    $prim = $null
    if ($dlg) {
        for ($i = 0; $i -lt 240 -and -not $prim; $i++) {
            # The review re-plans, so a few files can disappear between preview and review; match the counted button, not the exact preview count.
            $prim = $dlg.FindAll($D, (Cond $A::ControlTypeProperty $CT::Button)) | Where-Object { $_.Current.Name -match '^Clean [\d,]+ items$' } | Select-Object -First 1
            if (-not $prim) { Start-Sleep -Milliseconds 250 }
        }
    }
    Check ([bool]$prim) "clean review shows the counted button '$(if ($prim) { $prim.Current.Name } else { "Clean $n items (not found)" })'"
    $touch = $dlg.FindFirst($D, (Cond $A::NameProperty 'Will not touch: Windows, WindowsApps, System Volume Information, your protected folders, the backup folder, cookies, passwords and browser history.'))
    Check ([bool]$touch) 'clean review lists what it will not touch'
    if ($dlg) { $cancel = $dlg.FindFirst($D, (Cond $A::NameProperty 'Cancel')); if ($cancel) { Click $cancel } }
    Start-Sleep 1
} else { Skip 'clean review checks (preview did not finish)' }

# 3. Large files: a real Recycle of the two test files, through the review.
Nav 'Large files'
SetText 'Root' $dir
(FindId 'MinMb').GetCurrentPattern([System.Windows.Automation.RangeValuePattern]::Pattern).SetValue(1)
Click (Find 'Find'); Start-Sleep 3
Click (Find 'Select visible'); Start-Sleep 1
Check ((FindId 'SelText').Current.Name -match '^2 selected') ("two test files selected: " + (FindId 'SelText').Current.Name)
Click (Find 'Recycle')
$recycleBtn = Wait 'Recycle 2 items' 30
$ackBox = $root.FindAll($D, (Cond $A::ControlTypeProperty $CT::CheckBox)) | Where-Object { $_.Current.Name -eq 'I have reviewed the items with warnings' } | Select-Object -First 1
Check ($recycleBtn -and -not $recycleBtn.Current.IsEnabled) 'recycle button waits for the review tick (files are new, so they carry a warning)'
if ($ackBox) { Toggle $ackBox; Start-Sleep -Milliseconds 300 }
if ($recycleBtn) { Click $recycleBtn }
Start-Sleep 4
Check (-not (Test-Path "$dir\big-a.bin") -and -not (Test-Path "$dir\big-b.bin")) 'both test files left the folder'
Check ((FindId 'Status').Current.Name -match '2 done') ("status: " + (FindId 'Status').Current.Name)

# 4. Filters, row menu, lock check (read-only).
foreach ($f in @(@{ N = 'big-c.bin'; S = 3MB }, @{ N = 'small-d.bin'; S = 1.5MB })) { $x = [IO.File]::Create("$dir\$($f.N)"); $x.SetLength($f.S); $x.Close() }
Click (Find 'Find')
$listed = WaitRows 2 15
Check ($listed.Count -eq 2) "two test files listed ($($listed.Count))"
Click (Find 'Filters'); Start-Sleep 1
(FindId 'MinSizeBox').GetCurrentPattern([System.Windows.Automation.RangeValuePattern]::Pattern).SetValue(2)
Click (Find 'Apply')
$filtered = WaitRows 1 10
Check ($filtered.Count -eq 1) "size filter 2 MB leaves one row ($($filtered.Count))"
$chip = $root.FindAll($D, (Cond $A::ControlTypeProperty $CT::Button)) | Where-Object { $_.Current.Name -like 'Size*2 MB*' } | Select-Object -First 1
Check ([bool]$chip) 'filter shows a removable chip'
Click $chip
$allRows = WaitRows 2 10
Check ($allRows.Count -eq 2) 'removing the chip shows both rows again'
if ($Foreground) {
    $row = (Rows)[0]
    $row.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select(); Start-Sleep -Milliseconds 500
    (New-Object -ComObject WScript.Shell).SendKeys('+{F10}'); Start-Sleep 1
    Check ([bool](Wait 'Show in File Explorer' 5)) 'Shift+F10 opens the row menu'
    Check ([bool](FindLike 'Move to Recycle Bin') -and [bool](FindLike 'Check if in use') -and [bool](FindLike 'Copy') -and [bool](FindLike 'Properties')) 'menu has recycle, in-use check, copy and properties'
    Click (FindLike 'Check if in use'); Start-Sleep 2
    Check ([bool]($root.FindAll($D, (Cond $A::ControlTypeProperty $CT::Text)) | Where-Object { $_.Current.Name -match 'Not in use' })) 'in-use check reports "Not in use"'
    Click (Find 'OK'); Start-Sleep 1
} else {
    Skip 'Shift+F10 row menu and its items (needs -Foreground; keyboard input would take focus)'
}

# 5. Dashboard drive switch (no keyboard needed).
Nav 'Dashboard'
$combo = FindId 'DrivePick'
$combo.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand(); Start-Sleep 1
$eItem = $root.FindAll($D, (Cond $A::ControlTypeProperty $CT::ListItem)) | Where-Object { $_.Current.Name -like 'E:*' } | Select-Object -First 1
if ($eItem) { $eItem.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select(); Start-Sleep 1 } else { Skip 'no E: drive listed' }
Check ((FindId 'FreeLabel').Current.Name -eq 'Free on E:' -or -not $eItem) ("dashboard switched drive: " + (FindId 'FreeLabel').Current.Name)

# 5b. Apps inventory: source provenance and a readable list (read-only).
Nav 'Apps'
$appsStatus = $null
for ($i = 0; $i -lt 40; $i++) {
    $s = FindId 'Status'
    if ($s -and $s.Current.Name -match 'programs') { $appsStatus = $s.Current.Name; break }
    Start-Sleep -Milliseconds 500
}
Check ([bool]$appsStatus) ("apps inventory loaded: " + $appsStatus)
Check ($appsStatus -match 'Registry|MSI|MSIX') 'apps status names the inventory sources'
Check ([bool](Find 'Source')) 'apps grid has a Source column'
Check ((Rows).Count -gt 0) "apps grid has rows ($((Rows).Count))"

# 5c. Settings search, category navigation, and the typed ADVANCED unlock (read-only).
Nav 'Settings'
SetText 'SearchBox' 'vault'
Start-Sleep 1
$searchHit = $root.FindAll($D, (Cond $A::ControlTypeProperty $CT::ListItem)) | Where-Object { $_.Current.Name -like '*Vault size cap*' } | Select-Object -First 1
Check ([bool]$searchHit) 'Settings search finds Vault size cap'
if ($searchHit) { $searchHit.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select(); Start-Sleep 1 }
Check ([bool]($root.FindAll($D, (Cond $A::ControlTypeProperty $CT::Text)) | Where-Object { $_.Current.Name -eq 'Backups & recovery' })) 'Settings search result opens Backups & recovery'
SetText 'SearchBox' ''
Start-Sleep 1

$advItem = $root.FindAll($D, (Cond $A::ControlTypeProperty $CT::ListItem)) | Where-Object { $_.Current.Name -eq 'Advanced' } | Select-Object -First 1
if ($advItem) { $advItem.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select(); Start-Sleep 1 }
$unlockBox = $root.FindAll($D, (Cond $A::ControlTypeProperty $CT::Edit)) | Where-Object { $_.Current.Name -eq 'Type ADVANCED' } | Select-Object -First 1
Check ([bool]$unlockBox) 'Settings Advanced shows the typed unlock box'
if ($unlockBox) {
    $unlockBox.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('WRONG'); Start-Sleep -Milliseconds 300
    $unlockBtn = $root.FindAll($D, (New-Object System.Windows.Automation.AndCondition((Cond $A::NameProperty 'Unlock'), (Cond $A::ControlTypeProperty $CT::Button)))) | Select-Object -First 1
    Check ($unlockBtn -and -not $unlockBtn.Current.IsEnabled) 'wrong word keeps Advanced locked'
    $unlockBox.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('ADVANCED'); Start-Sleep -Milliseconds 300
    Check ($unlockBtn -and $unlockBtn.Current.IsEnabled) 'typing ADVANCED enables Unlock'
    if ($unlockBtn) { Click $unlockBtn; Start-Sleep 1 }
    Check ([bool]($root.FindAll($D, (Cond $A::ControlTypeProperty $CT::Text)) | Where-Object { $_.Current.Name -eq 'Unlocked for this app session.' })) 'Advanced settings are unlocked for this session'
}

# 5d. Settings About: version text and the notices button (the button is not a text element).
$aboutItem = $root.FindAll($D, (Cond $A::ControlTypeProperty $CT::ListItem)) | Where-Object { $_.Current.Name -eq 'About & updates' } | Select-Object -First 1
if (-not $aboutItem) { $aboutItem = Find 'About & updates' }
if ($aboutItem) { $aboutItem.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select(); Start-Sleep 1 }
$aboutText = $root.FindAll($D, (Cond $A::ControlTypeProperty $CT::Text)) | Where-Object { $_.Current.Name -match '^SpacePilot \d' } | Select-Object -First 1
Check ([bool]$aboutText) ("Settings About shows the version: " + $(if ($aboutText) { $aboutText.Current.Name.Split("`n")[0] } else { 'missing' }))
$notices = $root.FindAll($D, (New-Object System.Windows.Automation.AndCondition((Cond $A::NameProperty 'Third-party notices'), (Cond $A::ControlTypeProperty $CT::Button)))) | Select-Object -First 1
Check ([bool]$notices) 'Settings About exposes Third-party notices as a button'

# 6. Window: minimum size and collapsed sidebar (moved off-screen in background mode).
# Restore first: a window parked off-screen can be minimized by the OS, which makes a size check meaningless.
[CDCleanUiTest.UiBg]::ShowWindow($h, 4) | Out-Null; Start-Sleep 1
[CDCleanUiTest.UiBg]::SetWindowPos($h, [IntPtr]::Zero, $X0, $Y0, 500, 400, $SWP_NOZ_NOACT) | Out-Null; Start-Sleep 2
$r = New-Object CDCleanUiTest.UiBg+RECT; [CDCleanUiTest.UiBg]::GetWindowRect($h, [ref]$r) | Out-Null
$state = "h=$h main=$($p.MainWindowHandle) iconic=$([CDCleanUiTest.UiBg]::IsIconic($h)) zoomed=$([CDCleanUiTest.UiBg]::IsZoomed($h)) title=[$($p.MainWindowTitle)]"
Check (($r.R - $r.L) -ge [int](1024 * $scale) - 2 -and ($r.B - $r.T) -ge [int](640 * $scale) - 2) "window refuses to shrink below 1024x640 (now $($r.R - $r.L)x$($r.B - $r.T) at scale $scale; $state)"
[CDCleanUiTest.UiBg]::SetWindowPos($h, [IntPtr]::Zero, $X0, $Y0, [int](1060 * $scale), [int](760 * $scale), $SWP_NOZ_NOACT) | Out-Null; Start-Sleep 2
$dash = Find 'Dashboard'
Check ($dash.Current.BoundingRectangle.Width -lt 100 * $scale) "sidebar collapses to icons below 1100 px (Dashboard item width $([int]$dash.Current.BoundingRectangle.Width))"
[CDCleanUiTest.UiBg]::SetWindowPos($h, [IntPtr]::Zero, $X0, $Y0, [int](1240 * $scale), [int](840 * $scale), $SWP_NOZ_NOACT) | Out-Null; Start-Sleep 1

$p.Refresh(); Check (-not $p.HasExited) 'app still running'
if (-not $p.HasExited) { Stop-Process $p -Force }
Remove-Item $dir -Recurse -Force -ErrorAction SilentlyContinue
if ($fail) { "$fail FAILED"; exit 1 } else { "ALL UI CHECKS PASSED" }
