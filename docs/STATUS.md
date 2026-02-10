# Status

## What Works
- Solution builds successfully (.NET 8 WPF, targets Windows 10 SDK 19041)
- System tray icon with dark-themed context menu (Show/Hide, Pause/Resume, Settings, Quit)
- Overlay window: transparent, always-on-top (configurable), draggable, slide-in animations
- Overlay size now fits visible cards (no persistent faint backdrop below cards)
- Overlay supports optional one-line banner mode for compact layouts
- One-line mode supports optional wrapped text to avoid truncation on smaller monitors
- One-line mode supports optional auto full-width behavior per monitor
- One-line mode includes a wrapped-banner max-lines control to keep more cards visible at large font sizes
- Overlay supports stacked order control (newest on top or newest on bottom)
- Overlay supports manual left/right edge resizing (when enabled)
- Overlay width now preserves live manual resize when changing unrelated settings (e.g., font size)
- Stacked mode now supports full multi-line word wrap with optional truncation toggle
- Long wrapped content now remains accessible via auto vertical scrolling when it exceeds max overlay height
- Overlay applies monitor-aware effective max height to avoid off-screen clipping
- Overlay drag reliability improved with deterministic HWND hook attach (+ WPF drag fallback if hook is unavailable)
- Snap-to-edges now uses the active monitor work area (secondary monitor snapping works)
- Resizing while near the right edge now keeps the right edge anchored/snapped more reliably
- Click-through hit testing now returns transparent hit results so mouse input passes through consistently
- Settings window: three tabs (Appearance, Behavior, Position) with premium dark theme
- Settings header now includes the same app icon used in the system tray
- Settings window now uses the app tray icon in the title-bar icon slot
- Behavior tab includes:
  - configurable visible card count (1-40)
  - content field toggles (show app name, title, body)
  - full-wrap stacked mode toggle (disable line clamping/truncation)
  - per-field line count controls for app/title/body in stacked mode
  - single-line banner toggle
  - single-line wrap toggle, wrapped-banner max-lines control, and auto full-width toggle
  - stacked-order toggle (newest at top vs newest at bottom)
  - fade-only animation toggle and wider animation speed range (0-1200ms)
  - stacked-only text-limit controls (hidden while single-line mode is enabled)
- Position tab includes quick preset buttons for top/side placement
- Appearance tab includes color picker buttons and separate app-name color customization
- Font size range increased for accessibility (up to 56px)
- Tray menu includes quick toggles for click-through and always-on-top states
- QueueManager: configurable max visible count, overflow count, deduplication, expiry timers
- SettingsManager: load/save to %AppData%\NotificationsPro\settings.json (isolated temp dirs for tests)
- Preview/test notification button sends mock notifications to the overlay
- Settings auto-save with 500ms debounce
- **Real notification capture** via UserNotificationListener API
  - Subscribes to NotificationChanged event + polling fallback (every 2s)
  - Polling guard prevents overlapping async polls
  - Extracts app/title/body text from toast notifications with app-name filtering for better title/body fidelity
  - Seeds existing notifications on startup (marks as seen, doesn't display old ones)
  - Trims seen ID set at 5000 to prevent unbounded growth
  - Status indicator in tray context menu with diagnostic info (captured count, system count, poll #)
  - "Grant Notification Access" always visible in tray menu
  - "Retry Access Check" works even when access already granted (for troubleshooting)
- Accessibility fallback capture path improved for Windows 11:
  - listens to a broader accessibility event range
  - removes overly strict window/object filters
  - filters by shell host process + toast-like window size
  - surfaces live diagnostics (captured/candidate/event counters + last class seen)
- Tray menu includes a click-through toggle so drag can be restored quickly without opening settings
- No DropShadowEffect on notification cards (causes severe WPF perf issues with AllowsTransparency)
- 33 unit tests covering QueueManager, SettingsManager (with round-trip and corruption), and SnapHelper

## What Doesn't Work Yet
- Multi-monitor selection (Milestone 3)
- Installer/packaging (Milestone 4)
- Toast duration alignment (using configurable duration instead)

## Known Issues / Troubleshooting
- **UserNotificationListener** may report "Allowed" but not deliver notifications for unpackaged apps. Workaround: open Windows Settings > Notifications, ensure the app has access. Use "Retry Access Check" in the tray menu to restart the listener.
- If overlay cannot be dragged, check click-through state first. Click-through intentionally blocks mouse interaction; use tray menu item "Disable Click-Through (Allow Dragging)".
- The tray status line shows diagnostic info (WinRT poll metrics or accessibility captured/candidate/event counters) to help debug listener visibility.

## How to Test

### Build and Run
```bash
dotnet build
dotnet run --project src/NotificationsPro
```

### Run Tests
```bash
dotnet test
```

### Manual Test Checklist
- [ ] App starts and tray icon appears
- [ ] Right-click tray icon shows dark context menu
- [ ] "Settings..." opens settings window
- [ ] "Send Test Notification" shows a notification card in the overlay
- [ ] Notification slides in from the left
- [ ] Notification fades out after configured duration
- [ ] Sending 4+ test notifications shows "+N more" overflow indicator
- [ ] "Visible Notifications" slider changes how many cards persist on screen
- [ ] "Show App Name / Show Title / Show Body Text" toggles update cards immediately
- [ ] "Limit Text Lines (Truncate)" off shows full multi-line wrapped text in stacked mode
- [ ] App/Title/Body line-count sliders constrain stacked card text height
- [ ] "Single-Line Banner Mode" compacts each notification into one line
- [ ] In single-line mode, "Wrap Banner Text" preserves long content without truncating critical text
- [ ] In single-line mode, "Banner Max Lines" controls wrapped card height and visible card density
- [ ] In single-line mode, "Auto Full-Width Banner" expands to near monitor width automatically
- [ ] Stacked-mode controls hide when single-line mode is enabled
- [ ] "Newest on Top" toggle changes stacked order immediately
- [ ] App Name Color applies independently from title/body text colors
- [ ] "Pick" buttons open a color chooser and update hex values
- [ ] Font size slider changes overlay text size in real time
- [ ] Font size can be increased to larger accessibility values (up to 56px)
- [ ] Background opacity slider changes overlay transparency
- [ ] Overlay no longer shows a faint empty panel under cards
- [ ] Very long stacked notifications can be scrolled when they exceed Max Overlay Height
- [ ] At max font sizes, overlay scrollbar remains usable and no cards are clipped off-screen
- [ ] Overlay can be resized from left/right edges when manual resize is enabled
- [ ] While right-aligned, width changes keep right edge snapped to the monitor edge
- [ ] Changing font size does not reset manually resized overlay width in single-line mode
- [ ] Fade-only animation option removes horizontal motion
- [ ] Toggle switches animate smoothly
- [ ] "Hide Overlay" / "Show Overlay" toggles the overlay window
- [ ] "Pause Notifications" / "Resume Notifications" pauses/resumes
- [ ] Overlay is draggable by clicking and dragging
- [ ] Overlay snaps to screen edges on primary and secondary monitors
- [ ] Click-through mode allows clicking through the overlay to underlying apps
- [ ] Position preset buttons move overlay to expected top/side anchors
- [ ] "Reset to Defaults" restores all settings
- [ ] "Quit" closes everything cleanly
- [ ] Settings persist after restart

## Known Limitations
- Multi-monitor support not yet implemented
- No installer — run from source or publish manually
- Unpackaged desktop apps may have limited UserNotificationListener support
