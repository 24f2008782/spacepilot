# Smoke test: starts CDClean, clicks every sidebar page, checks the app is still alive after each.
param([string]$Exe)
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
$p = Start-Process $Exe -PassThru
# Wait for the window (first start after a publish can take 10+ seconds).
for ($t = 0; $t -lt 60 -and -not $p.HasExited -and $p.MainWindowHandle -eq 0; $t++) { Start-Sleep -Milliseconds 500; $p.Refresh() }
# Background by default: park the window off-screen without activating it.
Add-Type -MemberDefinition '[DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr a, int x, int y, int cx, int cy, uint f);' -Name SmokeBg -Namespace CDCleanSmoke
if ($p.MainWindowHandle -ne 0) { [CDCleanSmoke.SmokeBg]::SetWindowPos($p.MainWindowHandle, [IntPtr]::Zero, -32000, -32000, 1240, 840, 0x14) | Out-Null }
Start-Sleep 2
$p.Refresh()
if ($p.HasExited) { "FAIL: exited at start (code $($p.ExitCode))"; exit 1 }
$root = [System.Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
$pages = 'Scan','Clean','System (admin)','Apps','Leftovers','Old files','Large files','Duplicates','Organize folder','Backups','Schedule','Rules','Settings','Dashboard','Disk map'
$fail = 0
foreach ($name in $pages) {
    $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $name)
    $item = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
    if (-not $item) { "MISSING: $name"; $fail++; continue }
    try {
        $sel = $item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern); $sel.Select()
    } catch {
        try { $item.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke() } catch { "NO PATTERN: $name"; $fail++; continue }
    }
    Start-Sleep -Milliseconds 2500
    $p.Refresh()
    if ($p.HasExited) { "CRASH on: $name (code $($p.ExitCode))"; exit 1 }
    "ok: $name"
}
Start-Sleep 3
$p.Refresh()
if ($p.HasExited) { "CRASH after the last page (code $($p.ExitCode)). See %LOCALAPPDATA%\CDClean\logs\crash.log"; exit 1 }
Stop-Process $p -Force
if ($fail) { exit 1 }
"ALL PAGES OPENED"
