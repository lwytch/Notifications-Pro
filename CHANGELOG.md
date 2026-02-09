# Changelog

## Milestone 2 — Real Notification Capture

### Added
- NotificationListener service using UserNotificationListener API
- Auto-permission request on first launch (Windows prompts user)
- Real-time notification detection via NotificationChanged event
- Toast text extraction (title + body) forwarded to QueueManager
- Startup seeding: existing notifications marked as seen (not displayed)
- Seen ID trimming at 5000 entries to prevent unbounded memory growth
- Status indicator in tray context menu (shows listener state)
- Tray icon tooltip updates to reflect access status

### Changed
- Target framework updated to net8.0-windows10.0.19041.0 for WinRT API access
- SettingsManager now accepts custom path (testable with isolated temp directories)
- OverlayLeft/OverlayTop changed from double.NaN to nullable double for JSON compatibility

### Fixed
- Settings save silently failing due to double.NaN serialization
- SettingsManager tests now isolated from user's real settings file

## Milestone 1 — Core App Skeleton

### Added
- .NET 8 WPF solution structure
- System tray app with purple "N" icon and dark-themed context menu
- Overlay window: transparent, always-on-top (configurable), draggable, edge-snapping
- Notification cards: slide-in from left, fade-out on expiry, left accent border
- QueueManager: max 3 visible notifications, overflow count (+N more), deduplication, expiry timers
- SettingsManager: load/save from %AppData%\NotificationsPro\settings.json
- Settings window with three tabs:
  - **Appearance**: font family, size, weight, line spacing, colors (title/body/background/accent), opacity, corner radius, padding
  - **Behavior**: notification duration, always-on-top toggle, click-through toggle, animation toggle and speed
  - **Position**: overlay width, max height, snap-to-edges toggle and distance
- Premium dark theme with custom styles for buttons, toggle switches, sliders, combo boxes, tabs, scrollbars, and tooltips
- Preview/test notification button with rotating sample content
- Reset to Defaults button
- Auto-save with 500ms debounce
- Unit tests: QueueManager (12 tests), SettingsManager (7 tests), SnapHelper (7 tests)
- Documentation: PLAN.md, STATUS.md, ARCHITECTURE.md, CLAUDE.md, AGENTS.md
