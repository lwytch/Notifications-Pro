# Plan

## Milestones

### Milestone 1: Core App — Tray + Overlay + Settings + Preview
**Status: In Progress**

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
- [ ] Verify everything works end-to-end with manual testing

### Milestone 2: Real Notification Capture
- [ ] Implement UserNotificationListener API
- [ ] Request notification access permission
- [ ] Wire live notifications into QueueManager
- [ ] Align timing with system toast duration when available
- [ ] Privacy validation: confirm no content leaves RAM
- [ ] Document API limitations (which notifications can/can't be captured)

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
Milestone 1 — verifying the skeleton builds and runs correctly.

## Blocked
Nothing currently blocked.
