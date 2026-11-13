# SpacePilot 1.5.3 — Manual Review Guide

**Purpose:** Walk through every screen read-only on your E: drive. Do **not** delete, archive, recycle, uninstall, or run any admin task. This guide is only for scanning and observing.

**App binary:** `publish\spacepilot\CDClean.exe`
**Version:** 1.5.3 (window title "SpacePilot 1.5.3")
**Data folder:** `%LOCALAPPDATA%\CDClean` (settings, logs, audit)
**No data is changed by this guide.**

Tick each box in Obsidian as you complete it. Write notes under each screen if something is broken.

---

## Pre-flight

- [x] Close other heavy apps (browsers, games) to keep RAM free; the app targets 250–300 MB during scans.
- [x] Make sure E: drive is connected and readable.
- [x] Launch `CDClean.exe`. Window title should read "SpacePilot 1.5.3".
- [x] Window should be at least 1024×640; sidebar collapses to icons below 1100 px width.

---

## 1. Dashboard
**What to do:** Observe the drive cards. Switch the drive picker to E:.

- [x] Drive list shows E: with total/free space and file system.
- [x] Selecting E: updates "Free on E:" label.
- [ ] No crash, no blank cards.

**Risk:** Dashboard numbers are estimates from `DriveInfo`; if E: is a USB/removable drive, scan warnings apply later.

---

## 2. Disk Map (scan E: — read only)
**What to do:** Pick E: in the drive dropdown → click "Scan drive". Wait for completion. Do **not** click any delete/export except CSV/JSON/PNG if you want (those are read-only exports).

- [x] Scan completes without crash; status line shows size, file count, time.
- [x] Treemap renders tiles; double-click zooms into a folder; Alt+Up zooms out.
- [x] Tree (left) expands folders; selection syncs between tree and map.
- [x] "Files only" toggle changes tile sizes.
- [x] Bottom tabs: Largest files (filter box works), File types, Unreadable, Ledger.
- [x] Ledger shows "Used on the drive", "Files and folders on disk", "Not attributed" lines.
- [x] Unreadable tab lists any folders that could not be read.
- [ ] Export CSV/JSON/PNG saves a file and opens its folder (optional).
- [x] "Fast scan (admin, MFT)" button: if not elevated, shows a message; if elevated, scans via MFT. **Do not run elevated unless you intend to** — it requires admin.

**Risks:**
- Whole-volume scan of E: may take 10–60 s depending on size and drive type.
- MFT fast scan requires admin (UAC) and is unverified on this build; an unsigned MFT reader may be flagged by antivirus.
- Map is capped at ~1.75 M nodes on 4 GB PCs / ~2.1 M on 8 GB+; larger drives show "Map is partial".

changes
1. For the disk map screen specifically:

- Fast scan works and the search works. Double-clicking on the colored area on the right side works. On the left side, I can't see the folder name itself. Please check that because that's an issue here and fix it.
- The Export Map as PNG option is not exporting in PNG. The second export is not working at all. Fix that.
- The bottom tabs are working and everything on them is working.
- The ledger view is not perfect. Unreadable is perfect and export is not working. Please check that fast scan works. I restarted it and it actually works pretty well.
- For large files, allow me to enter what should be the minimum baseline for it.
- There seems to be a small filter column, or the entire filter line, from the drive selection dropdown to the export dropdown button. Can you get it on a single line or on two lines if the screen is small?
- For the tree map (the disk map you're showing), make sure to show every single thing inside each folder if possible. Without double-clicking, it should show everything. If I double-click on it, show me only that specific folder.

These are the changes I need in the disk map.

 Also check that the scanning disk map is using more than 300 MB of RAM, 30%, 40%, or 50% CPU, and very high power. See how you can control and minimize this as well please. Thank you.
 
---

## 3. Clean
**What to do:** Set Root to a disposable subfolder (for example, a folder under `%TEMP%`). Click "Preview" only. Do **not** click "Clean ticked".

- [x] Preview finishes; status says "Nothing has been changed".
- [x] Review dialog shows counted button and "Will not touch: Windows, WindowsApps, ..." list.
- [x] Cancel closes the dialog; nothing is removed.
- [x] Large files / Old files / Duplicates pages load and list files when you click "Find" with a small min size.

**Risks:** Preview is read-only, but "Clean ticked" / Recycle / Archive are destructive — skip them for this review.

Check that the Clean Screen menu is working perfectly and that there is no issue.

Explain why it would say, "If I click on Clean Tick, will it open a modal?" Do not click on Clean Tick. It does open a modal when I click on Clean Tick for the Clean Screen, so we can avoid that.

When I click on Clean Tick, make sure you do not directly delete it. Put it in the Recycle Bin. We will try not to delete anything permanently unless we are asking the modal. If it is deleted permanently, we will show it in red in the modal so the user knows it is being permanently deleted. Otherwise, mostly everywhere, we will try to put it in the Recycle Bin so the user will manually go there and delete it. Remember that.

For the system cleanup, I have not done system cleanup. 

---

## 4. System (admin) — read only
**What to do:** Open the page. Do **not** click "Run" on any task. Click "Run maintenance analysis (read-only)".

- [x] Task cards render with title, gain, undo.
- [x] Clicking "Run" opens a review dialog with "Run disabled until checkbox"; cancel it (do not run).
- [ ] "Run maintenance analysis" runs DISM/vssadmin and shows WinSxS, Windows.old, shadow storage, WU cache sizes. May take 30–60 s.
- [ ] Log box and progress bar behave (no hang).

**Risks:** Maintenance analysis shells out to `dism.exe` and `vssadmin.exe` (read-only) but can take a minute. Admin tasks are destructive — do not run them during this review.

System admin: I have not run anything because I do not want to check anything. I'll just open one model for the system admin.

Trim WSL shows the recommended command, the copy command, and everything. That's perfect and this works exactly like I thought. If I click Skip or Cancel, it comes up. That's perfect.

Run maintenance analysis: I clicked on it, and it says, "This can take a minute." Instead of "take a minute," it can show an estimated time in reverse so the user knows what's happening. While this is running, show that it is actually running on the right side, not the "No task running" space. Show the analytics with the log that's running on the right side so the user knows what's happening, with cleaner logs.

For all the other system cleanups, also show proper logs on the right side, not just in the running spaces there.

Run maintenance is checking longer than I thought. It has been almost a minute and it has shown me everything, which is pretty good. For the log you're showing, make sure to display it in the "No task running" space on the right side. Thank you. 

---

## 5. Apps
**What to do:** Let inventory load. Observe counts. Click a row to see the detail pane. Do **not** click "Uninstall".

- [x] Status line lists program count and sources (MSI, Registry 64/32-bit, Registry (user), MSIX, Steam, winget).
- [x] Source column present and populated.
- [x] Grid has rows (>0).
- [x] Selecting a row updates the detail pane: name, source, version, publisher, install location, metadata key, product code / winget id, broken state, running processes, pending reboot.
- [x] "Open folder" opens Explorer (optional, read-only).
- [x] Filters (drive) narrow the list.

**Risks:** Inventory shells out to `winget export` (15 s cap) on load — may delay first load. MSIX inventory uses `PackageManager` and may be slow on machines with many packages.


The app list is taking quite a long time to load, around 10 to 15 seconds and sometimes up to 20 seconds. Check how you can optimize this while using less RAM and tell me about that.

The grid is pretty clean. For the default apps in the application, it is not showing the size. Can you try to discover how they store this, do research on it, and get the size if possible? This is for the app screen 


---

## 6. Leftovers
**What to do:** Pick "System drive only" → click "Find". Observe. Do **not** click Recycle/Archive/Keep.

- [x] Unmatched folders listed with size, file count, risk.
- [x] Status line shows count and roots scanned.
- [x] Row double-click opens the folder in Explorer (read-only).

**Risks:** Leftovers are *candidates*, not confirmed junk — some belong to apps not in the registry. Never bulk-remove without review.

For the leftovers, it's pretty fine. It works as it is with no issues, I would say.

- For the grid in the leftovers, try to auto-adjust it so it fits inside the top-to-bottom scroller. We'll try to avoid that.
- For leftover scanning, make sure to mention how long it will take. While any scanning is happening, show a toast message or a message somewhere below so the user knows it will take 10 to 15 seconds to scan and process everything cleanly. This way the user knows what is actually happening.
- If I double-click on it, it opens the explorer. That's perfect.
- If I right-click on it, your right-click looks very good. The right-click menu is the best I've seen right now. It's very good and I actually love it. Thank you. The leftovers are perfect.
- For the old files, if I click on Find, make sure to show a modal or a toast notification in the top right for at least 10 seconds. It should include a backward-counting timer animation and a line animation, and say that it will take an estimated amount of time to complete scanning. Wherever there is scanning, whether for the leftovers, the old files, or duplicates, we will do this as well.

Thank you.

---

## 7. Old files / Large files / Duplicates
**What to do:** Set Root to an E: subfolder. Click "Find" with a small threshold. Observe only.

- [x] Lists populate with size, count, modified date.
- [x] Filters and chips work.
- [x] Do **not** click Recycle/Archive.

**Risks:** Read-only listing; destructive actions are gated behind the review dialog, but skip them for this review.

---

## 8. Organize folder
**What to do:** Set Root to a test folder on E:. Click "Preview". Observe. Do **not** click "Organize".

- [x] Preview shows planned moves grouped by category.
- [x] Nothing is moved.

The organized folders I have are fine, and finding large files is also fine. Just make sure torsional friction is totaled and that duplicates are also totaled.  
  
For the organized folder, let me see a preview of it. Can you split this organized folder into two parts?

- On the left side, show where the actual files will go.
- On the right side, show the summary: which documents will have so many files and which will have so many files.

Try to make it a little smarter if possible. Thank you

---

## 9. Backups
**What to do:** Observe the list of backup runs. Do **not** restore or delete.

- [x] Runs listed with size, file count, restored flag.
- [x] Status shows count and backup root.

The backup screen looks a little empty.

- Check that and allow me to change the backup folder I'm supposed to select to any drive.
- For the Delete Ticked, Restore Ticked, and Pern folder, make sure the modal opens and that you specifically show what is actually happening.

All those things as well for the backup

---

## 10. Schedule / Rules
**What to do:** Observe. Do **not** toggle schedules or save rules.

- [x] Schedule page shows weekly/daily switches and times.
- [x] Rules page shows the rules editor and list.

For the scheduling add more: Scheduling will come later because we are still in tier 2 only 

---

## 11. Settings
**What to do:** Browse the settings sections. Do **not** change or save.

- [x] Page opens without crash.

---

## 12. Arrange / other pages
- [x] All 15 pages open (smoke test covers this).

---

## Known risks (read before you start)

1. **No deletions in this review.** Every destructive action (Recycle, Archive, Clean, Uninstall, admin tasks) is behind a review dialog, but skip them entirely for this pass.
2. **Admin tasks** (System page) change system files — do not run them on your working PC.
3. **Uninstall** runs native mechanisms (MSI/MSIX/winget) that are unverified live; only run in a disposable VM.
4. **MFT fast scan** requires elevation and may trigger antivirus; not run during this review unless you explicitly elevate.
5. **Leftovers** are candidates, not confirmed junk — some belong to unregistered apps.
6. **Scan memory cap:** drives with >~1.75 M (4 GB PC) or ~2.1 M (8 GB+) nodes produce a partial map marked in the status.
7. **Whole-volume scan time:** E: may take 10–60 s; cancel is supported (partial results kept).
8. **winget inventory** shells out on Apps load (15 s cap) — first load may be slow.
9. **Data folder** is `%LOCALAPPDATA%\CDClean`; nothing in this review writes there except normal settings/audit (read-only browsing does not change settings).

---

## Review notes (write below)

### Screen: ___
- Works:
- Broken:
- Confusing:
- Suggestion:

### Screen: ___
- Works:
- Broken:
- Confusing:
- Suggestion:

(Add more as needed.)


![My Screenshot](<file:///C:/Users/HP/Pictures/Screenshots/Screenshot2507.png>)

