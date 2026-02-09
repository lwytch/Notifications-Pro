# Architecture

## Overview

Notifications Pro is a C# .NET 8 WPF application that runs as a system tray app. It captures Windows toast notifications and displays their text in a customizable always-on-top overlay window.

## Components

```
App.xaml.cs (Entry Point)
├── SettingsManager         Load/save settings from %AppData%\NotificationsPro\settings.json
├── QueueManager            In-memory notification queue (max 3 visible + overflow count)
├── OverlayWindow           Transparent always-on-top WPF window
│   └── OverlayViewModel    Binds queue state and appearance settings to the overlay
├── SettingsWindow           Tabbed settings UI (Appearance / Behavior / Position)
│   └── SettingsViewModel   Binds settings with debounced auto-save
└── TrayIcon (NotifyIcon)   System tray with dark-themed context menu
```

## Notification Capture

Uses `Windows.UI.Notifications.Management.UserNotificationListener` API:
- User-level permission (no admin required) — prompts on first run
- Sanctioned Microsoft API for reading toast notifications
- Subscribes to `NotificationChanged` event for real-time detection
- On each event, calls `GetNotificationsAsync()` and diffs against seen IDs
- Seen IDs (`HashSet<uint>`) are system-generated, contain no notification content
- Set is trimmed at 5000 entries to prevent unbounded growth
- On startup, existing notifications are seeded as "seen" (not displayed)
- Some system notifications may not be captured (documented limitation)

## Overlay Rendering

- `WindowStyle=None`, `AllowsTransparency=True`, `ShowInTaskbar=False`
- Notifications displayed via `ItemsControl` with a `DataTemplate`
- Each card has a left accent border, title (semibold), and body
- Slide-in animation from left on arrival (`TranslateTransform` + `DoubleAnimation`)
- Fade-out animation on expiry (`Opacity` animation)
- Click-through mode via Win32 `WS_EX_TRANSPARENT` extended window style

## Settings Storage

Only file written: `%AppData%\NotificationsPro\settings.json`

Contains UI preferences only:
- Font family, size, weight, line spacing
- Colors (text, background, accent, title)
- Opacity, corner radius, padding, border
- Duration, always-on-top, click-through, animations
- Overlay position and size
- Monitor index, snap settings

**Never contains notification content.**

## Queue Logic

1. New notification arrives → deduplicate against visible items (2s window)
2. If `visible.Count < maxVisible` → add to list (newest first), start expiry timer
3. If at capacity → increment overflow count, discard content immediately
4. On expiry → set `IsExpiring=true` (triggers fade-out), then remove from list
5. On removal → decrement overflow count if > 0

## Privacy Model

- `NotificationItem` exists only in RAM (not serializable, no persistence attributes)
- `QueueManager` holds max 3 references; overflow stores count only
- Expiry timers remove references; GC reclaims memory
- No logging of notification content
- No database, registry, cache, or telemetry
