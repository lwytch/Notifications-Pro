# Plan

## Milestones

### Milestone 1: Core App — Tray + Overlay + Settings + Preview
**Status: Complete**

- [x] Solution structure (.NET 8 WPF)
- [x] AppSettings model with defaults
- [x] SettingsManager (load/save %AppData%\NotificationsPro\settings.json)
- [x] QueueManager (configurable visible limit, overflow count, dedup, expiry)
- [x] System tray app with dark-themed context menu
- [x] Overlay window (transparent, always-on-top configurable, slide-in animations)
- [x] Settings window (tabbed: Appearance / Behavior / Position)
- [x] Preview/test notification button
- [x] Premium dark theme (custom button, toggle, slider, combobox, tab, scrollbar, tooltip styles)
- [x] Unit tests (QueueManager, SettingsManager, SnapHelper)
- [x] Overlay drag fix: PreviewMouseLeftButtonDown bypasses ScrollViewer
- [x] Performance fix: removed DropShadowEffect from notification cards

### Milestone 2: Real Notification Capture
**Status: Complete**

- [x] Implement UserNotificationListener API
- [x] Request notification access permission (auto-prompts on first run)
- [x] Wire live notifications into QueueManager
- [x] Privacy validation: only notification IDs (system uint) tracked, no content stored
- [x] Document API limitations
- [x] Seed existing notifications on startup (mark as seen, don't display)
- [x] Trim seen IDs set to prevent unbounded growth (cap at 5000)
- [x] Status indicator in tray menu showing listener state
- [x] Polling fallback (every 2s) with overlap guard
- [x] Diagnostic status messages (captured count, system count, poll number)
- [x] "Grant Notification Access" always visible in tray menu
- [x] "Retry Access Check" works even when access already granted
- [x] Accessibility fallback hardened for Win11 (broader events + shell host detection + live diagnostics)
- [x] Capture-mode override and recovery: System tab now offers Auto / Prefer WinRT / Force Accessibility, and WinRT seed/poll failures automatically switch to accessibility capture
- [ ] Align timing with system toast duration when available (deferred — using configurable duration)

### Milestone 3: Customization Polish
**Status: In Progress**

Per-field typography:
- [x] Per-field font size — independent sliders for App Name, Title, and Body
- [x] Per-field font weight — independent dropdowns for App Name, Title, and Body

Card shape & layout:
- [x] Border toggle + color picker + thickness slider (wired to UI)
- [x] Accent stripe thickness slider (configurable, was hardcoded 3px)
- [x] Accent stripe toggle (ability to disable independently)
- [x] Card gap / spacing between cards (configurable, was hardcoded 8px)
- [x] Outer content margin around card area (configurable, was hardcoded 4px)

Overflow badge consistency:
- [x] Overflow badge inherits card colors (background, text, font, corner radius)

Deduplication controls:
- [x] Deduplication on/off toggle
- [x] Deduplication window duration slider

Animation refinement:
- [x] Slide-in direction (left / right / top / bottom — configurable via Settings > Behavior > Animations)

Settings UI reorganization (Behavior tab has 6 concerns in one panel):
- [ ] Split Behavior tab — move content/display controls to a separate tab from timing/window/animation (deferred)
- [ ] Ensure logical grouping: Typography, Colors, Card Shape, Content/Display, Behavior, Position (deferred)
- [x] Background-image and spacing polish: Restore the single-column settings rhythm on Appearance and other affected tabs, move explanatory copy into tooltips where possible, add explicit image fit behavior options, and verify the rollback README/help text still matches the shipped single-panel UI.
- [x] App-specific background assets: Allow specific apps to override the default card background image treatment without reintroducing multi-panel complexity, and add fullscreen-backdrop image support with fit/opacity controls that stay privacy-safe and export/import correctly.
- [x] Settings ownership cleanup: Add an Apps tab for per-app presentation overrides, and move Quiet Hours plus Burst Limiting out of Filtering into a behavior/scheduling home that matches what they actually do.
- [x] Overlay scrollbar theming and interaction repair: fix the overlay hit-test split so the scrollbar and search box receive normal client input without losing drag-anywhere behavior elsewhere, add themed track/thumb color plus padding/radius controls in Appearance, and keep theme/export/import coverage aligned with those new style settings.

Previously completed (Milestone 3):
- [x] Click-through toggle (Win32 WS_EX_TRANSPARENT)
- [x] Tray quick toggle for click-through recovery (avoid drag lockout)
- [x] Notification content controls (show/hide app name, title, body)
- [x] Configurable max visible notifications (1-40)
- [x] Overlay auto-size polish (removes ghost backdrop panel and stray scrollbar)
- [x] One-line banner mode (compact per-notification display)
- [x] Optional wrapped text in one-line mode (prevents truncation on smaller monitors)
- [x] One-line wrapped banner max-lines control (balance readability vs density)
- [x] Optional auto full-width behavior in one-line mode
- [x] One-line mode now honors app/title/body text color choices in both compact and wrapped banners
- [x] Returning from one-line mode now restores stacked line-limiting defaults for denser card flow
- [x] Stacked order control (newest-on-top vs newest-on-bottom)
- [x] App-name color customization (separate from title/body colors)
- [x] Built-in color picker buttons in Settings UI
- [x] Manual edge resize support (left/right drag handles)
- [x] Animation style controls (fade-only + extended speed range)
- [x] Tray icon preview shown in Settings header
- [x] Per-field line count controls (app/title/body)
- [x] Full-wrap stacked mode option (disable truncation / line clamps)
- [x] Long-content overflow affordance (scroll when stacked cards exceed max overlay height)
- [x] Preserve live manual width when changing unrelated settings
- [x] Tray quick toggle for Always on Top
- [x] Edge snapping on all monitors
- [x] Position presets in Settings (top/side placement shortcuts)
- [x] Max overlay height now supports high-resolution ranges up to 8K-class displays
- [x] Overlay-height presets added (1080p / 2K / 4K / 8K)
- [x] Max overlay width now supports high-resolution ranges up to 8K-class displays
- [x] Overlay-width presets added (1080p / 2K / 4K / 8K)
- [x] Guard card animations against frozen template transforms to prevent startup crashes

### Milestone 4: Card Interaction
**Status: Complete**

Core interaction:
- [x] Click to dismiss — click a card to immediately remove it (via WM_NCLBUTTONUP with drag detection)
- [x] Hover to pause expiry timer — mouse over the overlay pauses all countdowns, resume on mouse leave (via WM_NCMOUSEMOVE + poll timer)
- [x] Dismiss all — "Clear All" in right-click context menu and tray menu item
- [x] Copy notification text — right-click context menu "Copy Text" copies card content to clipboard

Right-click context menu on cards:
- [x] Dismiss this notification
- [x] Mute this app (quick per-app suppress, ties into Milestone 5 filtering)
- [x] Copy text to clipboard

Timestamps:
- [x] Optional timestamps on cards with selectable format (Relative / Time / DateTime)
- [x] Timestamp display controls in settings (show/hide + style + size + weight + color)

### Milestone 5: Filtering & Smart Control
**Status: Complete**

Per-app filtering:
- [x] Maintain a list of seen app names (names only, no content — privacy safe)
- [x] Per-app mute/unmute in settings Filtering tab (toggle buttons populated from seen apps)
- [x] Quick "Mute this app" from card context menu
- [x] Quick-mute from tray menu (recent apps list with mute toggles)

Keyword rules:
- [x] Keyword highlighting — highlight notifications containing specific words with a configurable accent color
- [x] Keyword muting — suppress notifications containing specific words

Scheduling & focus:
- [x] Quiet hours / Do Not Disturb schedule — auto-pause between configured hours (e.g. 22:00–08:00), handles midnight wrapping
- [x] Focus mode — timed pause from tray menu (15/30/60 min) with auto-resume and live countdown display
- [x] Burst-rate limiting — auto-suppress when N+ notifications arrive within M seconds (protects against notification storms)

Settings UI:
- [x] New Filtering tab in Settings window (per-app mute, keyword highlight/mute, quiet hours, burst limit)

### Milestone 6: Themes & Profiles
**Status: Complete**

Built-in theme presets:
- [x] Ship 6 presets: "Windows Dark" (default), "Dark Purple", "Light", "Frosted Glass", "High Contrast", "Minimal"
- [x] One-click apply sets all colors, opacity, corner radius, and accent at once
- [ ] Per-field "reset to theme default" micro-button next to each color picker (deferred)

Custom themes:
- [x] Save current settings as a named custom theme (stored in %AppData%\NotificationsPro\themes\)
- [x] Load / switch between saved themes
- [x] Delete custom themes

Import / export:
- [x] Export settings as shareable JSON file
- [x] Import settings from JSON file

Tray integration:
- [x] Theme quick-switch submenu in tray menu (built-in + custom themes)

Profiles (stretch):
- [ ] Named profiles (e.g., "Work", "Gaming", "Streaming") that bundle theme + filter rules + position + behavior
- [ ] Quick-switch between profiles from tray menu

### Milestone 7: Accessibility & Inclusivity
**Status: Complete**

Professional accessibility support beyond basic customization.

System integration:
- [x] Respect Windows "Reduce motion" setting (`SystemParameters.ClientAreaAnimation`) — force fade-only transitions when system motion is reduced
- [x] Toggle: "Respect Windows Reduce Motion" (on by default, allow override)
- [x] Detect Windows High Contrast mode (`SystemParameters.HighContrast`) — auto-apply High Contrast theme preset
- [x] Respect Windows text scaling — optional "Scale with Windows text size" toggle
- [x] AutomationProperties on overlay window for screen reader hint (LiveSetting=Assertive)

Color accessibility:
- [x] ContrastHelper utility for WCAG 2.1 contrast ratio calculation
- [x] WcagContrastTextConverter and WcagContrastColorConverter for XAML binding
- [x] Color-Blind Safe built-in theme (Wong palette — 7th theme)
- [ ] Display WCAG contrast ratio inline on color pickers (deferred — converters ready)
- [ ] Warn when chosen text/background colors fall below WCAG AA minimum (deferred)

Notification timing:
- [x] Auto-duration by content length — configurable extra seconds per line of body text
- [x] Persistent notification option — notifications stay visible until manually dismissed
- [x] Extended max duration range (30s → 120s)

Keyboard & motor accessibility:
- [x] Focus indicators visible on PrimaryButton, SecondaryButton, ToggleSwitch, Slider, ComboBox
- [x] Minimum click target sizes on toggle switches (44dp MinWidth/MinHeight)
- [x] Global hotkeys for overlay control (toggle visibility, dismiss all, toggle DND)
- [x] Hotkey configuration UI in Accessibility tab
- [x] Spoken notification combinations expanded: Title Only, Title + Body, Body + Timestamp, Title + Timestamp, and Title + Body + Timestamp
- [x] Per-app spoken notification controls and voice setup help: choose which seen apps are narrated, surface Windows voice-install guidance, and disclose the built-in voice requirements clearly
- [x] Spoken notification replay guard: visible cards are spoken once, new arrivals no longer replay cards that already finished speaking, and unfinished visible cards can still resume after pause/unmute
- [x] Spoken app selector clarity: switched from Speak/Skip buttons to explicit Read Aloud checkboxes
- [x] Spoken voice availability and trigger clarity: enumerate every voice Windows exposes to Notifications Pro across app and desktop speech APIs, add an explicit narration trigger mode for `All allowed notifications` vs `Only matching narration rules`, and update help/readme wording so Narrator-only voices are disclosed honestly.
- [ ] Full keyboard navigation audit (deferred)

Cognitive accessibility:
- [x] Information density presets ("Compact" / "Comfortable" / "Spacious") — one-click bundles
- [x] New Accessibility tab in Settings window

### Milestone 8: UX Polish & Settings Enhancements
**Status: Complete**

Professional-grade polish that distinguishes "functional" from "premium".

Live preview:
- [x] Inline mini-preview card inside settings window — always-visible sample card that updates in real-time as sliders/pickers change (no need to send test notification or find the overlay)
- [x] Preview shows all three fields (app name, title, body) with current style applied

Empty & first-run states:
- [x] Empty overlay ghost card — when no notifications visible, show a low-opacity "Waiting for notifications..." placeholder so new users know the overlay is there
- [x] First-run tray balloon — `NotifyIcon.ShowBalloonTip()` on first launch with tray/settings quick-start guidance
- [x] First-run tip bar in settings — dismissable info bar with updated navigation/theme/shortcut guidance
- [x] Track `HasShownWelcome` in AppSettings (UI state, not notification content)

Settings window polish:
- [x] Confirm before "Reset to Defaults" — MessageBox confirmation to prevent accidental resets
- [x] "Saved" micro-feedback — brief "Saved" label appears next to "Changes are saved automatically" on each save (1.5s auto-hide)
- [x] Remember settings window position between opens (save to AppSettings)
- [x] Ctrl+T keyboard shortcut to send test notification from settings
- [x] About dialog runtime details: tray About now shows the full 4-part version, package identity, listener status, install path, and runtime version
- [x] Settings persistence audit: text alignment now round-trips through settings save/load, and export/import tests cover the new narration and capture-mode fields

Tray icon enhancements:
- [x] Checkmarks on tray menu toggle items (Pause, Always on Top, Click-Through)
- [x] Tray icon state variants — dimmed/monochrome when paused
- [x] Notification count badge on tray icon (active card count, red badge in bottom-right)
- [x] "Clear All Notifications" in tray menu (already existed from M4)

### Milestone 9: System Integration & Multi-Monitor
**Status: Complete**

- [x] Start with Windows toggle (registry `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`)
- [x] Global hotkeys via `RegisterHotKey` Win32 API (completed in M7)
- [x] Hotkey configuration UI in settings (completed in M7)
- [x] Multi-monitor picker in Position tab (ComboBox populated with `Screen.AllScreens` names + resolution)
- [x] Move overlay to selected monitor with one click
- [x] Per-monitor DPI awareness (.NET 8 WPF provides per-monitor V2 DPI awareness by default)
- [x] Monitor-aware position presets (quick position buttons now target the selected monitor)

### Milestone 10: Streaming & Presentation
**Status: Complete**

Features for streamers, presenters, and content creators.

- [x] Chroma key background option — solid green/blue/magenta background for OBS chroma key capture
- [x] OBS-friendly fixed window mode — non-transparent, fixed-size window that captures cleanly in OBS window capture
- [x] Presentation mode — auto-enable DND when a specific app is fullscreen (PowerPoint, Zoom, etc.)
- [x] Per-app color tinting — optional subtle background tint derived from app name hash (e.g., Teams = blue, Slack = purple)

### Milestone 9.5: Enhanced Settings, Sounds, Icons & Theming
**Status: Complete**

Quick wins, dynamic theming, sounds, icons, and settings UX enhancements.

- [x] Width text input box (Position tab) for precise pixel overlay width entry
- [x] Accessibility master toggle + section descriptions in Accessibility tab
- [x] Fullscreen overlay mode with configurable opacity
- [x] Settings window dynamic theming (Windows Dark/Light, High Contrast, System, or Custom colors)
- [x] True Frosted Glass settings opacity layered transparency (applies alpha channel to surfaces)
- [x] StaticResource→DynamicResource conversion for live theme switching (Theme.xaml + SettingsWindow.xaml)
- [x] SettingsThemeService for runtime Application.Resources brush updates
- [x] Overlay scrollbar controls (show/hide, width 4-20px, opacity)
- [x] Toast suppression toggle (remove Windows toast after capture, WinRT only, safe on exit)
- [x] Settings window popup display mode (Window vs Popup anchored to toast corner on the taskbar monitor, auto-close option)
- [x] Settings window compact display mode (Shrink the width of the panel for a lighter footprint)
- [x] Settings Export & Import serialization preservation across app restarts
- [x] Per-app notification sounds (system sounds + custom WAV, per-app overrides)
- [x] Per-app notification icons (10 built-in vector presets, icon size slider 16-48px, per-app overrides)
- [x] IconService, SoundService, IconPreset, AppIconConverter
- [x] 16 new unit tests (M9.5 defaults, clone, deep-copy, JSON round-trip, IconPreset, SoundService, QueueManager NotificationAdded, ThemePreset settings colors)

### Milestone 11: Packaging & Final Polish
**Status: In Progress**

- [x] Security Strategy: Remove currently-tracked `.agents` directory (`git rm -r --cached .agents`).
- [x] Legal Strategy: Add an open-source `LICENSE` (e.g., MIT) to the repo root.
- [x] Legal Strategy: Add an explicit "Disclaimer of Liability" and "As-Is" warranty section to `README.md`.
- [x] Packaging Strategy: Implement an MSIX packaging project (`.wapproj`) to bundle the WPF app and grant native WinRT permissions.
- [x] Update Strategy: Configure an `.appinstaller` file with the MSIX to support automatic over-the-air client updates.
- [x] Final polish: settings popup now anchors to the Windows toast corner on the taskbar monitor (multi-monitor aware)
- [x] Final polish: settings popup position restore no longer overrides popup mode placement
- [x] Final polish: fullscreen overlay mode now renders true fullscreen (monitor bounds) without taskbar/work-area clipping
- [x] Final polish: settings UI now uses a professional left navigation layout for reliable tab access in popup mode
- [x] Final polish: settings default display mode changed to Popup for new installs/reset defaults
- [x] Final polish: overlay theme application can now be decoupled from settings-window theme via UI toggle
- [x] Final polish: settings tips and first-run guidance updated for current feature set
- [x] Final polish: reduced-motion handling now keeps reliable fade animations instead of silently disabling all transitions
- [x] Final polish: cramped per-app sound/icon rows cleaned up to prevent clipped labels
- [x] Final polish: default dark palette shifted to Windows-like neutral colors (overlay, settings UI, tray menu)
- [x] Final polish: surfaced hidden UI Styling color fields (surface light/hover + secondary/muted text)
- [x] Final polish: default visible notifications increased from 3 to 15 for better context retention
- [x] Final polish: overlay theme controls moved into Appearance; Profiles tab now focuses on full settings import/export
- [x] Final polish: timestamp controls now include size, display mode, weight, and color with live preview
- [x] Final polish: windowed settings title bar now uses immersive dark non-client styling (no light-gray chrome clash)
- [x] Final polish: UI theme preset dropdown now mirrors overlay theme names (built-in + custom) for consistent selection
- [x] Final polish: settings spacing pass applied for improved readability across panels
- [x] Final polish: section headers now render in boxed panels across settings tabs for faster visual scanning
- [x] Final polish: tray/settings icon updated to monochrome white badge with black "N" (purple removed)
- [x] Final polish: primary button text now auto-contrasts against accent color (fixes high-contrast readability)
- [x] Final polish: timestamp style controls moved to Appearance; Behavior keeps timestamp visibility toggle
- [x] Final polish: overlay theme apply now explicitly preserves settings-window theme when link toggle is off
- [x] Final polish: Position tab reorganized (monitor targeting + snapping + 3x3 quick presets) and size controls moved to dedicated Size tab
- [x] Final polish: settings tab content top-gap reduced and section/action control spacing tightened to reduce visual clutter
- [x] Final polish: keyword/presentation add-remove actions widened to prevent clipped button labels in Filtering/Streaming
- [x] Final polish: accessibility fallback now splits merged browser-hosted notifications (e.g., Reddit + X from Chrome) into separate cards
- [x] Final polish: popup panel height reduced (68% → 55% of work area) for a more proportionate panel on 1080p+
- [x] Final polish: settings window rounded corners with adjustable radius slider (0–20px) in UI Styling tab — XAML clipping in popup mode, DWM corner preference in windowed mode
- [x] Final polish: tooltips added to all UI Styling color picker labels and section headers
- [ ] Full manual test checklist pass
- [x] README finalization with screenshots
- [ ] Comprehensive onboarding / first-run experience

### UI/UX Audit & Polish Pass
**Status: Complete**

Comprehensive professional UI/UX review — 21 fixes covering spacing, consistency, visual hierarchy, and Help content:
- [x] Settings window height reduced (760→680)
- [x] Section header and setting label vertical rhythm improved
- [x] Color pickers standardized (swatch size, button labeling, live-update bindings)
- [x] Toggle row margins tightened (12→8px)
- [x] Redundant self-tooltip on section headers removed
- [x] Custom theme delete button properly labeled
- [x] List item button sizing and row margins unified
- [x] Add-keyword input row margins made consistent
- [x] Redundant Quick Position label removed
- [x] Conditional/nested settings visually indented
- [x] Help tab grids unified (column width, row spacing)
- [x] Help tab expanded with Settings Overview and extra troubleshooting
- [x] Overlay default size reduced for smaller displays
- [x] Tab panel corner radius unified (10→8px)
- [x] Tab content padding asymmetry fixed
- [x] UI Styling color picker spacing reduced
- [x] ComboBox bottom margins standardized
- [x] Per-keyword highlight colors with per-keyword color picker

### Milestone 12: Security Hardening & Code Quality
**Status: Complete**

Findings from comprehensive pre-release audit.

Security fixes (must fix before public release):
- [x] S1 (HIGH): Path traversal in custom icon loading — validate file paths stay within AppData icons/sounds directory in IconService.GetOrCreateCustomIcon()
- [x] S2 (MEDIUM): Settings import validation — add 1MB file size limit, validate numeric bounds, validate custom icon/sound paths in imported JSON
- [x] S3 (MEDIUM): Regex timeout — add timeout to keyword matching in QueueManager.MatchesAnyKeyword() to prevent hangs
- [x] S4 (MEDIUM): Hex parsing crash — use TryParse in ContrastHelper.ParseHexToRgb() instead of Convert.ToByte for malformed hex like "#GGGGGG"
- [x] S5 (MEDIUM): Add SetLastError=true to HotkeyManager P/Invoke declarations for better error diagnostics
- [x] S6 (LOW): Sanitize exception messages in NotificationListener StatusMessage to avoid exposing debug info

Code quality fixes (critical):
- [x] C1 (CRITICAL): Memory leak — timestamp DispatcherTimer in OverlayViewModel never stopped; store as field, stop on cleanup
- [x] C2 (CRITICAL): Memory leak — OverlayWindow subscribes to SettingsChanged in OnLoaded but never unsubscribes on close
- [x] C3 (HIGH): Crash guard — Screen.AllScreens[0] accessed without length check in App.xaml.cs popup positioning fallback
- [x] C4 (MEDIUM): SystemParameters.StaticPropertyChanged event handler in App.xaml.cs never unsubscribed
- [x] C5 (LOW): HwndSource not disposed in HotkeyManager (only set to null)

Release preparation:
- [x] Add SECURITY.md (privacy statement + security reporting instructions)
- [x] Add CONTRIBUTING.md
- [x] Add .gitattributes (line ending normalization)

### Milestone 13: Functionality & UX Improvements
**Status: Essential + High Value complete**

Post-release improvements identified by comprehensive audit.

Essential (before or shortly after release):
- [x] About dialog in tray menu (version, GitHub link, license, listener mode, .NET version)
- [x] Listener health status in tray tooltip ("Listening via WinRT/Accessibility" + paused/click-through state)
- [x] ~~WinRT auto-retry~~ — **removed**: RequestAccessAsync returns false-positive "Allowed" for unpackaged apps, causing auto-upgrade to silently kill the working accessibility hook. Manual retry via tray menu still works.
- [x] Streaming preset button — one-click OBS setup (chroma key ON, fixed window ON, tint ON)
- [x] Keyboard shortcut hints in tray menu items (e.g., "Show Overlay    Ctrl+Alt+N")

High value (v1.1):
- [x] Regex support for keyword matching (toggle per keyword — ".*" button on highlight and mute keywords)
- [x] ~~Search/filter overlay~~ — **reverted**: ICollectionView binding caused notification display to break over time. Overlay binds directly to ObservableCollection. Search UI remains but filtering is disabled pending a safer implementation.
- [x] Right-click card quick action: "Settings for this app..." opens settings on Filtering tab
- [x] Copy visible notifications to clipboard ("Copy All to Clipboard" in card context menu)
- [x] Drag cursor affordance on overlay resize edges (already handled by Win32 HTLEFT/HTRIGHT hit test)
- [x] Consolidate Position + Size tabs into "Layout" (single tab with Monitor, Quick Position, Size, Snapping, Startup)
- [x] Session-only in-memory notification archive (RAM-only, opt-in, bounded, never persisted, cleared on close)

Nice-to-have:
- [x] Notification grouping by app — toggle in Behavior tab, ICollectionView GroupDescriptions, themed group headers in overlay
- [x] Time-based theme switching — schedule day/night themes with configurable HH:mm times, 60-second polling timer
- [x] CLI arguments — --pause, --resume, --theme <name>, --send-test, --hide, --show (parsed in OnStartup after init)
- [x] Dyslexia-friendly font preset — bundled OpenDyslexic-Regular.otf, quick-select buttons in Appearance > Typography
- [x] Undo/redo for settings — Ctrl+Z/Y with 50-entry stack, undo/redo buttons in settings header
- [x] Named profiles — ProfileManager service (%AppData%\profiles\), save/load/delete UI in Profiles tab, tray menu submenu
- [x] Full keyboard navigation audit — tab mnemonics (Alt+A/B/F/L/O/R/Y/U/P/H), Escape closes, Cycle navigation on TabControl
- [x] Screen reader / AutomationProperties audit — AutomationProperties.Name on Window, TabControl, all TabItems, notification cards

### Milestone 14: Bug Fixes & Refinements
**Status: Complete**

Addressed post-release feedback to improve reliability and visual consistency:
- [x] Settings popup minimize to tray — added window controls (Minimize/Close) when settings are in default Popup mode.
- [x] Standardized MSIX app icons — regenerated white "N" logo for Start Menu tiles and splash screen to match the system tray icon.
- [x] Help tab layout fix — corrected margin alignment in the Privacy section of the Help tab.
- [x] Settings tab visual polish — replaced plain text headers with embedded Lucide SVG icons (no external scripts).
- [x] "Startup with Windows" package fix — migrated from Registry Run key to MSIX `desktop:StartupTask` API so the toggle works securely inside the container.
- [x] "Per-App Muting" initialization fix — fixed "No apps seen yet" by populating the known apps list immediately upon opening settings instead of waiting for a new notification.
- [x] Improved out-of-box defaults:
  - Animation speed increased to 1200ms.
  - Max visible notifications increased to 40.
  - Default overlay height dynamically matches the primary monitor's total height on first run.
- [x] UI scaling fixes for textboxes, comboboxes, and position buttons.
- [x] Keyword highlight layout realignment.
- [x] Added GitHub link to the Help tab.
- [x] Fixed Settings popup header buttons not receiving clicks due to title bar drag preview events.
- [x] Fixed title bar dragging by stripping the explicit `Cursor` logic to prevent move icon overriding native WPF cursors on Undo/Redo.
- [x] Fixed Settings Window squashing UI when toggling "Show Preview" by adding mathematical `SizeChanged` WPF event.
- [x] Fixed Preview failing to shrink the window automatically when collapsed due to WPF hiding SizeChanged quirk. 
- [x] Fixed `Undo` memory action failing to visibly update the UI by adding explicit C# reflection `OnPropertyChanged` loop for all inputs.
- [x] Settings UI structural overhaul — migrated layout controls to "Appearance", OS controls to new "System" tab, and functional configuration to "Behavior".
- [x] Text Alignment rendering controls (Left, Center, Right) for all Notification components.
- [x] Single-Line Banner mode natively renders the timestamp inline.
- [x] Detached "Replace Mode" so it can be used globally outside of single-line structures.
- [x] Settings information architecture pass — audit every tab, move misplaced controls into logical homes, fix the broken text-alignment path, repair the hotkey editor, and refresh in-app help/documentation to match the revised layout.

### Milestone 15: Voice Access & Accessibility Hardening
**Status: Complete**

- [x] Microsoft Voice Access card labels — added an opt-in Accessibility-tab setting with Off, Body Only, and Title + Body + Timestamp modes; verified cards expose meaningful UI Automation names instead of class names; documented the privacy/accessibility implications in Help and release notes.
- [x] Post-reorg diagnostics hardening — System tab now exposes in-app notification access recovery (open settings + retry check), and Accessibility now surfaces hotkey registration failures for invalid or already-taken combos.

### Milestone 16: Spoken Notifications
**Status: Complete**

- [x] Built-in spoken notification reader — Accessibility tab now includes on/off narration, Body Only vs Title + Body + Timestamp modes, installed-voice selection, speed/volume controls, Preview Voice playback, and privacy disclosures that explain nearby-audio exposure plus RAM-only handling.
- [x] Expanded spoken content combinations — added Title Only, Title + Body, Body + Timestamp, and Title + Timestamp modes so narration can be tuned more precisely without needing separate features.

### Milestone 17: Default UX & Grouping Polish
**Status: Complete**

- [x] Default settings alignment: unify the shipped first-run/reset defaults for visible-card count, animation timing, and related docs/example files so the out-of-box experience matches the intended UX.
- [x] Overflow affordance hardening: make the overflow badge actionable without retaining discarded notification content, and update the wording so it no longer implies hidden cards can be expanded later.
- [x] Grouping customization pass: move information density into Appearance, keep app-grouping behavior in its logical tab, and replace the hardcoded app banner with a properly styleable grouping presentation.

### Milestone 18: Stable Single-Panel Enhancements
**Status: Mostly complete**

Targeted rules and narration:
- [x] Field-scoped keyword rules: highlight and mute rules should target `Title`, `Body`, or `Title + Body`, preserve regex support, and work cleanly in the restored single-panel app.
- [x] Field-scoped narration triggers: keep the existing built-in narrator, but add optional title/body/title+body matching rules that can force `Read Aloud` or `Skip Read Aloud` with spoken-content overrides.
- [x] App-filtered rules: allow highlight, mute, and narration rules to be limited to one source app so browser-hosted services like X can be targeted precisely without affecting every notification.
- [x] Social/account targeting model: X/social workflows should support text-pattern targeting for handles, hashtags, account names, and watchwords using literal or regex rules with optional app filters; Windows still exposes text only, not structured account IDs.

Visual customisation:
- [x] Background image card mode: add optional image-backed notification cards for the single-panel app so each visible notification can render an image instead of a flat background colour, with privacy-safe local asset storage plus opacity, hue, and brightness controls.

Settings UX and persistence:
- [x] Quick tips control: add an explicit user-facing toggle so tray/settings first-run guidance can be turned off without relying on the one-time welcome-state flag alone.
- [x] Export/import coverage expansion: restored single-panel rule and background-image settings must round-trip through `AppSettings`, `SettingsViewModel`, export/import JSON, reset defaults, and regression tests.
- [x] Install/update persistence audit: traced the packaged app to the single `%AppData%\NotificationsPro\settings.json` store, then added a one-time startup settings-schema migration so legacy `3`-card / `300ms` / `480px` installs upgrade cleanly without overwriting current user choices on every launch.
- [x] Startup-defaults repair follow-up: bump the settings schema so already-stamped broken installs still self-repair from the old `3` visible notifications / `0-300ms` animation / `480px` height startup state on their next packaged launch.

Docs and release readiness:
- [x] README workflow guides: add dedicated sections for `Getting the Most Out of Notifications Pro`, `Getting the Most Out of X`, `Other Social Platforms`, and `Common Notification-Heavy Tools`, covering setup, browser-hosted app limitations, per-app narration, privacy limits, and troubleshooting for services such as X, Reddit, Instagram, Codex, and Antigravity in the single-panel app.
- [x] README/Help gap analysis: update README, in-app Help, example settings, and status text for advanced narration rules, card background images, voice setup, privacy disclosures, defaults, and troubleshooting without reintroducing multi-panel language.
- [x] Public repo sanitisation audit: reviewed the tracked tree and git history for secrets, local certificates, hardcoded signing material, proprietary/internal tooling references, author metadata, and publisher identity, and recorded the findings in `analysis/public-release-audit-2026-03-12.md`.
- [ ] Public release follow-through: rotate the exposed signing credential, decide whether to rewrite history to remove the old secret-bearing signing-script revision and local-path-heavy commit messages, and confirm whether the package publisher identity should stay public as-is.

### Milestone 19: Settings IA Cleanup & Advanced Image Controls
**Status: In progress**

Information architecture cleanup from the 2026-03-12 settings audit:
- [ ] Sidebar order and naming cleanup: Reorder the settings tabs to `Appearance / Behavior / Apps / Filtering / Layout / Sounds / Accessibility / Streaming / System / Settings Window / Profiles / Help`, and rename `UI Styling` to `Settings Window` if it remains the home for settings-window chrome/theme controls.
- [ ] Tab ownership cleanup: Move `Persistent Notifications` and `Auto-Duration` into `Behavior > Timing`, move `Per-App Speech` into `Apps`, move `Presentation Mode` into `Behavior`, move `Per-App Color Tinting` into `Appearance`, move `Always on Top` plus `Click-Through` into `Layout`, move `Show Quick Tips` into `Settings Window`, and relocate `Global Hotkeys` into a general controls/system home instead of leaving them under Accessibility.
- [ ] Appearance flow cleanup: Re-sequence `Appearance` so quick-start controls (`Themes`, `Custom Themes`, `Theme Schedule`, `Density Presets`) appear before detailed typography/timestamp/color/image controls, and keep the lower half for secondary styling groups such as grouping, icons, and scrollbar options.
- [x] Apps-tab cleanup: Make `Apps` the canonical per-app overrides surface by renaming the section to `Per-App Overrides`, moving per-app narration into it, adding app search plus `Only modified` filtering, and adding per-app clear/reset actions so large app lists stay manageable.
- [x] Apps card layout cleanup: rework the per-app override cards into a cleaner aligned form layout, keep long background-image paths out of the main visual flow, and make the card-background override actions read as one coherent control group instead of a cramped mixed row.
- [ ] Scope signposting and discoverability: Add lightweight “configured in …” cross-links or helper rows between split concepts such as timestamp visibility vs timestamp styling, app grouping behavior vs grouping appearance, and default icon vs per-app icon overrides without duplicating the controls themselves.
- [ ] Tooltip and inline-copy audit: Reduce non-critical inline guidance in dense tabs, keep privacy/access/live-status text inline where required, and move general usage hints into tooltips so sections stay readable.
- [x] Accessibility disclosure cleanup: moved the built-in narration and Voice Access transparency/privacy copy out of the main Accessibility layout and into the relevant tooltips, while keeping the fuller disclosures in Help/README.

Control-system and visual-rhythm cleanup from the 2026-03-12 layout audit:
- [ ] Shared form-control normalization: Split the current monospaced text-box style into semantic input styles (`general text` vs `structured value`), standardize control heights, and make sure normal text entry fields such as theme names, profile names, app filters, and general keyword inputs no longer inherit a technical monospace look.
- [ ] Button tier cleanup: Define explicit shared button tiers for footer actions, standard form actions, compact inline actions, and token/chip actions so `Apply`, `Browse`, `Clear`, `Remove`, `Pick`, regex toggles, and list-row buttons all look related instead of individually sized.
- [ ] Spacing-token cleanup: Normalize section spacing, field-label spacing, row spacing, nested-indent spacing, and first-control-after-header spacing so tabs stop relying on ad hoc local margin overrides that make some rows feel crushed and others too loose.
- [ ] Slider-row consistency pass: Ensure slider sections consistently show current values where that helps comprehension, remove the remaining one-off slider rows that lack mirrored values, and align slider/value spacing across Appearance, Behavior, Accessibility, Layout, and System.
- [ ] Repeated-card layout cleanup: Replace the improvised per-item card internals in `Filtering`, `Apps`, and similar list-heavy areas with one canonical repeated-card pattern that has a compact header, proper sub-field labels, and a consistent inline action cluster.
- [ ] Responsive action-row cleanup: Replace cramped fixed-width or hard-column action rows with responsive patterns for compact mode, especially for width/height presets, add/remove rows, and browse/clear rows so the popup layout still looks deliberate at the smaller width.

Background image and advanced visual-control gaps:
- [x] Card background image treatment expansion: Add missing image-processing controls for notification cards, including at minimum `Black & White`, `Saturation`, and `Contrast`, while keeping existing `Fit`, `Placement`, `Opacity`, `Hue`, and `Brightness` controls coherent and export/import safe.
- [x] Explicit card background mode: Add a clearer `Solid / Image` card-background mode so image-backed cards are an explicit styling decision instead of something users infer from whether an image path happens to be populated.
- [x] Portrait-image handling pass: Add a practical portrait-safe crop/anchor option (for example `Top / Center / Bottom`) if current fit modes still crop tall artwork poorly in wide cards, and verify text readability across the built-in density presets.
- [ ] Background-image control consistency: Align the control model between the global card background, per-app card background overrides, and fullscreen backdrop backgrounds so the same concepts (`Fit`, `Placement`, `Image Treatment`) behave consistently without forcing every surface to expose every option.
- [x] Single-line banner compatibility guard: Keep banner mode on solid backgrounds only, but make that constraint explicit in the UI/help so users understand why card background images do not apply there.
- [ ] Notification-style gap review: Evaluate a short list of additional high-value card-style controls during the redesign, especially `Timestamp Placement`, `Icon Placement`, and simple background-treatment presets, while explicitly avoiding expensive styling features that would regress rendering performance.

Operational gaps:
- [x] Real listener diagnostics: Add a `Run Capture Diagnostic` or equivalent action under `System > Notification Access` so users can distinguish overlay rendering issues from live WinRT/accessibility capture failures.
- [ ] Regression hardening for settings moves: Extend tests to cover moved settings, settings defaults, export/import, reset defaults, and tab-navigation links so another IA pass does not quietly break persistence or discoverability.

Post-redesign process guardrails:
- [ ] Create a local `settings-ia-review` skill after the new layout is finalized: document the canonical tab ownership, approved tab order, and “do not duplicate controls across tabs” rules so future feature work follows the agreed information architecture.
- [ ] Create a local `ui-form-rhythm-review` skill after the new layout is finalized: document the shared control tiers, spacing tokens, typography rules, inline action patterns, and compact-mode responsiveness checks so future UI changes preserve alignment and visual consistency.
- [ ] Create a local `settings-regression-checklist` skill after the new layout is finalized: require a focused pass over spacing, control heights, button widths, tooltip usage, moved-setting persistence, and export/import coverage whenever a settings feature is added or reorganized.

## Current Focus
Next planned pass: execute the settings IA cleanup from the 2026-03-12 audit while still completing the remaining release-readiness work (MSIX settings-persistence audit and public-repo sanitisation).

## Blocked
- UserNotificationListener may not deliver notifications for unpackaged desktop apps even when reporting "Allowed". May need MSIX packaging (Milestone 11) to fully resolve.
