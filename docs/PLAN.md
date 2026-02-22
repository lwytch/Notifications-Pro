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
- [x] StaticResource→DynamicResource conversion for live theme switching (Theme.xaml + SettingsWindow.xaml)
- [x] SettingsThemeService for runtime Application.Resources brush updates
- [x] Overlay scrollbar controls (show/hide, width 4-20px, opacity)
- [x] Toast suppression toggle (remove Windows toast after capture, WinRT only, safe on exit)
- [x] Settings window popup display mode (Window vs Popup anchored to toast corner on the taskbar monitor, auto-close option)
- [x] Per-app notification sounds (system sounds + custom WAV, per-app overrides)
- [x] Per-app notification icons (10 built-in vector presets, icon size slider 16-48px, per-app overrides)
- [x] IconService, SoundService, IconPreset, AppIconConverter
- [x] 16 new unit tests (M9.5 defaults, clone, deep-copy, JSON round-trip, IconPreset, SoundService, QueueManager NotificationAdded, ThemePreset settings colors)

### Milestone 11: Packaging & Final Polish
**Status: In Progress**

- [ ] MSIX packaging or installer
- [ ] Clean uninstall (optionally remove %AppData% settings)
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
- [ ] Full manual test checklist pass
- [ ] README finalization with screenshots
- [ ] Comprehensive onboarding / first-run experience

## Current Focus
Milestone 11 final polish is in progress (notification split hardening + Position/Size UI cleanup complete). Next: packaging/installer and full manual QA pass.

## Blocked
- UserNotificationListener may not deliver notifications for unpackaged desktop apps even when reporting "Allowed". May need MSIX packaging (Milestone 11) to fully resolve.
