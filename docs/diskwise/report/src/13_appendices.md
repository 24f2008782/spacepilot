# Appendix A — Source Index

Every claim in this report of the form `[Sxx]` resolves here. Tiers: **T1** first-hand user report · **T2** official documentation or vendor licence/KB · **T3** technical press with named reporting · **T4** vendor/SEO/tutorial content used only as corroboration.

**Notes on accuracy.** Reddit and forum content was retrieved through a search layer, so thread text is excerpt-level and exact timestamps are frequently unavailable; the "Date" column gives the best available indication (article date where published, otherwise the period inferred from content, marked with `≈`). Tier T1 sources establish that a symptom was experienced — not that the reporter's diagnosis was correct. No engagement metrics (upvotes, views) are quoted anywhere, because they could not be verified.

| ID | Source | URL | Tier | Date / notes |
|---|---|---|---|---|
| S01 | r/Windows11 — Windows 11 storage keeps disappearing | https://www.reddit.com/r/Windows11/comments/1quu3tx/windows_11_storage_keeps_disappearing_after/ | T1 | ≈2026; includes 60 GB Delivery Optimization claim and hibernation advice |
| S02 | r/pcmasterrace — Files disappearing on drives, Windows 11 | https://www.reddit.com/r/pcmasterrace/comments/1bgz0xp/files_disappearing_on_drives_windows_11/ | T1 | Mar 2024; Storage Sense blamed; files later found in a hidden folder |
| S03 | r/WindowsHelp — Gigabytes of free space mysteriously missing | https://www.reddit.com/r/WindowsHelp/comments/1hqeuea/gigabytes_of_free_space_mysteriously_missing_on/ | T1 | Dec 2024; Windows 7→10/11 volume discrepancy; third-party tool disagrees with OS |
| S04 | r/WindowsHelp — Moved to Windows 11, won't tell me what's taking space | https://www.reddit.com/r/WindowsHelp/comments/rwaazk/moved_to_windows_11_and_it_wont_tell_me_whats/ | T1 | Jan 2022; "200 GB of unknown stuff" |
| S05 | r/Windows11 — Can I delete hiberfil.sys? | https://www.reddit.com/r/Windows11/comments/1m0nc7e/can_i_delete_hiberfilsys_it_is_taking_a_lot_of/ | T1 | Jul 2025 |
| S06 | r/techsupport — hiberfil.sys 31.9 GB, cannot delete, not shown in Explorer | https://www.reddit.com/r/techsupport/comments/1hzjd33/hiberfilsys_uses_319_gb_of_storage_cant_delete_it/ | T1 | Jan 2025 |
| S07 | r/techsupport — pagefile.sys 25.2 GB and hiberfil.sys 6.1 GB | https://www.reddit.com/r/techsupport/comments/1lkziir/pagefilesys_is_252_gb_and_hiberfilsys_is_61_gb/ | T1 | Jun 2025 |
| S08 | r/techsupport — Why is pagefile.sys so large and how to reduce it | https://www.reddit.com/r/techsupport/comments/1umh1b0/why_is_pagefilesys_so_large_on_my_system_and_how/ | T1 | Jul 2026; conflicting advice |
| S09 | r/Windows10 — Can/should I disable page file? 64 GB RAM | https://www.reddit.com/r/Windows10/comments/mk6ws7/canshould_i_disable_page_file_i_have_64gb_ram/ | T1 | Apr 2021; conflicting advice |
| S10 | Microsoft Learn — Clean up the WinSxS folder | https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/clean-up-the-winsxs-folder?view=windows-11 | T2 | Current; "don't delete the folder" |
| S11 | Microsoft Q&A — Why is WinSxS 30 GB? | https://learn.microsoft.com/en-us/answers/questions/4102109/why-is-winsxs-folder-30gb-too-large-come-on | T1/T2 | ≈2023–2024 |
| S12 | Microsoft Q&A — WinSxS growing too large (Server 2016) | https://learn.microsoft.com/en-us/answers/questions/4162252/winsxs-folder-issue-growing-too-large | T1 | 2023 |
| S13 | Microsoft Q&A — Remove Windows.old after upgrade; TrustedInstaller lock | https://learn.microsoft.com/en-us/answers/questions/3888242/how-do-i-remove-the-windows-10-directory-after-upg | T1/T2 | Mar 2025 |
| S14 | Microsoft Q&A — Removing Windows.old | https://learn.microsoft.com/en-us/answers/questions/5863540/removing-windows-old | T1/T2 | ≈2026 |
| S15 | Tech-Hub — Safely clean the SoftwareDistribution folder | https://techhub.com.lk/kb/article/cleanup-softwaredistribution-folder | T4 | 2026; corroborates stop-services procedure |
| S16 | iTechGuides — SoftwareDistribution FAQs (incl. Delivery Optimization cache path) | https://www.itechguides.com/windows-softwaredistribution-folder-faqs/ | T3/T4 | 2026 |
| S17 | Windows Latest — 24H2 creates undeletable 8.63 GB update cache | https://www.windowslatest.com/2024/10/10/windows-11-24h2-issue-creates-undeletable-8-63-gb-windows-update-cache/ | T3 | Oct 2024; checkpoint-update explanation |
| S18 | TechRadar — 24H2 eats 8.6 GB | https://www.techradar.com/computing/windows/windows-11-24h2-update-yoinks-8-6gb-of-your-precious-drive-space-and-refuses-to-give-it-back-but-a-fix-is-coming | T3 | Oct 2024 |
| S19 | ExtremeTech — 24H2 8.63 GB cache | https://www.extremetech.com/computing/windows-11-24h2-update-creates-an-863gb-cache-file-that-cannot-be-deleted-easily | T3 | Oct 2024 |
| S20 | PCMag — 24H2 junk file; Microsoft: "this is a reporting error" | https://www.pcmag.com/news/windows-11-24h2-update-comes-with-8gb-junk-file-you-cant-delete | T3 | Oct 2024 |
| S21 | r/windows — Windows Temp folder filling up | https://www.reddit.com/r/windows/comments/ieb1lt/windows_temp_folder_filling_up/ | T1 | 2020; 3 TB of AV definitions in temp |
| S22 | r/computerhelp — C drive keeps filling up for no reason | https://www.reddit.com/r/computerhelp/comments/1kdhyg5/c_drive_keeps_filling_up_for_no_reason/ | T1 | May 2025 |
| S23 | r/windows — "Crap load of temporary files" (43.7 GB, only 101 MB deletable) | https://www.reddit.com/r/windows/comments/dtxf6w/crap_load_of_temporary_files/ | T1 | 2019; recurring complaint |
| S24 | r/Windows11 — 5 GB of temporary files, nothing to delete | https://www.reddit.com/r/Windows11/comments/1h9vpap/how_do_i_delete_this_temporary_files_it_says_i/ | T1 | Dec 2024 |
| S25 | Microsoft Q&A — Temporary file size in Storage settings not accurate | https://learn.microsoft.com/en-us/answers/questions/3917031/temporary-file-size-to-remove-in-settings-system-s | T1/T2 | Jan 2025 |
| S26 | Microsoft Q&A — High storage temporary files in Windows 11 | https://learn.microsoft.com/en-us/answers/questions/3870567/high-storage-temporary-files-in-windows-11 | T1 | May 2025 |
| S27 | r/windows — Proven tool to remove installation leftovers? | https://www.reddit.com/r/windows/comments/n8od7u/is_there_a_proven_tool_to_remove_installations/ | T1 | 2021 |
| S28 | TheWindowsClub — Remove leftover files after uninstall | https://www.thewindowsclub.com/how-to-remove-leftover-files-after-uninstall-in-windows | T4 | 2023 |
| S29 | Bretech — Safely remove leftover files after uninstalling | https://www.bretech.net/blog/how-to-remove-leftover-files-after-uninstalling-software/ | T4 | 2026 |
| S30 | r/software — Revo Uninstaller safe? (Affinity Designer → Ableton Live false positive) | https://www.reddit.com/r/software/comments/1bofvtr/revo_uninstaller_safe/ | T1 | Mar 2024 |
| S31 | r/antivirus — Revo Uninstaller scan modes; risk of unrelated matches | https://www.reddit.com/r/antivirus/comments/1bpwfvi/question_about_revo_uninstaller/ | T1 | Mar 2024 |
| S32 | r/computers — Program won't let me uninstall (missing uninstaller) | https://www.reddit.com/r/computers/comments/1h23jmv/need_help_program_wont_let_me_uninstall_it/ | T1 | Nov 2024 |
| S33 | TechBloat — Uninstall via Registry Editor; broken uninstall strings | https://www.techbloat.com/how-to-uninstall-programs-using-registry-editor-in-windows-11.html | T4 | 2026 |
| S34 | Recoverit — "Windows cannot find uninstall.exe" | https://recoverit.wondershare.com/computer-problems/windows-cannot-find-uninstall-exe.html | T4 | 2026 |
| S35 | ITTrip — Remove uninstalled "ghost" startup entries | https://en.ittrip.xyz/windows11/win11-startup-ghost-app | T4 | 2026 |
| S36 | RealityPathing — McAfee services still running after uninstall | https://realitypathing.com/mcafee-services-still-run-after-uninstall-how-to-clean-up/ | T4 | 2026 |
| S37 | r/techsupport — I can't delete this file ("open in ReMouse", uninstalled) | https://www.reddit.com/r/techsupport/comments/1qtlfpl/i_cant_delete_this_file/ | T1 | ≈2026 |
| S38 | r/techsupport — File won't delete in Windows 11 (LockHunter; dllhost) | https://www.reddit.com/r/techsupport/comments/1j6jec4/file_wont_delete_in_windows_11/ | T1 | Mar 2025 |
| S39 | r/WindowsHelp — File cannot be deleted because it is in use (Filmora) | https://www.reddit.com/r/WindowsHelp/comments/1g2n1hd/file_cannot_be_deleted_because_it_is_being_used/ | T1 | Oct 2024 |
| S40 | r/techsupport — Taking ownership: access denied (TrustedInstaller owner) | https://www.reddit.com/r/techsupport/comments/mpvj7o/taking_ownership_of_program_file_folder_access/ | T1 | 2021 |
| S41 | r/windows — TrustedInstaller rights for System32 | https://www.reddit.com/r/windows/comments/hukf76/how_to_get_trusted_installer_rights_for_system32/ | T1 | 2020 |
| S42 | Systweak — "You require permission from TrustedInstaller" | https://www.systweak.com/blogs/fix-trustedinstaller-permission-error-windows/ | T4 | 2026 |
| S43 | Ten Forums — C:\Windows\Installer 60 GB, safe to delete? | https://www.tenforums.com/performance-maintenance/152725-c-windows-installer-60-gbs-safe-delete.html | T1 | 2020; still widely cited |
| S44 | Super User — Is it safe to delete C:\Windows\Installer (Microsoft technician quoted) | https://superuser.com/questions/1392087/is-it-safe-to-delete-c-windows-installer-on-windows-10 | T1/T2 | 2019 |
| S45 | DiskMop — C:\Windows\Installer is 30 GB: what can be deleted | https://diskmop.com/blog/windows-installer-folder-cleanup | T4 | 2026 |
| S46 | r/Windows10 — Why does Windows deny access to WindowsApps | https://www.reddit.com/r/Windows10/comments/ob40fy/why_does_windows_deny_my_access_to_windowsapps/ | T1 | 2021 |
| S47 | r/windowsapps — WindowsApps folder ("had to reinstall Windows") | https://www.reddit.com/r/windowsapps/comments/qihmpw/windowsapp_folder/ | T1 | 2021 |
| S48 | r/techsupport — WindowsApps/ModifiableWindowsApps on a games drive | https://www.reddit.com/r/techsupport/comments/x8cbl4/windowsapps_and_modifiablewindowsapps_appeared_on/ | T1 | 2022 |
| S49 | r/sysadmin — System Volume Information consuming the volume | https://www.reddit.com/r/sysadmin/comments/13anol5/csystem_volume_information_taking_up_massive/ | T1 | May 2023 |
| S50 | r/techsupport — What is System Volume Information, can I delete it | https://www.reddit.com/r/techsupport/comments/rd1y9x/what_is_system_volume_information_and_can_i/ | T1 | 2021 |
| S51 | ACSApp — Reduce a large System Volume Information folder | https://acsapp.com/blog/how-to-reduce-a-large-system-volume-information-folder-in-windows/ | T4 | 2026 |
| S52 | r/wsl2 — ext4.vhdx taking too much storage with no usage | https://www.reddit.com/r/wsl2/comments/1mi4sj5/ext4vhdx_taking_too_much_storage_with_no_usage/ | T1 | Aug 2025 |
| S53 | r/docker — Docker + WSL2 VHDX keep growing even when empty | https://www.reddit.com/r/docker/comments/1msw42x/docker_wsl2_vhdx_files_keep_growing_even_when/ | T1 | Aug 2025 |
| S54 | r/docker — Docker taking 30 GB after prune | https://www.reddit.com/r/docker/comments/1bzkp0u/docker_taking_30gb_of_disk_space_even_after_a/ | T1 | Apr 2024 |
| S55 | r/docker — Docker drive suddenly large; sparse VHD advice | https://www.reddit.com/r/docker/comments/1fqv2ub/docker_drive_on_windows_recently_started_taking/ | T1 | Sep 2024 |
| S56 | r/webdev — node_modules eating 70 GB | https://www.reddit.com/r/webdev/comments/1kizdsz/node_modules_is_eating_70gb_of_my_projects_folder/ | T1 | May 2025 |
| S57 | r/node — How much space does Node.js take | https://www.reddit.com/r/node/comments/1dej3hy/how_much_space_does_nodejs_take/ | T1 | Jun 2024 |
| S58 | r/nextjs — npm run dev takes 10 GB on C: | https://www.reddit.com/r/nextjs/comments/1d4qu7y/npm_run_dev_and_after_1_hour_it_take_10gb_in_c/ | T1 | May 2024 |
| S59 | r/sysadmin — WizTree 3 release thread (TreeSize over-counts WinSxS hard links) | https://old.reddit.com/r/sysadmin/comments/6x43dr/wiztree_3_has_just_been_released_its_an_amazingly/ | T1 | 2017; historically important |
| S60 | FileCroco — WizTree review (hard links counted once; totals reconcile) | https://www.filecroco.com/download-wiztree/ | T3 | 2026 |
| S61 | GitHub dundee/gdu #688 — OneDrive online-only files counted as disk usage | https://github.com/dundee/gdu/issues/688 | T1 | Oct 2026 |
| S62 | GitHub dundee/gdu #692 — Fix: zero disk usage for online-only files | https://github.com/dundee/gdu/pull/692 | T1 | 2026 |
| S63 | r/WindowsHelp — No matter how much storage I cut off it's never enough | https://www.reddit.com/r/WindowsHelp/comments/1ii2csg/no_matter_how_much_storage_i_cut_off_its_never/ | T1 | Feb 2025 |
| S64 | r/Windows11 — Storage Sense only cleans C: | https://www.reddit.com/r/Windows11/comments/11m8bjm/storage_sense_is_so_frustratingly_close_to_being/ | T1 | Mar 2023 |
| S65 | Microsoft Support — Manage drive space with Storage Sense | https://support.microsoft.com/en-us/windows/experience/storage-filemanagement/manage-drive-space-with-storage-sense | T2 | Current |
| S66 | r/WindowsHelp — Whole Downloads folder cleared | https://www.reddit.com/r/WindowsHelp/comments/ogre44/my_whole_downloads_folder_has_been_cleared_except/ | T1 | 2021 |
| S67 | Microsoft Q&A — Windows 11 24H2 update space issue | https://learn.microsoft.com/en-us/answers/questions/3905630/windows-11-24h2-update-space-issue | T1 | Jan 2025 |
| S68 | CloudHouse — Fix 24H2 update failing (20 GB requirement) | https://cloudhousetechnologies.com/blog/how-to-fix-windows-11-24h2-update-failing-not-installing-2026 | T4 | 2026 |
| S69 | Microsoft Q&A — How to fix corrupted files (SFC) | https://learn.microsoft.com/en-us/answers/questions/5683492/how-to-fix-corrupted-files-in-windows | T1/T2 | 2025 |
| S70 | Microsoft Q&A — SFC cannot repair corrupted system files | https://learn.microsoft.com/en-us/answers/questions/6059921/windows-sfc-cannot-repair-corrupted-system-files | T1 | 2026 |
| S71 | r/pcgamingtechsupport — Uninstalled game still taking up space | https://www.reddit.com/r/pcgamingtechsupport/comments/gmf7v3/uninstalled_game_still_taking_up_space/ | T1 | 2020 |
| S72 | r/Steam — Uninstalling Steam games eats some GB still | https://www.reddit.com/r/Steam/comments/1bp9rb1/uninstalling_steam_games_eats_some_gb_still/ | T1 | Mar 2024 |
| S73 | r/windows — Delete mass amounts of subfolders (invert-selection workaround) | https://www.reddit.com/r/windows/comments/18i1984/need_to_delete_mass_amounts_of_subfolders_but/ | T1 | Dec 2023 |
| S74 | Super User — How to delete huge number of files (Explorer memory blow-up) | https://superuser.com/questions/416071/how-to-delete-huge-number-of-files-on-windows | T1 | 2012; still referenced |
| S75 | r/Windows11 — Delete multiple files matching a pattern | https://www.reddit.com/r/Windows11/comments/18gemcx/how_do_you_delete_multiple_files_with_the_same/ | T1 | Dec 2023 |
| S76 | r/DataHoarder — Free duplicate photo finder for 140k photos | https://www.reddit.com/r/DataHoarder/comments/m7m6i1/free_duplicated_photo_finder_for_windows_i_have/ | T1 | 2021 |
| S77 | r/DataHoarder — Duplicate photo finder for Windows | https://www.reddit.com/r/DataHoarder/comments/1jz22mo/duplicate_photo_finder_for_windows/ | T1 | Apr 2025 |
| S78 | r/Windows10 — Best duplicate photo free app (free-tier deletion caps) | https://www.reddit.com/r/Windows10/comments/grkypo/best_duplicate_photo_free_app/ | T1 | 2020 |
| S79 | EndpointWeekly — WMIC removed from Windows 11 | https://endpointweekly.com/blog/wmic-removed-windows-11-replace-scripts-powershell-cim.html | T3 | Aug 2026 |
| S80 | Neowin — Microsoft removing a 25-year-old legacy feature (WMIC) | https://www.neowin.net/news/microsoft-is-removing-a-legacy-windows-feature-that-has-been-around-for-25-years/ | T3 | Aug 2026 |
| S81 | iTechGuides — Optimize/defragment drives in Windows 11 (retrim) | https://www.itechguides.com/how-to-optimize-and-defragment-drives-in-windows-11/ | T3 | 2026 |
| S82 | Windows Latest — Optimize Drives redesign; weekly schedule; SSDs are trimmed | https://www.windowslatest.com/2026/11/03/windows-11s-big-redesign-now-covers-even-its-ancient-defrag-tool-as-microsoft-finally-bets-on-winui-after-years-of-neglect-and-pushing-webview/ | T3 | Nov 2026 |
| S83 | r/WindowsHelp — "What is this file, it's taking 100 GB" (Delivery Optimization) | https://www.reddit.com/r/WindowsHelp/comments/1kkhpe4/what_is_this_file_is_it_safe_to_delete_its_taking/ | T1 | May 2025 |
| S84 | r/software — WizTree vs TreeSize (MFT read; seconds vs minutes) | https://www.reddit.com/r/software/comments/ugawm0/wiztree_vs_treesize_which_would_you_recommend_and/ | T1 | May 2022 |
| S85 | WizTree — End User License Agreement | https://diskanalyzer.com/eula | T2 | Current; personal-use grant; no reverse engineering |
| S86 | GitHub windirstat/windirstat — repository and GPL notice | https://github.com/windirstat/windirstat | T2 | Current |
| S87 | GitHub harryytm/windirstat — README stating GPL-2.0 | https://github.com/harryytm/windirstat | T2 | Mirror/derivative; licence text |
| S88 | GitHub Klocman/Bulk-Crap-Uninstaller — README, Apache-2.0 | https://github.com/Klocman/Bulk-Crap-Uninstaller | T2 | Current |
| S89 | JAM Software KB — TreeSize Free limitations (no hard links/ADS/dedup) | https://knowledgebase.jam-software.com/treesize_free/ | T2 | Current; vendor KB |
| S90 | Evinco — Everything by voidtools (MFT indexing) | https://www.evinco-software.com/eng/blog/everything-by-voidtools-the-ultimate-fast-windows-file-search-utility/ | T3 | 2026 |
| S91 | NinjaOne — How to use Disk Cleanup (status/practice) | https://www.ninjaone.com/blog/how-to-use-disk-cleanup-in-windows/ | T3 | 2025 |
| S92 | Microsoft Q&A — Windows component cleanup (StartComponentCleanup /ResetBase) | https://learn.microsoft.com/en-au/answers/questions/5927876/windows-component-cleanup | T1/T2 | Jun 2026 |
| S93 | r/software — Program to remove leftovers (tool-by-tool leftover counts; BCU 364 MB portable) | https://www.reddit.com/r/software/comments/zk1m80/program_to_remove_leftovers_from_various_programs/ | T1 | Dec 2022 |
| S94 | r/Windows11 — Which is the best uninstaller? | https://www.reddit.com/r/Windows11/comments/ybivqe/which_is_the_best_uninstaller/ | T1 | Oct 2022 |
| S95 | r/privacy — Alternative to CCleaner's uninstall feature | https://www.reddit.com/r/privacy/comments/1k3bf82/looking_for_an_alternative_to_ccleaners_uninstall/ | T1 | Apr 2025 |
| S96 | r/software — Best uninstaller for Windows?!? | https://www.reddit.com/r/software/comments/1mvg2vj/best_uninstaller_for_windows/ | T1 | Aug 2025 |
| S97 | r/WinDirStat — community reports (CSV export crash, slow scanning) | https://www.reddit.com/r/WinDirStat/ | T1 | ≈2026 crawl |
| S98 | GitHub Drawer/TreeSizr — MFT analyser with hard-link/placeholder handling | https://github.com/Drawer/TreeSizr | T3 | 2026; **licence not verified** — no reuse permitted until checked |
| S99 | r/techsupport — Analyser stops at 25% on a full drive | https://www.reddit.com/r/techsupport/comments/1oum8r/finding_ghost_data_on_almostfull_hard_drive_both/ | T1 | 2013; historical pattern |

## A.1 Sources deliberately not used

- SEO listicles and "best cleaner 2026" pages (no first-hand evidence, frequently rewritten copy).
- AI-generated answer summaries on Q&A sites, except where they merely restate a documented Microsoft procedure already supported by T1/T2 sources.
- Any X/Twitter content: not retrievable in this environment (§2.2).

---

# Appendix B — Icon Technical Specification

| Property | Value |
|---|---|
| Master canvas | 512 × 512, 96 px corner radius (~22.5%) |
| Safe margin | Geometry confined to a 360 px circle; the amber shard reaches x≈375, within the 512 canvas |
| Palette | Navy `#1B2E4C → #080E18`; arc `#1FA8E0 → #2DE2C5`; shard `#FFC24D → #FF8A1F` |
| ICO frames | 16, 20, 24, 32, 40, 48, 64, 96, 128, 256 (RGBA) |
| 16–24 px handling | Optical variant: ring stroke 66 (vs 50), no inner bars, larger shard, higher arc contrast |
| Light theme | Tile `#FFFFFF → #EAF1F8`, arc `#0E7FB8 → #16C9AB`, shard `#F5A623 → #E07A00`, inner marks `#0B1424` |
| Dark theme | Transparent background, brighter arc `#7CF7DE`, used on dark surfaces where a navy tile would disappear |
| Monochrome | Single-colour paths using `currentColor` for docs, print, and themed contexts |

**Shell integration checklist (to be completed in your build):**

1. Add the ICO to the resource script: `IDI_APP_ICON ICON "diskwise.ico"`.
2. Load with `LoadIconWithScaleDown(hInst, MAKEINTRESOURCE(IDI_APP_ICON), cx, cy, &hicon)` so Windows selects the nearest frame instead of stretching a single bitmap.
3. Set an explicit `AppUserModelID` (`SetCurrentProcessExplicitAppUserModelID(L"YourCompany.DiskWise")`) and register the same ID in the shortcut so taskbar pinning and jump lists use the product icon.
4. Window class icons: `wcex.hIcon` = 32 px, `wcex.hIconSm` = 16 px; on DPI change, reload at the new scale rather than letting the shell scale.
5. Verify at 100/125/150/175/200% and in Start menu "All apps" (44×44 target), taskbar (32 px at 100%, 40 px at 125%), title bar (16/20/24 px), and the installer/Apps list (48 px).

---

# Appendix C — Registry Safety Rules

The product's registry capability is deliberately narrow. These rules are normative for implementation.

## C.1 Allowed scope (read-only by default)

| Scope | Allowed operation |
|---|---|
| `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\<product>` and WOW6432Node equivalent | Read; export; remove **only** as part of a verified uninstall of that exact product |
| `HKLM\SOFTWARE\<Publisher>\<Product>` | Read; export; remove only for class-A ownership |
| `HKCU\SOFTWARE\<Publisher>\<Product>` | Same, per-user scope only |
| `HKLM\SYSTEM\CurrentControlSet\Services\<service>` | Read-only report; removal only through the service control manager after review |
| `...\CurrentVersion\Run` / `RunOnce` values referencing the removed product's executable | Remove only with class-A evidence and export |
| File-association entries owned by the product (`HKCR\Applications\<exe>`, product-specific ProgIDs) | Remove only if no other installed product references them |
| `...\Installer\Products` / `Features` | **Never** modified directly; MSI APIs only |

## C.2 Absolute prohibitions

- No bulk "registry scan and clean" mode exists in the product.
- No modification of `HKEY_CLASSES_ROOT\CLSID`, `Interface`, `TypeLib` entries, Windows Defender, servicing, policy, or security-provider keys.
- No change to ownership or ACLs on any registry key.
- No removal of a key that fails ownership verification — it is reported, not removed.
- No claim that registry cleaning improves performance.

## C.3 Mandatory procedure for any approved removal

1. Export the key (or subtree) to `.reg` into the job's restore bundle; record SHA-256 of the export.
2. Verify the export is readable by re-parsing it before proceeding.
3. Remove the key with `RegDeleteTreeW` scoped to the verified path (never a recursive delete of a parent).
4. Record the operation in the audit log with the evidence class and the export path.
5. On restore: import with `RegRestoreKey`/`reg import`, then verify key presence and value data.
6. If a restore fails, surface the exact export path to the user with instructions — never silently swallow the failure.

## C.4 Restore-bundle contents per job

`manifest.jsonl` (every item + reason code) · `files/` (quarantined files with sidecar metadata) · `registry/*.reg` (exports) · `verify.txt` (hashes and post-conditions) · `README.txt` (plain-language restore instructions).

---

# Appendix D — Assumptions and Open Questions

## D.1 Assumptions made in this report

1. The existing application is a native Windows desktop program written predominantly in C with a Win32 or MFC-style UI; this is inferred from the brief, not verified.
2. Distribution is proprietary/closed-source. If the product is or will be GPL-compatible, §7's licence conclusions change materially — and WinDirStat's codebase becomes an option.
3. The target audience is technically curious home users and small IT; enterprise deployment is a secondary concern reflected only in the allow-listing and CLI notes.
4. The developer is a small team; effort estimates assume one competent Windows C developer.
5. The product may request elevation per operation, and the current build is not `requireAdministrator` — **[PENDING VERIFICATION]**.

## D.2 Questions that must be answered before Phase 0 ends

| # | Question | Why blocking |
|---|---|---|
| Q1 | Does the current build run elevated at startup? | Determines whether the broker in §9.7 is mandatory or optional |
| Q2 | Is selection index-based, path-based, or handle-based? | Determines whether R2 is an extension or a replacement |
| Q3 | Are deletions direct, `SHFileOperation`, or `IFileOperation`? | Determines the reversibility strategy |
| Q4 | Which toolkit renders the tables, and are they virtual? | Determines R8's performance ceiling |
| Q5 | Is there an existing quarantine/backup concept? | Determines Phase 0 scope |
| Q6 | What is the current settings format and location? | Determines migration work in R14 |
| Q7 | Which Windows versions are supported today? | Determines API floors (e.g. `GetFileInformationByHandleEx` is Vista+, USN is universal) |
| Q8 | Is the binary code-signed today? | Determines the EDR strategy and SmartScreen behaviour |
| Q9 | Is there an installer/updater to preserve? | Determines packaging work |
| Q10 | Are there existing localisation resources? | Determines string-handling rules in the new UI |

## D.3 Closing note

This document separates three things carefully: what the evidence shows (§3), what the design should be (§4–§9), and what was actually built (§12). Only the last is a claim of accomplishment, and it is limited to the icon assets and the tested C reference core. The rest is a plan that becomes real the moment the source code is available — and the first ten days of that plan are written out in §12.4 so that no further analysis is needed to begin.

---
