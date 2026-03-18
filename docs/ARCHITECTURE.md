# Architecture

## Overview

Notifications Pro is a C# .NET 8 WPF application that runs as a system tray app. It captures Windows toast notification text and displays it in a customizable always-on-top overlay window.

## Components

```
App.xaml.cs (Entry Point)
├── SettingsManager          Load/save %AppData%\NotificationsPro\settings.json
├── ThemeManager             Custom themes under %AppData%\NotificationsPro\themes\
├── QueueManager             In-memory queue + filtering + timers (overflow stores count only)
│   ├── SoundService          Optional sounds (system or custom WAV)
│   └── IconService           Optional per-app icons (built-in presets or user-provided image files)
├── NotificationListener     WinRT listener + polling + accessibility fallback (optional toast suppression)
├── HotkeyManager            Global hotkeys via Win32 RegisterHotKey
├── StartupHelper            Start-with-Windows packaged StartupTask helper
├── OverlayWindow            Transparent overlay window
│   └── OverlayViewModel     Binds queue state + settings to overlay rendering
└── SettingsWindow           Tabbed settings UI:
    - Appearance / Behavior / Filtering / Layout / Sounds / Streaming / Accessibility / UI Styling / System / Profiles / Help
    └── SettingsViewModel    Binds settings with debounced auto-save + import/export + theme apply
```

## Notification Capture

Primary path uses `Windows.UI.Notifications.Management.UserNotificationListener`:
- User-level permission (no admin required) and auto-prompt on first run
- Subscribes to `NotificationChanged` for real-time detection
- Polling fallback runs every 2 seconds with an overlap guard
- On each change/poll: call `GetNotificationsAsync()` and diff against seen IDs
- Tracks only notification IDs (`uint`) and minimal metadata for dedup (no content persistence)
- Seen ID set is trimmed (cap 5000) to prevent unbounded growth
- On startup, existing notifications are seeded as "seen" (not displayed)

Fallback path uses Windows accessibility events (best-effort):
- Used when WinRT delivery is unreliable on some unpackaged desktop scenarios
- Captures toast-like candidates using shell host heuristics and surfaces live diagnostics

Optional behavior:
- If "Suppress Toast Popups" is enabled, captured WinRT notifications are removed from Windows (best-effort) after capture.

## Overlay Rendering

- `WindowStyle=None`, `AllowsTransparency=True`, `ShowInTaskbar=False`
- Notifications displayed via `ItemsControl` with a `DataTemplate`
- Layout modes:
  - stacked cards (wrap + optional line-limiting/truncation, per-field typography, optional timestamps)
  - one-line banner mode (optional wrap + max lines + auto full-width)
- Slide-in direction is configurable; animations can be disabled or forced to fade-only
- If "Respect Reduce Motion" is enabled, system "Reduce Motion" disables motion and forces fade-only
- Long content uses internal scrolling and user-configurable scrollbar visibility/size/opacity
- Optional visuals: per-app tinting (deterministic hash) and per-app icons (user-configured)
- Click-through mode via Win32 `WS_EX_TRANSPARENT` extended window style
- Multi-monitor support: target monitor selection, monitor-aware snapping/presets, and optional fullscreen overlay backdrop mode

## Settings Storage

Settings and user-provided assets are stored under `%AppData%\NotificationsPro\`:
- `settings.json` (preferences, filters, per-app config; never notification content)
- `themes\*.json` (custom themes)
- `icons\` (optional custom icon files)
- `sounds\` (optional custom WAV files)

Import/export:
- Settings can be exported to and imported from a user-selected JSON file via file dialogs.

**Never contains notification content.**

## Queue Logic

- Configurable retained notifications (default 40, range 1–1000) with an overflow `+N not shown` summary
- Overflow never stores content (count only)
- Deduplication window is configurable; duplicate notifications within the window are suppressed
- Filtering occurs before enqueue:
  - per-app mute
  - keyword mute/highlight
  - quiet hours
  - burst limiting
- Expiry timing supports:
  - persistent notifications (no expiry)
  - auto-duration based on content length
  - manual duration setting

## Privacy Model

- `NotificationItem` exists only in RAM (not serializable, no persistence attributes)
- Queue stores only currently visible items; overflow stores count only
- Expiry/dismissal removes references; GC reclaims memory
- No logging of notification content
- No database, registry, cache, or telemetry
