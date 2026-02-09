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
- [ ] Align timing with system toast duration when available (deferred — using configurable duration)

### Milestone 3: Full Customization + Multi-Monitor
- [ ] Multi-monitor support (select monitor, move via tray menu)
- [ ] Per-monitor DPI awareness
- [ ] Click-through toggle (Win32 WS_EX_TRANSPARENT)
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
Milestone 2 bug fixes complete. Ready to move to Milestone 3 — full customization, snapping, multi-monitor handling, click-through.

## Blocked
- UserNotificationListener may not deliver notifications for unpackaged desktop apps even when reporting "Allowed". May need MSIX packaging (Milestone 4) to fully resolve.
