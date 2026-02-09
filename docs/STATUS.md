# Status

## What Works
- Solution builds successfully (.NET 8 WPF)
- System tray icon with dark-themed context menu (Show/Hide, Pause/Resume, Settings, Quit)
- Overlay window: transparent, always-on-top (configurable), draggable, resizable, slide-in animations
- Settings window: three tabs (Appearance, Behavior, Position) with premium dark theme
- QueueManager: max 3 visible, overflow count, deduplication, expiry timers
- SettingsManager: load/save to %AppData%\NotificationsPro\settings.json
- Preview/test notification button sends mock notifications to the overlay
- Settings auto-save with 500ms debounce
- 21 unit tests covering QueueManager, SettingsManager, and SnapHelper

## What Doesn't Work Yet
- Real notification capture (Milestone 2)
- Multi-monitor selection (Milestone 3)
- Click-through mode (code exists but needs testing)
- Installer/packaging (Milestone 4)

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
- Notification capture uses mock data only (real capture in Milestone 2)
- Multi-monitor support not yet implemented
- No installer — run from source or publish manually
