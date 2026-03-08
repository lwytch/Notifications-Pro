# Repository Map

This file acts as a structural guide to help AI coding agents immediately orient themselves.

## Core Application (C# .NET 8 WPF)

`src/NotificationsPro/`
  `App.xaml(.cs)`               — Entry point, tray icon, and window management.
  `Models/`                     — Data structures (AppSettings, NotificationItem).
  `Services/`                   — Core logic (SettingsManager, QueueManager, NotificationListener, ThemeManager).
  `ViewModels/`                 — UI binding logic (OverlayViewModel, SettingsViewModel).
  `Views/`                      — View templates (OverlayWindow, SettingsWindow).
  `Helpers/`                    — Utility classes (SnapHelper, IconHelper, etc).
  `Converters/`                 — XAML value converters.
  `Resources/Theme.xaml`        — Core visual dictionary and styling.

`tests/NotificationsPro.Tests/`
  Contains xUnit tests for core logic (QueueManager, SettingsManager, SnapHelper, ThemeTests).

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
