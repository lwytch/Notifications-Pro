# Plan

## Milestones

### Milestone 1: Core App — Tray + Overlay + Settings + Preview
**Status: Complete**

- [x] Solution structure (.NET 8 WPF)
- [x] AppSettings model with defaults
- [x] SettingsManager (load/save %AppData%\NotificationsPro\settings.json)
- [x] QueueManager (max 3 visible, overflow count, dedup, expiry)
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
- [x] Optional relative timestamps on cards ("just now", "2m ago") — refreshed every 15s
- [x] Show/hide timestamp toggle in settings (Behavior > Content)

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
- [x] Ship 6 presets: "Dark Purple" (default), "Dark Neutral", "Light", "Frosted Glass", "High Contrast", "Minimal"
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
- [x] Respect Windows "Reduce motion" setting (`SystemParameters.ClientAreaAnimation`) — auto-disable animations when system says so
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
- [x] First-run tray balloon — `NotifyIcon.ShowBalloonTip()` on first launch: "Notifications Pro is running. Right-click here for settings."
- [x] First-run tip bar in settings — dismissable info bar on first open: "Drag the overlay to reposition it."
- [x] Track `HasShownWelcome` in AppSettings (UI state, not notification content)

Settings window polish:
- [x] Confirm before "Reset to Defaults" — MessageBox confirmation to prevent accidental resets
- [x] "Saved" micro-feedback — brief "Saved" label appears next to "Changes are saved automatically" on each save (1.5s auto-hide)
- [x] Remember settings window position between opens (save to AppSettings)
- [x] Ctrl+T keyboard shortcut to send test notification from settings

Tray icon enhancements:
- [x] Checkmarks on tray menu toggle items (Pause, Always on Top, Click-Through)
- [x] Tray icon state variants — dimmed/monochrome when paused
- [x] Notification count badge on tray icon (active card count, red badge in bottom-right)
- [x] "Clear All Notifications" in tray menu (already existed from M4)

### Milestone 9: System Integration & Multi-Monitor
**Status: Not Started**

- [ ] Start with Windows toggle (registry `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` or Task Scheduler)
- [ ] Global hotkeys via `RegisterHotKey` Win32 API (configurable bindings for toggle overlay, dismiss all, toggle DND)
- [ ] Hotkey configuration UI in settings
- [ ] Multi-monitor picker in Position tab (ComboBox populated with `Screen.AllScreens` names)
- [ ] Move overlay to selected monitor with one click
- [ ] Per-monitor DPI awareness (handle `DpiChanged`, re-constrain position and width)
- [ ] Monitor-aware position presets (top-left of monitor 2, etc.)

### Milestone 10: Streaming & Presentation (Stretch)
**Status: Not Started**

Features for streamers, presenters, and content creators.

- [ ] Chroma key background option — solid green/blue/magenta background for OBS chroma key capture
- [ ] OBS-friendly fixed window mode — non-transparent, fixed-size window that captures cleanly in OBS window capture
- [ ] Presentation mode — auto-enable DND when a specific app is fullscreen (PowerPoint, Zoom, etc.)
- [ ] Per-app color tinting — optional subtle background tint derived from app name hash (e.g., Teams = blue, Slack = purple)

### Milestone 11: Packaging & Final Polish
**Status: Not Started**

- [ ] MSIX packaging or installer
- [ ] Clean uninstall (optionally remove %AppData% settings)
- [ ] Final polish: edge cases, error resilience
- [ ] Full manual test checklist pass
- [ ] README finalization with screenshots
- [ ] Comprehensive onboarding / first-run experience

## Current Focus
Milestone 8 UX polish complete. Next: Milestone 9 system integration, or Milestone 10 streaming.

## Blocked
- UserNotificationListener may not deliver notifications for unpackaged desktop apps even when reporting "Allowed". May need MSIX packaging (Milestone 11) to fully resolve.
