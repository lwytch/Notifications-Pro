# AGENTS.md

Tool-agnostic guidance for any AI coding agent working in this repository.

## What This Project Is

Notifications Pro: a Windows tray app (C# .NET 8 + WPF) that mirrors Windows toast notification text into a customizable always-on-top overlay. Text-first (no rich notification content), optional user-configured per-app icons/sounds, and no clickable links.

## Commands

```bash
dotnet restore                                          # restore dependencies
dotnet build                                            # build solution
dotnet run --project src/NotificationsPro               # run the app
dotnet test                                             # run all tests
# Build MSIX release package (Requires Windows SDK):
powershell -ExecutionPolicy Bypass -File scripts\app-packaging\release.ps1 -Version "1.x.x"
```

## Repo Map

Please read `REPO_MAP.md` in the repository root for a breakdown of the architectural structure, testing locations, packaging scripts, and AI tools.

## Privacy Rules (HARD CONSTRAINTS — never violate)

1. **Never persist notification content** — no database, no flat files, no registry, no cache, no telemetry.
2. **No logs containing notification title/body/content.** If logging exists, it must be OFF by default and must never include notification content even when enabled.
3. **Notification content exists only in RAM** for rendering. Discard immediately after display expires/dismissal.
4. **No history buffer by default.** Only hold what's currently visible/on-screen; overflow stores a count only. Exception: the opt-in Session Archive (disabled by default) temporarily holds notification text in RAM for the current session. It is never written to disk and is cleared when the app closes.
5. **Overflow notifications store only a count**, not content.
6. **Persistent writes are settings/assets only** — `%AppData%\NotificationsPro\settings.json`, `%AppData%\NotificationsPro\themes\*.json`, and optional user-provided assets under `%AppData%\NotificationsPro\icons\` and `%AppData%\NotificationsPro\sounds\` (plus user-chosen import/export JSON). Never write notification title/body.

## Queue Logic Rules

- Max visible notifications is configurable (default 40, range 1–1000).
- Additional notifications increment an overflow counter ("+N more") — do **not** store their content.
- Each visible notification expires after configurable/system duration, then is discarded from memory.

## Coding Conventions

- **MVVM pattern**: Views bind to ViewModels; no code-behind logic beyond window mechanics.
- **Naming**: PascalCase for public members, `_camelCase` for private fields, `I`-prefix for interfaces.
- **File layout**: One class per file. Name file after the class.
- **WPF bindings**: Use `INotifyPropertyChanged` or `ObservableCollection<T>`. No direct UI manipulation from services.
- **Dependency injection**: Services (QueueManager, SettingsManager, NotificationListener) should be injectable/mockable for testing.
- **No clickable links**: URLs in notification text render as plain text only.
- **Overlay icons**: Optional per-app icons (user-configured built-in presets or custom images). Icons are assigned per app name, not per notification. No notification content is used for icon selection.

## Agent Task Workflow & Maintaining Documentation

1. Read `docs/PLAN.md` to find the current focus and pick an unchecked item
2. Implement the feature or fix
3. Run `dotnet build` and `dotnet test` — all must pass
4. Update `docs/PLAN.md` (check off completed items, add new tasks, note blockers)
5. Update `docs/STATUS.md` (what works, what doesn't)
6. Update `CHANGELOG.md` if the change is user-visible
7. Verify you haven't violated the Privacy Rules.
8. Run the "Before You Commit" checklist below

## Adding Features Safely

1. Never introduce persistence of notification content (see Privacy Rules above).
2. No GitHub Actions, CI/CD workflows, or `.github/workflows/` directory.
3. No paid services, hosted APIs, telemetry, error reporting, or cloud dependencies.
4. Settings changes go in `AppSettings.cs` model + `settings.example.json` — never commit real `settings.json`.
5. New testable logic should have corresponding unit tests.

## Before You Commit Checklist

- [ ] `dotnet build` succeeds with no errors
- [ ] `dotnet test` passes all tests
- [ ] Grep for notification content in any file I/O, logging, or serialization — must find none
- [ ] No `settings.json` staged — only `settings.example.json`
- [ ] `.gitignore` covers new build artifacts or local files
- [ ] `docs/PLAN.md` and `docs/STATUS.md` updated if scope changed
- [ ] No `.github/workflows/` files added
- [ ] No database, cache, or telemetry code introduced
