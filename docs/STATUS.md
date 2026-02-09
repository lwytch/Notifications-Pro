# Status

## What Works
- Solution builds successfully (.NET 8 WPF, targets Windows 10 SDK 19041)
- System tray icon with dark-themed context menu (Show/Hide, Pause/Resume, Settings, Quit)
- Overlay window: transparent, always-on-top (configurable), draggable, slide-in animations
- Overlay size now fits visible cards (no persistent faint backdrop below cards)
- Overlay supports optional one-line banner mode for compact layouts
- Overlay supports manual left/right edge resizing (when enabled)
- Overlay drag reliability improved with deterministic HWND hook attach (+ WPF drag fallback if hook is unavailable)
- Settings window: three tabs (Appearance, Behavior, Position) with premium dark theme
- Settings header now includes the same app icon used in the system tray
- Behavior tab includes:
  - configurable visible card count (1-8)
  - content field toggles (show app name, title, body)
  - single-line banner toggle
  - fade-only animation toggle and wider animation speed range (0-1200ms)
- Appearance tab includes color picker buttons and separate app-name color customization
- Font size range increased for accessibility (up to 56px)
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
- 31 unit tests covering QueueManager, SettingsManager (with round-trip and corruption), and SnapHelper

## What Doesn't Work Yet
- Multi-monitor selection (Milestone 3)
- Click-through mode (code exists but needs testing)
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
- [ ] "Single-Line Banner Mode" compacts each notification into one line
- [ ] App Name Color applies independently from title/body text colors
- [ ] "Pick" buttons open a color chooser and update hex values
- [ ] Font size slider changes overlay text size in real time
- [ ] Font size can be increased to larger accessibility values (up to 56px)
- [ ] Background opacity slider changes overlay transparency
- [ ] Overlay no longer shows a faint empty panel under cards
- [ ] Overlay can be resized from left/right edges when manual resize is enabled
- [ ] Fade-only animation option removes horizontal motion
- [ ] Toggle switches animate smoothly
- [ ] "Hide Overlay" / "Show Overlay" toggles the overlay window
- [ ] "Pause Notifications" / "Resume Notifications" pauses/resumes
- [ ] Overlay is draggable by clicking and dragging
- [ ] Overlay snaps to screen edges
- [ ] "Reset to Defaults" restores all settings
- [ ] "Quit" closes everything cleanly
- [ ] Settings persist after restart

## Known Limitations
- Multi-monitor support not yet implemented
- No installer — run from source or publish manually
- Unpackaged desktop apps may have limited UserNotificationListener support
