# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Project Is

Notifications Pro is a Windows tray app (C# .NET + WPF) that mirrors Windows toast notification text into a customizable always-on-top overlay window. Optional per-app icons (user-configured, not from notification content). No clickable links.

## Build / Run / Test Commands

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run --project src/NotificationsPro

# Run unit tests
dotnet test

# Run a single test class
dotnet test --filter "FullyQualifiedName~QueueManagerTests"

# Run a single test method
dotnet test --filter "FullyQualifiedName~QueueManagerTests.TestOverflowCount"

# Publish self-contained
dotnet publish src/NotificationsPro -c Release -r win-x64 --self-contained
```

## Project Structure

```
src/NotificationsPro/
  App.xaml(.cs)                # Entry point, tray icon, window management
  Models/
    AppSettings.cs             # Settings POCO (serialized to JSON, no notification content)
    NotificationItem.cs        # In-memory-only notification data (title, body, timestamp)
    ThemePreset.cs             # Named visual theme (colors, opacity, shape, accent)
    IconPreset.cs              # Built-in icon presets (vector geometry paths)
  Services/
    QueueManager.cs            # Queue logic: max 3 visible, overflow count, dedup, expiry
    SettingsManager.cs         # Load/save settings from %AppData%\NotificationsPro\settings.json
    SettingsThemeService.cs    # Runtime settings window theming (Dark/Light/System)
    SoundService.cs            # Per-app notification sounds (system + custom WAV)
    IconService.cs             # Per-app icon resolution (built-in presets + custom images)
  ViewModels/
    BaseViewModel.cs           # INotifyPropertyChanged base class
    RelayCommand.cs            # ICommand implementation
    OverlayViewModel.cs        # Binds queue state + appearance settings to overlay
    SettingsViewModel.cs       # Binds settings to UI with debounced auto-save
  Views/
    OverlayWindow.xaml(.cs)    # Transparent always-on-top overlay with slide-in animations
    SettingsWindow.xaml(.cs)   # Tabbed settings UI (Appearance / Behavior / Position)
  Helpers/
    SnapHelper.cs              # Snap-to-edge calculations
    IconHelper.cs              # Programmatic tray icon generation
  Converters/
    ColorToBrushConverter.cs   # Hex string to WPF Brush converter
  Resources/Theme.xaml         # Premium dark theme styles
tests/NotificationsPro.Tests/
  QueueManagerTests.cs         # 12 tests: capacity, overflow, dedup, pause, clear
  SettingsManagerTests.cs      # 7 tests: defaults, clone, reset, apply, privacy
  SnapHelperTests.cs           # 7 tests: edge snapping, corners, default position
docs/
  PLAN.md                      # Living plan with milestones and checkboxes
  STATUS.md                    # Current state + manual test checklist
  ARCHITECTURE.md              # System design documentation
```

## Privacy Rules (HARD CONSTRAINTS — never violate)

1. **Never persist notification content** — no database, no flat files, no registry, no cache, no telemetry.
2. **No logs containing notification title/body/content.** If logging exists, it must be OFF by default and must never include notification content even when enabled.
3. **Notification content exists only in RAM** for rendering. Discard immediately after display expires. Do not discard windows notifications from notification centre, this is a seperate application.
4. **No history buffer.** Only hold what's currently on-screen or queued in memory (max 3 visible + overflow count only).
5. **Overflow notifications store only a count**, not content.
6. **The only file written** is `%AppData%\NotificationsPro\settings.json` (UI/style settings only, never notification data).

## Coding Conventions

- **MVVM pattern**: Views bind to ViewModels; no code-behind logic beyond window mechanics.
- **Naming**: PascalCase for public members, `_camelCase` for private fields, `I`-prefix for interfaces.
- **File layout**: One class per file. Name file after the class.
- **WPF bindings**: Use `INotifyPropertyChanged` or `ObservableCollection<T>`. No direct UI manipulation from services.
- **Dependency injection**: Services (QueueManager, SettingsManager, NotificationListener) should be injectable/mockable for testing.
- **No clickable links**: URLs in notification text render as plain text only.
- **Overlay icons**: Optional per-app icons (user-configured built-in presets or custom images). Icons are assigned per app name, not per notification. No notification content is used for icon selection.

## Queue Logic Rules

- Max 3 notifications visible simultaneously.
- Additional notifications increment an overflow counter ("+N more") — do **not** store their content.
- Each visible notification expires after configurable/system duration, then is discarded from memory.

## Maintaining Documentation

When making changes, update these files:

| File | When to update |
|---|---|
| `docs/PLAN.md` | Mark completed items, add new tasks, note blockers |
| `docs/STATUS.md` | After any feature/fix lands — update what works, what doesn't |
| `CHANGELOG.md` | After completing a milestone or significant feature |
| `CLAUDE.md` | When repo structure, commands, or conventions change |
| `AGENTS.md` | Keep aligned with CLAUDE.md changes |

## Adding Features Safely

1. Never introduce persistence of notification content (see Privacy Rules above).
2. No GitHub Actions, CI/CD workflows, or `.github/workflows/` directory.
3. No paid services, hosted APIs, telemetry, error reporting, or cloud dependencies.
4. Settings changes go in `AppSettings.cs` model + `settings.example.json` — never commit real `settings.json`.
5. New testable logic should have corresponding unit tests.

## Before You Commit Checklist

- [ ] `dotnet build` succeeds with no errors
- [ ] `dotnet test` passes
- [ ] Grep for notification content in any file I/O, logging, or serialization — must find none
- [ ] No `settings.json` (real user settings) staged — only `settings.example.json`
- [ ] `.gitignore` covers new build artifacts or local files
- [ ] `docs/PLAN.md` and `docs/STATUS.md` updated if scope changed
- [ ] No `.github/workflows/` files added
- [ ] No database, cache, or telemetry code introduced

## Milestones

1. Tray app + overlay window + settings UI skeleton + preview/mock notifications + queue logic
2. Real notification capture + timing alignment + privacy validation
3. Full customization + snapping + multi-monitor handling + click-through
4. Packaging + uninstall/reinstall docs + final polish + tests
