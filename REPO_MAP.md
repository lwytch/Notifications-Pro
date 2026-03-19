# Repository Map

This file is the quick orientation guide for AI agents and maintainers working in this repository.

## Root Guides & Configuration

`AGENTS.md`
  Local AI-agent operating rules, repo workflow, privacy constraints, and available skills.

`REPO_MAP.md`
  Structural overview of the repository; update this when major folders, workflows, or responsibilities change.

`README.md`
  User-facing product overview, setup instructions, workflow guidance, troubleshooting, and release notes.

`CHANGELOG.md`
  Rolling engineering changelog for user-visible additions, fixes, and behavior changes.

`LICENSE`
  GPL v3 license for the project.

`SECURITY.md`
  Security and privacy posture for the public repository.

`CONTRIBUTING.md`
  Contributor workflow and repository expectations.

`settings.example.json`
  Example serialized settings payload; keep this aligned with `AppSettings`.

`docs/`
  Living project documentation.
  `PLAN.md`                     — Roadmap, active implementation backlog, and completed milestones.
  `STATUS.md`                   — Current shipped behavior, manual verification checklist, and known limitations.
  `ARCHITECTURE.md`             — Higher-level system design and component relationships.

## Core Application (`src/NotificationsPro/`)

`App.xaml(.cs)`
  Application entry point, tray menu wiring, profile/theme switching, and window lifecycle management.

`Models/`
  In-memory and persisted configuration/state models such as `AppSettings`, `NotificationItem`, and rule definitions.

`Services/`
  Operational services and persistence boundaries.
  `NotificationListener.cs`     — WinRT + accessibility notification capture pipeline.
  `QueueManager.cs`             — RAM-only visible queue, overflow counting, filtering, highlighting, and expiry behavior.
  `SettingsManager.cs`          — Load/save/apply settings, normalize persisted values, and reject oversized startup settings payloads.
  `ThemeManager.cs`             — Custom theme storage plus settings import/export, including oversized-theme guardrails.
  `ProfileManager.cs`           — Named full-settings profile save/load/delete with size-limited JSON loads.
  `SettingsThemeService.cs`     — Runtime theming for the settings window.
  `SpokenNotificationService.cs` — Built-in narration pipeline and voice management.
  `IconService.cs` / `SoundService.cs` — Local asset handling for per-app icons and sounds.
  `HotkeyManager.cs`            — Global hotkey registration and diagnostics.
  `BackgroundImageService.cs`   — Managed local background-image validation plus bounded transformed-image caching for card/fullscreen artwork.

`ViewModels/`
  MVVM state and command surfaces for the UI.
  `OverlayViewModel.cs`         — Overlay-facing computed settings, grouping, and rendering state.
  `SettingsViewModel.cs`        — Main settings, save pipeline, commands, import/export, and profile operations.
  `SettingsViewModel.SinglePanelEnhancements.cs`
                                — Filtering/rule-editor and related single-panel behavior helpers.
  `SettingsViewModel.SettingsAuditPolish.cs`
                                — Settings IA cleanup helpers such as app filtering/search views.

`Views/`
  WPF windows and templates.
  `OverlayWindow.xaml(.cs)`     — Notification rendering surface, card animations, hit-testing, and overlay interaction.
  `SettingsWindow.xaml(.cs)`    — Multi-tab settings UI, tooltips, and window/popup behavior.

`Helpers/`
  Focused normalization and formatting helpers used across settings, rendering, and tests.
  Includes animation helpers (`AnimationEasingHelper`, `NotificationAnimationStyleHelper`, `HighlightAnimationHelper`), rule helpers (`NotificationRuleMatcher`, `NotificationMatchScopeHelper`), and system helpers (`StartupHelper`, `SnapHelper`, `FullscreenHelper`).

`Converters/`
  WPF value converters for colors, icons, timestamps, one-line content, Voice Access labels, and card background image sources.

`Resources/Theme.xaml`
  Shared visual resource dictionary for the app and settings window.

`Fonts/`
  Bundled type assets such as `OpenDyslexic-Regular.otf`.

`Assets/`
  Application-owned visual assets used by the desktop app.

## Packaging & Distribution

`src/NotificationsPro.Package/`
  MSIX packaging project and package metadata.
  `NotificationsPro.Package.wapproj` — Windows application packaging project.
  `Package.appxmanifest`        — Package identity, startup task, capabilities, and visual metadata.
  `Images/`                     — MSIX tile, splash, and store imagery.

`scripts/app-packaging/`
  Maintainer-only local PowerShell packaging pipeline. This folder is gitignored and may be absent from the public checkout.
  `release.ps1`                 — Version bump + build + sign orchestration.
  `build_msix.ps1`              — Self-contained publish plus `MakeAppx` packaging step.
  `sign_msix.ps1`               — Certificate-store / PFX signing flow for the MSIX artifact.

`AppPackages/`
  Local generated MSIX output directory for release artifacts. Gitignored.

## Tests (`tests/NotificationsPro.Tests/`)

`NotificationsPro.Tests.csproj`
  xUnit test project for app logic.

`QueueManagerTests.cs`
  Queue, filtering, highlight, overflow, preview, and expiry coverage.

`SettingsManagerTests.cs`
  Settings defaults, migration, normalization, clone, and persistence coverage.

`BackgroundImageServiceTests.cs`
  Managed background-image path validation and transformed-image cache-boundary coverage.

`ThemeTests.cs`
  Theme preset behavior, JSON import/export coverage, and oversized custom-theme load guardrails.

`ProfileManagerTests.cs`
  Full-profile round-trip coverage.

`OverlayViewModelTests.cs`
  Overlay-facing state and computed-behavior checks.

`NotificationListenerTests.cs`
  Capture-path parsing and browser-toast split behavior coverage.

`SpokenNotification*.cs`, `VoiceAccessTextFormatterTests.cs`, `AccessibilityTests.cs`
  Narration, Voice Access, and accessibility behavior checks.

`StreamingPresentationTests.cs`, `SystemIntegrationTests.cs`, `StartupSettingsMigrationHelperTests.cs`, `UxPolishTests.cs`, `M95Tests.cs`
  Feature-area regression coverage for streaming, system integration, startup migration, and UX/persistence work.

## Local Ops & AI Tooling

`.agents/skills/`
  Local AI-agent skills used in this workspace. This directory is intentionally gitignored for the public repo.
  Notable local skills include `update-repo-map`, `maintain-plan`, `maintain-architecture-docs`, `maintain-status-docs`, `maintain-settings-sync`, `repack-msix`, `install-msix`, and `settings-regression-checklist`.

`runbooks/`
  Gitignored owner-facing operational notes and manual recovery procedures.

`analysis/`
  Gitignored local audit notes, implementation reports, and one-off investigations.
