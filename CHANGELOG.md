# Changelog

## Milestone 1 — Core App Skeleton (In Progress)

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
