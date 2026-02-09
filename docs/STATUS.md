# Status

## What Works
- Solution builds successfully (.NET 8 WPF, targets Windows 10 SDK 19041)
- System tray icon with dark-themed context menu (Show/Hide, Pause/Resume, Settings, Quit)
- Overlay window: transparent, always-on-top (configurable), draggable, slide-in animations
- Overlay drag uses PreviewMouseLeftButtonDown (tunneling event) to avoid ScrollViewer consuming mouse events
- Settings window: three tabs (Appearance, Behavior, Position) with premium dark theme
- QueueManager: max 3 visible, overflow count, deduplication, expiry timers
- SettingsManager: load/save to %AppData%\NotificationsPro\settings.json (isolated temp dirs for tests)
- Preview/test notification button sends mock notifications to the overlay
- Settings auto-save with 500ms debounce
- **Real notification capture** via UserNotificationListener API
  - Subscribes to NotificationChanged event + polling fallback (every 2s)
  - Polling guard prevents overlapping async polls
  - Extracts title + body from toast notifications
  - Seeds existing notifications on startup (marks as seen, doesn't display old ones)
  - Trims seen ID set at 5000 to prevent unbounded growth
  - Status indicator in tray context menu with diagnostic info (captured count, system count, poll #)
  - "Grant Notification Access" always visible in tray menu
  - "Retry Access Check" works even when access already granted (for troubleshooting)
- No DropShadowEffect on notification cards (causes severe WPF perf issues with AllowsTransparency)
- 29 unit tests covering QueueManager, SettingsManager (with round-trip and corruption), and SnapHelper

## What Doesn't Work Yet
- Multi-monitor selection (Milestone 3)
- Click-through mode (code exists but needs testing)
- Installer/packaging (Milestone 4)
- Toast duration alignment (using configurable duration instead)

## Known Issues / Troubleshooting
- **UserNotificationListener** may report "Allowed" but not deliver notifications for unpackaged apps. Workaround: open Windows Settings > Notifications, ensure the app has access. Use "Retry Access Check" in the tray menu to restart the listener.
- The tray status line shows diagnostic info (e.g. "Listening — 0 captured, 12 in system (poll #5)") to help debug whether the API can see notifications at all.

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
- [ ] Font size slider changes overlay text size in real time
- [ ] Background opacity slider changes overlay transparency
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
