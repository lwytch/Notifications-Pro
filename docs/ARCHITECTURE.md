# Architecture

## Overview

Notifications Pro is a .NET 8 WPF tray application that mirrors Windows toast notification text into a highly configurable always-on-top overlay. The design is text-first and privacy-first: notification title/body content stays RAM-only by default, while settings, themes, profiles, and user-provided local assets persist under `%AppData%\NotificationsPro\`.

The packaged MSIX build is the primary runtime because package identity improves notification access and startup integration. The source app project can still be built and run directly for development and test work.

## Runtime Topology

```text
App.xaml.cs
├── SettingsManager            Load/save settings.json, normalize values, reject oversized startup payloads
├── ThemeManager               Manage custom themes plus settings import/export helpers
├── ProfileManager             Save/load full AppSettings profiles
├── QueueManager               Visible notification queue, overflow count, filtering, dedup, timers, Session Archive
│   ├── SoundService           Play default/per-app sounds
│   ├── IconService            Resolve built-in or managed per-app icons
│   └── BackgroundImageService Resolve managed local background artwork with bounded render caching
├── NotificationListener       WinRT capture, accessibility capture, diagnostics, retry, suppression
├── SpokenNotificationService  Built-in narration pipeline and voice playback tracking
├── HotkeyManager              Register global shortcuts
├── SettingsThemeService       Apply runtime settings-window theme resources
├── OverlayWindow              Overlay shell, interaction, hit-testing, animation, scroll behavior
│   └── OverlayViewModel       Bind visible queue state plus overlay-facing settings
└── SettingsWindow             Tabbed settings surface
    └── SettingsViewModel      Save pipeline, commands, import/export, profile/theme application
```

`App.xaml.cs` also owns the tray icon, focus-mode timer, presentation-mode polling, scheduled theme switching, first-run flow, and the bridge wiring between services and windows.

## Settings Surface

The current settings UI is organized into twelve sections:

- Appearance
- Behavior
- Filtering
- Apps
- Layout
- Sounds
- Streaming
- Accessibility
- Settings Window
- System
- Profiles
- Help

This tab ownership matters architecturally because future doc updates need to follow the shipped UI, not historical names such as the old `UI Styling` section.

## Notification Capture Pipeline

`NotificationListener` is a configurable WinRT-first capture pipeline:

- Primary path: `Windows.UI.Notifications.Management.UserNotificationListener`
- Capture policy: `Auto`, `Prefer WinRT`, or `Force Accessibility`
- Reliability layer: guarded polling plus seen-ID seeding/trimming to avoid duplicate replay or unbounded growth
- Recovery tools: manual retry, access diagnostics, and current status surfaced in the System tab and tray UI
- Optional suppression: captured WinRT notifications can be removed from Windows after capture

Accessibility capture is no longer just a passive fallback for odd unpackaged runs. It is a supported operator-selected mode that can be used explicitly when WinRT delivery stalls or reports false-positive access states.

## Queue, Filtering, and Rendering

`QueueManager` is the boundary between capture and presentation:

- Retains only currently visible notifications in the main overlay queue
- Stores overflow as a count only (`+N not shown`)
- Applies pre-enqueue control flow for pause/focus state, per-app mute, rule-based mute/highlight/narration matching, deduplication, quiet hours, and burst limiting
- Supports persistent notifications, manual durations, and auto-duration based on content length

The main overlay is rendered by `OverlayWindow` and `OverlayViewModel`, which handle:

- Stacked cards and single-line banner mode
- Grouping, timestamps, typography, scrollbars, and click-through behavior
- Dragging, snapping, monitor placement, and fullscreen backdrop mode
- Per-app icons, sounds, tints, and card/background overrides

### Session Archive

The queue architecture has one explicit privacy exception:

- `Session Archive` is disabled by default
- When enabled, it keeps a bounded in-memory session list of captured notifications for the current app run
- It is cleared on app close
- It is not written to disk by Notifications Pro

That archive is separate from the visible queue and should always be described explicitly rather than hidden under the general RAM-only queue summary.

### In-Memory Asset Caches

Notifications Pro does use bounded in-memory caches for local rendering assets such as icons and transformed background images. These are render/performance caches only. They are not persistent storage and do not contain notification title/body history.

## Settings, Themes, Profiles, and Local Assets

Persistent data lives under `%AppData%\NotificationsPro\`:

- `settings.json` - app preferences, rule definitions, app-level overrides, and settings-window state
- `themes\*.json` - named custom overlay themes
- `profiles\*.json` - full `AppSettings` snapshots for profile save/load
- `icons\` - managed user-provided icon files
- `sounds\` - managed user-provided WAV files
- `backgrounds\` - managed user-provided card/fullscreen background images

Import/export/profile flows serialize `AppSettings` snapshots only. They do not persist `NotificationItem` content.

Managed asset references are normalized through the app's asset-path helpers so saved settings/profile/export JSON stores portable Notifications Pro-relative references instead of leaking machine-specific absolute paths.

Local JSON loads are guarded defensively:

- startup `settings.json`
- custom theme discovery
- settings import
- profile load

All of those paths apply a 1 MB file-size cap before loading.

## Packaging and Startup

Notifications Pro ships through:

- app project: `src/NotificationsPro/NotificationsPro.csproj`
- packaging project: `src/NotificationsPro.Package/NotificationsPro.Package.wapproj`

Runtime identity rules:

- Packaged runs keep their Windows package identity
- Unpackaged development runs set an explicit AppUserModelID only when package identity is absent
- Start-with-Windows is implemented through the packaged `StartupTask`, not a registry Run key

Release artifacts are built and signed locally, then distributed as:

- `NotificationsPro.msix`
- matching public `NotificationsPro.cer`

## Privacy Model

The privacy contract is narrower and more precise than "nothing is ever stored":

- Notification title/body content is never written to settings, themes, profiles, registry, logs, telemetry, or local caches
- Visible notification content exists in RAM while the card is on screen
- Overflow stores count only, not content
- Session Archive is the only built-in history-like feature, and it remains opt-in, RAM-only, bounded, and cleared on app close
- Clipboard copy actions and narration can hand notification text to Windows clipboard/audio surfaces, but Notifications Pro does not create its own disk-backed content history
- App-name-derived preferences can persist, including muted apps, spoken-app settings, and per-app icon/sound/background overrides
- The app makes no network calls and includes no telemetry
