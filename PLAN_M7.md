# Milestone 7: Accessibility & Inclusivity — Implementation Plan

Status: Implemented. This file is retained as the original implementation plan for reference.
See `docs/PLAN.md` for the current milestone status and roadmap.

## Phase 1: Notification Timing (Persistent + Extended Duration + Auto-Duration)
- **AppSettings.cs**: Add `PersistentNotifications`, `AutoDurationEnabled`, `AutoDurationSecondsPerLine`, `AutoDurationBaseSeconds`
- **SettingsViewModel.cs**: Mirror properties with QueueSave()
- **QueueManager.cs**: Skip expiry timer when persistent; calculate auto-duration from body line count
- **SettingsWindow.xaml**: Extend duration slider max to 120s; add persistent toggle + auto-duration controls in Behavior tab

## Phase 2: System Integration (Reduce Motion, High Contrast, Text Scaling, Screen Reader)
- **AppSettings.cs**: Add `RespectReduceMotion`, `RespectHighContrast`, `RespectTextScaling`
- **OverlayViewModel.cs**: Override AnimationsEnabled when reduce motion active; scale font sizes when text scaling active
- **App.xaml.cs**: Detect High Contrast on startup + subscribe to runtime changes; auto-apply HC theme
- **OverlayWindow.xaml**: Add AutomationProperties to exclude from screen reader

## Phase 3: WCAG Contrast Ratio
- **NEW Helpers/ContrastHelper.cs**: Relative luminance + contrast ratio + WCAG level calculation
- **NEW Converters/WcagContrastConverter.cs**: MultiValueConverter for XAML labels (ratio + level + color)
- **SettingsWindow.xaml**: Add contrast ratio labels next to each text color picker
- **ThemePreset.cs**: Add "Color-Blind Safe" built-in theme (Wong palette)

## Phase 4: Global Hotkeys
- **AppSettings.cs**: Add `GlobalHotkeysEnabled`, `HotkeyToggleVisibility`, `HotkeyDismissAll`, `HotkeyToggleDnd`
- **NEW Services/HotkeyManager.cs**: RegisterHotKey/UnregisterHotKey Win32 P/Invoke, parse "Ctrl+Alt+N" combos
- **App.xaml.cs**: Create/dispose HotkeyManager, wire events to toggle/dismiss/DND actions

## Phase 5: Information Density Presets
- **SettingsViewModel.cs**: `ApplyDensityPresetCommand` — Compact/Comfortable/Spacious bundles (font, padding, gap, line limits)

## Phase 6: Focus Indicators & Click Target Sizes
- **Theme.xaml**: Add IsKeyboardFocused triggers to PrimaryButton, SecondaryButton, ToggleSwitch, Slider, ComboBox
- **Theme.xaml**: Ensure minimum 44dp hit areas on toggle switches and slider thumbs

## Phase 7: New Accessibility Tab in Settings
- Consolidate all M7 controls into a dedicated "Accessibility" tab

## Phase 8: Tests & Documentation
- ContrastHelper unit tests, QueueManager persistent/auto-duration tests, default value assertions
- Update PLAN.md, STATUS.md, CHANGELOG.md
