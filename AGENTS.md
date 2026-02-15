# AGENTS.md

Tool-agnostic guidance for any coding agent working in this repository.

## What This Project Is

Notifications Pro: a Windows tray app (C# .NET 8 + WPF) that mirrors Windows toast notification text into a customizable always-on-top overlay. Text-first (no rich notification content), optional user-configured per-app icons/sounds, and no clickable links.

## Commands

```bash
dotnet restore                                          # restore dependencies
dotnet build                                            # build solution
dotnet run --project src/NotificationsPro               # run the app
dotnet test                                             # run all tests
dotnet test --filter "FullyQualifiedName~QueueManager"  # run specific test class
dotnet publish src/NotificationsPro -c Release -r win-x64 --self-contained  # publish
```

## Repo Map

```
src/NotificationsPro/
  App.xaml(.cs)              — Entry point, tray icon, window management
  Models/                    — AppSettings, NotificationItem (in-memory only)
  Services/                  — QueueManager, SettingsManager, NotificationListener, ThemeManager, SoundService, IconService, HotkeyManager, SettingsThemeService
  ViewModels/                — OverlayViewModel, SettingsViewModel, BaseViewModel, RelayCommand
  Views/                     — OverlayWindow, SettingsWindow (XAML + code-behind)
  Helpers/                   — SnapHelper, IconHelper, FullscreenHelper, StartupHelper, AppTintHelper, ContrastHelper
  Converters/                — ColorToBrushConverter, AppIconConverter, AppTintBrushConverter, WcagContrastConverter
  Resources/Theme.xaml       — Premium dark theme styles
tests/NotificationsPro.Tests/
  QueueManagerTests.cs       — Queue logic tests
  SettingsManagerTests.cs    — Settings serialization tests
  SnapHelperTests.cs         — Edge snap calculation tests
  ThemeTests.cs              — ThemePreset/ThemeManager tests
docs/
  PLAN.md                    — Living plan with milestones
  STATUS.md                  — Current state + manual test checklist
  ARCHITECTURE.md            — System design documentation
```

## Agent Task Workflow

1. Read `docs/PLAN.md` to find the current focus and pick an unchecked item
2. Implement the feature or fix
3. Run `dotnet build` and `dotnet test` — all must pass
4. Update `docs/PLAN.md` (check off completed items)
5. Update `docs/STATUS.md` (what works, what doesn't)
6. Update `CHANGELOG.md` if the change is user-visible
7. Run the "Before You Commit" checklist below

## Guardrails — Do NOT Violate

1. **No notification content persistence** — never write notification title/body to disk, database, registry, cache, or logs
2. **No CI/CD** — no `.github/workflows/`, no GitHub Actions
3. **No paid services** — no hosted APIs, telemetry, error reporting, cloud dependencies
4. **No clickable links** — URLs display as plain text only
5. **Text-first overlay** — no images or rich content from notification payloads; optional per-app icons are user-configured only (built-in presets or user-provided files)
6. **Settings + user assets only** — persistent data is limited to `%AppData%\NotificationsPro\settings.json`, `%AppData%\NotificationsPro\themes\*.json`, and optional user-provided assets under `%AppData%\NotificationsPro\icons\` and `%AppData%\NotificationsPro\sounds\` (plus user-chosen import/export JSON). Never persist notification content.
7. **No real settings committed** — only `settings.example.json` goes in the repo

## Before You Commit Checklist

- [ ] `dotnet build` succeeds with no errors
- [ ] `dotnet test` passes all tests
- [ ] Grep for notification content in any file I/O, logging, or serialization — must find none
- [ ] No `settings.json` staged — only `settings.example.json`
- [ ] `.gitignore` covers new artifacts
- [ ] `docs/PLAN.md` and `docs/STATUS.md` updated
- [ ] No `.github/workflows/` files
- [ ] No database, cache, or telemetry code

## Coding Conventions

- MVVM pattern: Views bind to ViewModels, minimal code-behind
- PascalCase for public members, `_camelCase` for private fields
- One class per file, file named after class
- Services should be injectable/mockable for testing
