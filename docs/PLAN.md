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

### Milestone 3: Full Customization + Multi-Monitor
- [ ] Multi-monitor support (select monitor, move via tray menu)
- [ ] Per-monitor DPI awareness
- [ ] Click-through toggle (Win32 WS_EX_TRANSPARENT)
- [x] Tray quick toggle for click-through recovery (avoid drag lockout)
- [x] Notification content controls (show/hide app name, title, body)
- [x] Configurable max visible notifications (1-8)
- [x] Overlay auto-size polish (removes ghost backdrop panel and stray scrollbar)
- [x] One-line banner mode (compact per-notification display)
- [x] App-name color customization (separate from title/body colors)
- [x] Built-in color picker buttons in Settings UI
- [x] Manual edge resize support (left/right drag handles)
- [x] Animation style controls (fade-only + extended speed range)
- [x] Tray icon preview shown in Settings header
- [ ] Edge snapping on all monitors
- [ ] Border toggle and customization
- [ ] Start with Windows toggle
- [ ] Hotkey support (optional)

### Milestone 4: Packaging + Polish
- [ ] MSIX packaging or installer
- [ ] Clean uninstall (optionally remove %AppData% settings)
- [ ] Final polish: edge cases, error resilience
- [ ] Full manual test checklist pass
- [ ] README finalization with screenshots placeholders

## Current Focus
Milestone 3 customization pass is underway. Next up: multi-monitor support, per-monitor DPI handling, and startup/launch polish.

## Blocked
- UserNotificationListener may not deliver notifications for unpackaged desktop apps even when reporting "Allowed". May need MSIX packaging (Milestone 4) to fully resolve.
