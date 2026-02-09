# Notifications Pro

A lightweight Windows tray app that mirrors Windows toast notification text into a customizable always-on-top overlay window — so you can actually read your notifications in full, without truncation.

## Features

- **Full-text notifications**: Title and body displayed without truncation, with text wrapping and scrolling for long content
- **Customizable overlay**: Font, colors, opacity, corner radius, padding, accent color — make it yours
- **Slide-in animations**: Notifications animate in from the left; configurable speed or disable entirely
- **Queue management**: Max 3 visible at once with a "+N more" overflow indicator
- **System tray app**: Runs quietly in the tray with a context menu for quick access
- **Always-on-top** (configurable): Keep the overlay above all windows, or disable it
- **Click-through mode**: Let mouse clicks pass through the overlay
- **Edge snapping**: Overlay snaps to screen edges when dragged nearby
- **Premium dark theme**: Clean, modern UI throughout

## Privacy

Notifications Pro is designed with privacy as a hard constraint:

- **No notification content is ever saved to disk.** Content exists only in RAM for display and is discarded after expiry.
- **No database, no logs, no cache, no telemetry.** The app creates zero persistent records of your notifications.
- **The only file written** is `%AppData%\NotificationsPro\settings.json`, which stores UI preferences only (fonts, colors, position). It never contains notification content.
- **No network calls.** Everything runs locally.

Windows itself may keep notification history in the Action Center — Notifications Pro does not interfere with or modify that behavior.

## Setup

### Requirements
- Windows 10 (version 1709+) or Windows 11
- .NET 8 Runtime (or build from source with .NET 8 SDK)

### Build & Run
```bash
dotnet restore
dotnet build
dotnet run --project src/NotificationsPro
```

### Publish
```bash
dotnet publish src/NotificationsPro -c Release -r win-x64 --self-contained
```

## How to Use

### Tray Icon
Right-click the purple "N" icon in the system tray:

- **Show/Hide Overlay** — Toggle the overlay window visibility
- **Pause/Resume Notifications** — Temporarily stop showing new notifications
- **Settings...** — Open the settings window (also accessible by double-clicking the tray icon)
- **Quit** — Exit the application

### Settings Window
The settings window has three tabs:

- **Appearance** — Font family, size, weight, line spacing, colors (title, body, background, accent), opacity, corner radius, padding
- **Behavior** — Notification duration, always-on-top toggle, click-through mode, animation toggle and speed
- **Position** — Overlay width, max height, snap-to-edges toggle and distance

Use the **Send Test Notification** button to preview your settings with sample notification content.

Changes are saved automatically.

### Overlay
- **Drag** the overlay by clicking and dragging anywhere on it (when click-through is off)
- **Resize** using the grip in the bottom-right corner
- The overlay snaps to screen edges when dragged nearby (configurable)

## Where Settings Are Stored

`%AppData%\NotificationsPro\settings.json`

This file contains only UI preferences (fonts, colors, layout). See `settings.example.json` for the default values.

## Notification Capture Limitations

Currently using mock/preview notifications (Milestone 1). Real notification capture via `UserNotificationListener` API is planned for Milestone 2.

Known limitations of the `UserNotificationListener` API:
- Requires user permission grant on first run
- Some system-level notifications may not be accessible
- Apps that opt out of notification mirroring won't be captured

## Running Tests
```bash
dotnet test
```

## Install / Uninstall

### Install
Build from source or use the published executable. No installer required for development.

### Uninstall
Delete the application files. Optionally remove settings:
```
del "%AppData%\NotificationsPro\settings.json"
rmdir "%AppData%\NotificationsPro"
```
