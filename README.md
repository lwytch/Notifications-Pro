# Notifications Pro

Notifications Pro is a Windows tray app (C# .NET 8 + WPF) that mirrors Windows toast notification text into a customizable always-on-top overlay, so you can actually read notifications without truncation.

This project is optimized for:
- readability (wrapping/scrolling, banner mode, per-field typography),
- on-stream presentation (chroma key, fixed window mode),
- and control (filtering, quiet hours, hotkeys, themes).

The project is still ongoing and is by no means a finished article, but I use this everyday, if you struggle with Reading notifications - This is for you, customise the size and location of them to whatever you want. 

More updates to come in next few days / weeks. 

I will also upload some screenshots eventually when its polished.

## Highlights

- **Real notification capture** via `UserNotificationListener` (with permission prompt), plus a hardened accessibility fallback for cases where WinRT delivery is unreliable.
- **Overlay window** with always-on-top, click-through, snapping, multi-monitor support, manual edge resize, and optional fullscreen overlay backdrop.
- **Layout modes**: stacked cards or one-line banner mode (with optional wrap + max lines), plus scroll for long content.
- **Interaction**: click-to-dismiss, hover-to-pause timers, right-click card menu (dismiss/copy/clear), tray “Clear All”, optional timestamps.
- **Filtering & smart control**: per-app mute, keyword mute/highlight, quiet hours, burst limiting, focus mode, presentation mode (auto DND when configured apps go fullscreen).
- **Themes & sharing**: built-in presets, save/load/delete custom themes, import/export settings JSON, and a tray theme quick-switch menu.
- **Sounds & icons**: optional notification sounds (system sounds or custom WAV) and per-app icons (built-in vector presets or custom image files), with per-app overrides.
- **System integration**: Start with Windows and global hotkeys (toggle overlay, dismiss all, toggle DND).
- **Settings UX**: tabs for Themes, Appearance, Behavior, Filtering, Position, Streaming, Accessibility, and UI Styling (including dark/light/system and popup mode).

## Common Use Cases

- **Streaming / OBS**: keep notifications readable on-stream using Chroma Key mode, fixed window sizing, and optional per-app tint/icons for fast recognition.
- **Presenting / meetings**: enable Presentation Mode to auto-DND when your presentation app is fullscreen; use Focus Mode and quiet hours to control interruptions.
- **Gaming / fullscreen apps**: keep an always-on-top overlay with click-through so you can read messages without alt-tabbing.
- **Accessibility & readability**: persistent notifications, auto-duration for long messages, density presets, text scaling, high-contrast handling, and reduce-motion support.
- **Monitoring & alerts**: highlight keywords (e.g., “failed”, “down”, “urgent”), use optional sounds for critical apps, and burst limiting to avoid floods.
- **Daily comms**: long chat/email/calendar notifications that normally get truncated (Teams/Slack/Discord/Outlook, etc.).

## Why No Clickable Links or Notification Images?

This is a deliberate “text-first” design:
- **Safety**: notifications frequently contain URLs; an always-on-top overlay with clickable links increases the risk of accidental clicks and phishing. URLs are shown as plain text only.
- **Predictability**: Windows toasts can include buttons/actions and rich layouts. The overlay is intentionally passive (read/dismiss/copy) so behavior is consistent across capture paths.
- **Privacy & simplicity**: rendering rich media can imply fetching/decoding additional content and adds edge cases. The overlay does not display images from notification payloads.

The app can show **optional per-app icons**, but these are user-configured (built-in vector presets or user-provided files) and are not sourced from notification content.

## Privacy & Data Model

Notifications Pro is designed to avoid persisting notification content:
- **No notification title/body is written to disk** (no database, no cache, no logs of notification text).
- Notification content exists only in RAM while displayed and is released after dismissal/expiry.
- The app makes **no network calls** and includes **no telemetry**.

Windows itself may keep notification history in the Action Center. Notifications Pro can optionally suppress native toast popups after capture (WinRT path only); leave this off if you want Windows’ default behavior untouched.

### Files Written

Under `%AppData%\NotificationsPro\`:
- `settings.json` (UI + behavior preferences; may include your filter lists like muted apps/keywords)
- `themes\*.json` (custom themes)
- `icons\` (optional custom icon files)
- `sounds\` (optional custom WAV files)

See `settings.example.json` for example defaults.

## Requirements

- Windows 10 or Windows 11
- .NET 8 SDK (to build from source) or .NET 8 Runtime (to run a published build)

## Build / Run / Test

```bash
dotnet restore
dotnet build
dotnet run --project src/NotificationsPro
dotnet test
```

## How To Use

- **Tray menu** (right-click the tray icon): show/hide overlay, pause (DND), always-on-top, click-through, focus mode timer, quick mute, theme switch, clear all, settings, quit.
- **Overlay**: click a card to dismiss; hover pauses expiry timers; right-click a card for dismiss/copy/clear and mute actions.
- **Settings**: changes are auto-saved; use “Send Test Notification” to preview styling without waiting for real notifications.

### Streaming Setup (OBS)

1. In Settings > Streaming, enable **Chroma Key** and choose a key color (Green/Blue/Magenta or custom).
2. Optionally enable **OBS Fixed Window Mode** and set a fixed width/height for clean window capture.
3. In OBS, add a **Window Capture** source for the overlay window and apply a **Chroma Key** filter using the same color.

### Publish

```bash
dotnet publish src/NotificationsPro -c Release -r win-x64 --self-contained
```

## Notification Access

On first run, Windows will prompt for notification access. If you denied it:
1. Open Windows Settings > Notifications (location varies by Windows version)
2. Find notification access/listener settings
3. Enable access for Notifications Pro

The tray menu also includes “Grant Notification Access” and “Retry Access Check” for troubleshooting.

## Troubleshooting

- **No notifications captured**: verify permission, then use the tray “Retry Access Check”. Some unpackaged desktop apps can be inconsistent with `UserNotificationListener`; the app will fall back to accessibility capture when needed.
- **Can’t drag the overlay**: click-through intentionally blocks mouse interaction; disable click-through from the tray menu.
- **Unexpected missing Windows toasts**: ensure “Suppress Toast Popups” is disabled.

## Project Docs

- `docs/STATUS.md` for current capabilities and a manual test checklist
- `docs/PLAN.md` for milestones/roadmap
- `docs/ARCHITECTURE.md` for a high-level component overview
