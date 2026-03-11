# Repository Map

This file acts as a structural guide to help AI coding agents immediately orient themselves.

## Core Application (C# .NET 8 WPF)

`src/NotificationsPro/`
  `App.xaml(.cs)`               — Entry point, tray icon, and window management.
  `Models/`                     — Settings, notification, rule, and per-app profile models (`AppSettings`, `NotificationItem`, `AppProfile`, rule definitions).
  `Services/`                   — Core runtime logic (SettingsManager, QueueManager, NotificationListener, SpokenNotificationService, BackgroundImageService, Theme/Profile managers).
  `ViewModels/`                 — UI binding logic for settings, narration rules, app profiles, and lane-aware overlays.
  `Views/`                      — Overlay and settings windows, including routed main/secondary overlay presentation.
  `Helpers/`                    — Utility classes for snapping, rule matching, overlay lanes, startup defaults, and secondary-overlay positioning.
  `Converters/`                 — XAML value converters for tinting, contrast, and background-image presentation.
  `Resources/Theme.xaml`        — Core visual dictionary and styling.

`tests/NotificationsPro.Tests/`
  Contains xUnit tests for queue/rule logic, settings round-tripping, startup defaults, theming, monitor/system integration, and streaming helpers.

## Packaging & Distribution (MSIX)

`src/NotificationsPro.Package/`
  `NotificationsPro.Package.wapproj` — Windows application packaging project.
  `Package.appxmanifest`        — The core MSIX config (Identity, Capabilities).
  `Images/`                     — Package icons and splash screens.

`scripts/app-packaging/`
  Contains the native PowerShell build/sign tools to avoid Visual Studio dependencies.
  `release.ps1`                 — Master orchestrator (bumps versions and runs build/sign).
  `build_msix.ps1`              — Compiles the raw payload and wraps it using MakeAppx.
  `sign_msix.ps1`               — Signs the generated MSIX using the local Code Signing Certificate.

## Operations & AI Tooling

`docs/`
  `PLAN.md`                     — Living roadmap and milestones.
  `STATUS.md`                   — Current capabilities and testing states.
  `ARCHITECTURE.md`             — System design documentation.

`runbooks/`
  (Gitignored) Documentation for the repository owner on how to manually perform operations if automation fails.

`.agents/skills/`
  AI Agent instructions for performing specialized repository actions (e.g., `repack-msix`, `update-repo-map`, `sanitise-for-publish`).
