# Changelog

## Unreleased

### Added
- Slide-in direction setting: notifications can now enter from Left, Right, Top, or Bottom (Behavior > Animations)
- Click to dismiss: click any notification card to immediately remove it
- Hover to pause: mouse over the overlay pauses all expiry timers; moving away resumes them
- Right-click context menu on cards: Dismiss, Copy Text, Clear All
- Context menu themed to match current overlay colors (background, text, border)
- "Clear All Notifications" tray menu item to remove all visible cards at once
- Optional relative timestamps on notification cards ("just now", "2m ago") with 15-second refresh
- Show Timestamp toggle in Settings > Behavior > Content
- Per-field font size controls: independent sliders for App Name, Title, and Body text
- Per-field font weight controls: independent dropdowns for App Name, Title, and Body text
- Card border controls: toggle, color picker, and thickness slider (Appearance > Card Border)
- Accent stripe controls: toggle to show/hide and thickness slider (Appearance > Accent Stripe)
- Card gap slider to control vertical spacing between notification cards
- Outer margin slider to control spacing around the card area inside the overlay
- Deduplication controls: on/off toggle and adjustable time window (Behavior > Deduplication)
- Overflow badge now inherits card theme colors (background, text, font, corner radius)
- Tray menu toggle for click-through mode ("Disable Click-Through (Allow Dragging)")
- Click-through state surfaced in tray tooltip for faster troubleshooting
- Behavior settings for:
  - max visible notifications (1-40)
  - per-field display toggles (app name, title, body)
- In-memory notification model now carries source app name separately from title/body for richer card layout
- One-line banner mode toggle for compact, long-strip notification cards
- Settings color picker buttons for title/body/app/background/accent colors
- Dedicated app-name color setting (separate from notification title/body colors)
- Settings header now shows the app's tray icon for stronger visual identity
- Optional manual overlay edge resize (left/right) with persisted width
- Per-field stacked-mode line count controls (app name, title, body)
- Tray quick toggle for Always on Top
- Stacked-mode "Limit Text Lines (Truncate)" toggle for full wrapped text when disabled
- Stacked-order toggle so notifications can flow newest-on-top or newest-on-bottom
- Single-line "Wrap Banner Text" toggle for preserving long banner text on smaller screens
- Single-line wrapped banner "Max Lines" setting to control card height/density
- Single-line "Auto Full-Width Banner" toggle for near edge-to-edge monitor-width banners
- Position preset buttons (top/side anchors) in Settings > Position
- Overlay-width preset buttons for 1080p, 2K, 4K, and 8K display classes
- Overlay-height preset buttons for 1080p, 2K, 4K, and 8K display classes

### Changed
- Existing "Font Size" and "Font Weight" labels renamed to "Body Font Size" and "Body Font Weight" for clarity
- ShowBorder default changed to false (border is now opt-in)
- BorderColor default changed from accent purple (#7C5CFC) to subtle dark (#363650)
- ScrollViewer max height in overlay now bound to OverlayMaxHeight setting (was hardcoded 600px)
- Accessibility fallback now listens to a broader WinEvent range and uses shell-host + toast-size heuristics instead of strict class/object filters
- Overlay drag path now retries HWND hook attach on load and keeps a WPF `DragMove` fallback if hook attachment is unavailable
- Overlay cards now apply configured line spacing to both title and body text for readability
- WinRT + accessibility text shaping now separates app name from content more reliably before rendering
- Font size range increased (up to 56px) for accessibility
- Animation controls expanded with fade-only mode and a wider speed range (0-1200ms)
- Settings save path now preserves live manually-resized overlay width unless width control was explicitly changed
- Overlay snapping/clamping now uses the active monitor work area instead of primary-only metrics
- Overlay scroll behavior now auto-shows vertical scrolling only when content exceeds Max Overlay Height
- Settings window now uses the same generated tray icon in the window title-bar icon slot
- One-line banner text shaping now prioritizes preserving title context before body preview
- Visible notifications range increased from 1-8 to 1-40
- Preview/test action now applies pending settings immediately before enqueuing preview cards
- Max Overlay Height range increased from 1200px to 4320px for high-resolution displays
- Overlay Width range increased from 1400px to 7680px for high-resolution displays

### Fixed
- Drag lockout diagnosability: click-through state is now obvious and quickly reversible from tray menu
- Accessibility mode diagnostics now report captured/candidate/event counters to show whether shell events are being seen
- Notification body text visibility bug (body was bound through a boolean converter incorrectly)
- Faint transparent panel under cards and persistent tiny scrollbar artifacts in the overlay
- Expiry removal now respects animation disablement (instant removal when animations are off)
- Single-line mode regression where changing font size could revert overlay width
- Stacked-mode readability issue where users could remain effectively constrained to one-line cards after leaving single-line mode
- Snap-to-edge behavior failing on secondary monitors
- Long stacked notifications appearing cut off because overflow had no visible scroll affordance
- Click-through mode intercepting mouse hit-tests instead of fully passing through
- Right-edge anchoring during width changes could drift off the monitor edge in banner mode
- Overlay max-height behavior now clamps against monitor work area to avoid off-screen clipped cards
- Overlay scrollbar usability at large font sizes improved (wider scrollbar + explicit content scrolling)
- Overlay now constrains internal scroll region to effective monitor-aware max height, preventing bottom card clipping
- Single-line full-width mode now preserves and restores manual width state when toggled off
- Preview/test notifications now generate unique content to avoid dedup hiding expected card counts
- Single-line text color regression where title/app segments could ignore configured color choices
- Stacked-mode density regression when leaving single-line mode with wrapped content (oversized cards + premature scrollbar)

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
