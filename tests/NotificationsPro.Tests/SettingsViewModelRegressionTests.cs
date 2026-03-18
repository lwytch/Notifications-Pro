using System.Reflection;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Threading;
using NotificationsPro.Models;
using NotificationsPro.Services;
using NotificationsPro.ViewModels;

namespace NotificationsPro.Tests;

public class SettingsViewModelRegressionTests : IDisposable
{
    private readonly string _tempDir;

    public SettingsViewModelRegressionTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "NotificationsProSettingsVmTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public void BuildCurrentSettings_AndLoadFromSettings_RoundTripMovedSettings()
    {
        StaThreadTestHelper.Run(() =>
        {
            var settingsManager = new SettingsManager(_tempDir);
            settingsManager.Load();
            var queueManager = new QueueManager(settingsManager);
            var viewModel = new SettingsViewModel(settingsManager, queueManager)
            {
                PersistentNotifications = true,
                AutoDurationEnabled = true,
                AutoDurationBaseSeconds = 9.5,
                AutoDurationSecondsPerLine = 3.5,
                AlwaysOnTop = false,
                ClickThrough = true,
                PresentationModeEnabled = true,
                PerAppTintEnabled = true,
                PerAppTintOpacity = 0.41,
                ShowQuickTips = false,
                CompactSettingsWindow = false
            };

            InvokeSaveSettings(viewModel);

            Assert.True(settingsManager.Settings.PersistentNotifications);
            Assert.True(settingsManager.Settings.AutoDurationEnabled);
            Assert.Equal(9.5, settingsManager.Settings.AutoDurationBaseSeconds);
            Assert.Equal(3.5, settingsManager.Settings.AutoDurationSecondsPerLine);
            Assert.False(settingsManager.Settings.AlwaysOnTop);
            Assert.True(settingsManager.Settings.ClickThrough);
            Assert.True(settingsManager.Settings.PresentationModeEnabled);
            Assert.True(settingsManager.Settings.PerAppTintEnabled);
            Assert.Equal(0.41, settingsManager.Settings.PerAppTintOpacity);
            Assert.False(settingsManager.Settings.ShowQuickTips);
            Assert.False(settingsManager.Settings.CompactSettingsWindow);

            var reloaded = new SettingsViewModel(settingsManager, new QueueManager(settingsManager));
            Assert.True(reloaded.PersistentNotifications);
            Assert.True(reloaded.AutoDurationEnabled);
            Assert.Equal(9.5, reloaded.AutoDurationBaseSeconds);
            Assert.Equal(3.5, reloaded.AutoDurationSecondsPerLine);
            Assert.False(reloaded.AlwaysOnTop);
            Assert.True(reloaded.ClickThrough);
            Assert.True(reloaded.PresentationModeEnabled);
            Assert.True(reloaded.PerAppTintEnabled);
            Assert.Equal(0.41, reloaded.PerAppTintOpacity);
            Assert.False(reloaded.ShowQuickTips);
            Assert.False(reloaded.CompactSettingsWindow);
        });
    }

    [Fact]
    public void SaveSettings_AppliesAndPersistsFullSettingsWindowThemePresetState()
    {
        StaThreadTestHelper.Run(() =>
        {
            var settingsManager = new SettingsManager(_tempDir);
            settingsManager.Load();
            var queueManager = new QueueManager(settingsManager);
            var viewModel = new SettingsViewModel(settingsManager, queueManager);
            var frostedGlass = viewModel.BuiltInThemes.First(t => t.Name == "Frosted Glass");

            viewModel.SettingsThemeMode = frostedGlass.Name;
            InvokeSaveSettings(viewModel);

            Assert.Equal(frostedGlass.Name, settingsManager.Settings.SettingsThemeMode);
            Assert.Equal(frostedGlass.SettingsWindowBg, settingsManager.Settings.SettingsWindowBg);
            Assert.Equal(frostedGlass.SettingsWindowSurface, settingsManager.Settings.SettingsWindowSurface);
            Assert.Equal(frostedGlass.SettingsWindowAccent, settingsManager.Settings.SettingsWindowAccent);
            Assert.Equal(frostedGlass.SettingsWindowOpacity, settingsManager.Settings.SettingsWindowOpacity);
            Assert.Equal(frostedGlass.SettingsSurfaceOpacity, settingsManager.Settings.SettingsSurfaceOpacity);
            Assert.Equal(frostedGlass.SettingsElementOpacity, settingsManager.Settings.SettingsElementOpacity);
            Assert.Equal(frostedGlass.SettingsWindowCornerRadius, settingsManager.Settings.SettingsWindowCornerRadius);

            var reloaded = new SettingsViewModel(settingsManager, new QueueManager(settingsManager));
            Assert.Equal(frostedGlass.Name, reloaded.SettingsThemeMode);
            Assert.Equal(frostedGlass.SettingsWindowBg, reloaded.SettingsWindowBg);
            Assert.Equal(frostedGlass.SettingsWindowOpacity, reloaded.SettingsWindowOpacity);
            Assert.Equal(frostedGlass.SettingsSurfaceOpacity, reloaded.SettingsSurfaceOpacity);
            Assert.Equal(frostedGlass.SettingsElementOpacity, reloaded.SettingsElementOpacity);
            Assert.Equal(frostedGlass.SettingsWindowCornerRadius, reloaded.SettingsWindowCornerRadius);
        });
    }

    [Fact]
    public void SettingsWindowThemeTweaks_SwitchModeToCustom_AndPersist()
    {
        StaThreadTestHelper.Run(() =>
        {
            var settingsManager = new SettingsManager(_tempDir);
            settingsManager.Load();
            var queueManager = new QueueManager(settingsManager);
            var viewModel = new SettingsViewModel(settingsManager, queueManager)
            {
                SettingsThemeMode = "Windows Dark"
            };

            viewModel.SettingsWindowOpacity = 0.73;
            viewModel.SettingsSurfaceOpacity = 0.44;
            viewModel.SettingsElementOpacity = 0.61;
            viewModel.SettingsWindowCornerRadius = 28;

            Assert.Equal("Custom", viewModel.SettingsThemeMode);

            InvokeSaveSettings(viewModel);

            Assert.Equal("Custom", settingsManager.Settings.SettingsThemeMode);
            Assert.Equal(0.73, settingsManager.Settings.SettingsWindowOpacity);
            Assert.Equal(0.44, settingsManager.Settings.SettingsSurfaceOpacity);
            Assert.Equal(0.61, settingsManager.Settings.SettingsElementOpacity);
            Assert.Equal(28, settingsManager.Settings.SettingsWindowCornerRadius);

            var reloaded = new SettingsViewModel(settingsManager, new QueueManager(settingsManager));
            Assert.Equal("Custom", reloaded.SettingsThemeMode);
            Assert.Equal(0.73, reloaded.SettingsWindowOpacity);
            Assert.Equal(0.44, reloaded.SettingsSurfaceOpacity);
            Assert.Equal(0.61, reloaded.SettingsElementOpacity);
            Assert.Equal(28, reloaded.SettingsWindowCornerRadius);
        });
    }

    [Fact]
    public void ApplyTheme_WithUnlinkedUiTheme_PreservesSettingsWindowThemeState()
    {
        StaThreadTestHelper.Run(() =>
        {
            var settingsManager = new SettingsManager(_tempDir);
            settingsManager.Load();
            var queueManager = new QueueManager(settingsManager);
            var viewModel = new SettingsViewModel(settingsManager, queueManager)
            {
                LinkOverlayThemeAndUiTheme = false,
                SettingsThemeMode = "Custom",
                SettingsWindowBg = "#121212",
                SettingsWindowAccent = "#33AAFF",
                SettingsWindowOpacity = 0.74,
                SettingsSurfaceOpacity = 0.49,
                SettingsElementOpacity = 0.63,
                SettingsWindowCornerRadius = 31
            };

            InvokeSaveSettings(viewModel);
            var theme = viewModel.BuiltInThemes.First(t => t.Name == "Light");

            InvokeApplyTheme(viewModel, theme);

            Assert.Equal("Custom", settingsManager.Settings.SettingsThemeMode);
            Assert.Equal("#121212", settingsManager.Settings.SettingsWindowBg);
            Assert.Equal("#33AAFF", settingsManager.Settings.SettingsWindowAccent);
            Assert.Equal(0.74, settingsManager.Settings.SettingsWindowOpacity);
            Assert.Equal(0.49, settingsManager.Settings.SettingsSurfaceOpacity);
            Assert.Equal(0.63, settingsManager.Settings.SettingsElementOpacity);
            Assert.Equal(31, settingsManager.Settings.SettingsWindowCornerRadius);
        });
    }

    [Fact]
    public void BuildSettingsThemePreviewSnapshot_IncludesSettingsWindowAccent()
    {
        StaThreadTestHelper.Run(() =>
        {
            var settingsManager = new SettingsManager(_tempDir);
            settingsManager.Load();
            var queueManager = new QueueManager(settingsManager);
            var viewModel = new SettingsViewModel(settingsManager, queueManager)
            {
                SettingsThemeMode = "Custom",
                SettingsWindowAccent = "#33AAFF"
            };

            var snapshot = InvokeBuildSettingsThemePreviewSnapshot(viewModel);

            Assert.Equal("Custom", snapshot.SettingsThemeMode);
            Assert.Equal("#33AAFF", snapshot.SettingsWindowAccent);
        });
    }

    [Fact]
    public void BuildSettingsThemePreviewSnapshot_UsesResolvedNamedThemeDefinition()
    {
        StaThreadTestHelper.Run(() =>
        {
            var settingsManager = new SettingsManager(_tempDir);
            settingsManager.Load();
            settingsManager.Apply(new AppSettings
            {
                SettingsThemeMode = "Light",
                SettingsWindowBg = "#111111",
                SettingsWindowSurface = "#161616",
                SettingsWindowSurfaceLight = "#202020",
                SettingsWindowSurfaceHover = "#2A2A2A",
                SettingsWindowText = "#F3F3F3",
                SettingsWindowTextSecondary = "#D0D0D0",
                SettingsWindowTextMuted = "#A0A0A0",
                SettingsWindowAccent = "#0078D4",
                SettingsWindowBorder = "#353535",
                SettingsWindowOpacity = 0.1,
                SettingsSurfaceOpacity = 0.2,
                SettingsElementOpacity = 0.3,
                SettingsWindowCornerRadius = 4
            });

            var queueManager = new QueueManager(settingsManager);
            var viewModel = new SettingsViewModel(settingsManager, queueManager);

            var snapshot = InvokeBuildSettingsThemePreviewSnapshot(viewModel);
            var lightTheme = viewModel.BuiltInThemes.First(t => t.Name == "Light");

            Assert.Equal("Light", snapshot.SettingsThemeMode);
            Assert.Equal(lightTheme.SettingsWindowBg, snapshot.SettingsWindowBg);
            Assert.Equal(lightTheme.SettingsWindowSurface, snapshot.SettingsWindowSurface);
            Assert.Equal(lightTheme.SettingsWindowAccent, snapshot.SettingsWindowAccent);
            Assert.Equal(lightTheme.SettingsWindowOpacity, snapshot.SettingsWindowOpacity);
            Assert.Equal(lightTheme.SettingsSurfaceOpacity, snapshot.SettingsSurfaceOpacity);
            Assert.Equal(lightTheme.SettingsElementOpacity, snapshot.SettingsElementOpacity);
            Assert.Equal(lightTheme.SettingsWindowCornerRadius, snapshot.SettingsWindowCornerRadius);
        });
    }

    [Fact]
    public void LoadProfile_AppliesSettingsWindowThemeAndDisplayModeThroughViewModel()
    {
        StaThreadTestHelper.Run(() =>
        {
            var settingsDir = Path.Combine(_tempDir, "settings");
            var profilesDir = Path.Combine(_tempDir, "profiles");
            var settingsManager = new SettingsManager(settingsDir);
            settingsManager.Load();
            var profileManager = new ProfileManager(profilesDir);

            profileManager.SaveProfile("Studio", new AppSettings
            {
                SettingsDisplayMode = "Popup",
                PopupAutoClose = true,
                SettingsThemeMode = "Custom",
                SettingsWindowBg = "#0F1014",
                SettingsWindowSurface = "#181B21",
                SettingsWindowSurfaceLight = "#232834",
                SettingsWindowSurfaceHover = "#2C3140",
                SettingsWindowText = "#F2F4F8",
                SettingsWindowTextSecondary = "#C7CDDA",
                SettingsWindowTextMuted = "#8B93A7",
                SettingsWindowAccent = "#33AAFF",
                SettingsWindowBorder = "#394257",
                SettingsWindowOpacity = 0.79,
                SettingsSurfaceOpacity = 0.36,
                SettingsElementOpacity = 0.62,
                SettingsWindowCornerRadius = 29,
                CompactSettingsWindow = false,
                LinkOverlayThemeAndUiTheme = false
            });

            settingsManager.Apply(new AppSettings
            {
                SettingsDisplayMode = "Window",
                PopupAutoClose = false,
                SettingsThemeMode = "Custom",
                SettingsWindowBg = "#111111",
                SettingsWindowAccent = "#0078D4",
                CompactSettingsWindow = true,
                LinkOverlayThemeAndUiTheme = true
            });

            var queueManager = new QueueManager(settingsManager);
            var viewModel = new SettingsViewModel(settingsManager, queueManager, profileManager);
            var refreshRequested = false;
            var bulkApplyStates = new List<bool>();
            viewModel.ConfigureRefreshSettingsWindow(() => refreshRequested = true);
            viewModel.ConfigureSettingsWindowBulkApplyState(isBulkApplying => bulkApplyStates.Add(isBulkApplying));

            InvokeLoadProfile(viewModel, "Studio");

            Assert.Equal("Popup", viewModel.SettingsDisplayMode);
            Assert.True(viewModel.PopupAutoClose);
            Assert.Equal("Custom", viewModel.SettingsThemeMode);
            Assert.Equal("#0F1014", viewModel.SettingsWindowBg);
            Assert.Equal("#33AAFF", viewModel.SettingsWindowAccent);
            Assert.Equal(0.79, viewModel.SettingsWindowOpacity);
            Assert.Equal(0.36, viewModel.SettingsSurfaceOpacity);
            Assert.Equal(0.62, viewModel.SettingsElementOpacity);
            Assert.Equal(29, viewModel.SettingsWindowCornerRadius);
            Assert.False(viewModel.CompactSettingsWindow);
            Assert.False(viewModel.LinkOverlayThemeAndUiTheme);

            Assert.Equal("Popup", settingsManager.Settings.SettingsDisplayMode);
            Assert.True(settingsManager.Settings.PopupAutoClose);
            Assert.Equal("#0F1014", settingsManager.Settings.SettingsWindowBg);
            Assert.Equal("#33AAFF", settingsManager.Settings.SettingsWindowAccent);
            Assert.False(settingsManager.Settings.CompactSettingsWindow);
            Assert.True(refreshRequested);
            Assert.Equal(new[] { true, false }, bulkApplyStates);
        });
    }

    [Fact]
    public void LoadProfile_WithBoundThemePresetCombo_RestoresEachSavedNamedUiThemePreset()
    {
        StaThreadTestHelper.Run(() =>
        {
            var settingsDir = Path.Combine(_tempDir, "bound-settings");
            var profilesDir = Path.Combine(_tempDir, "bound-profiles");
            var settingsManager = new SettingsManager(settingsDir);
            settingsManager.Load();
            var profileManager = new ProfileManager(profilesDir);
            var queueManager = new QueueManager(settingsManager);
            var viewModel = new SettingsViewModel(settingsManager, queueManager, profileManager);
            var combo = CreateBoundSettingsThemePresetCombo(viewModel);

            viewModel.SettingsThemeMode = "High Contrast";
            viewModel.NewProfileName = "HighContrast";
            InvokeSaveProfile(viewModel);
            PumpDispatcher();

            viewModel.SettingsThemeMode = "Frosted Glass";
            viewModel.NewProfileName = "Frosted";
            InvokeSaveProfile(viewModel);
            PumpDispatcher();

            InvokeLoadProfile(viewModel, "HighContrast");
            PumpDispatcher();
            var highContrast = viewModel.BuiltInThemes.First(t => t.Name == "High Contrast");

            Assert.Equal("High Contrast", viewModel.SettingsThemeMode);
            Assert.Equal("High Contrast", settingsManager.Settings.SettingsThemeMode);
            Assert.Equal("High Contrast", combo.SelectedItem);
            Assert.Equal(highContrast.SettingsWindowBg, settingsManager.Settings.SettingsWindowBg);
            Assert.Equal(highContrast.SettingsWindowAccent, settingsManager.Settings.SettingsWindowAccent);

            InvokeLoadProfile(viewModel, "Frosted");
            PumpDispatcher();
            var frosted = viewModel.BuiltInThemes.First(t => t.Name == "Frosted Glass");

            Assert.Equal("Frosted Glass", viewModel.SettingsThemeMode);
            Assert.Equal("Frosted Glass", settingsManager.Settings.SettingsThemeMode);
            Assert.Equal("Frosted Glass", combo.SelectedItem);
            Assert.Equal(frosted.SettingsWindowBg, settingsManager.Settings.SettingsWindowBg);
            Assert.Equal(frosted.SettingsWindowAccent, settingsManager.Settings.SettingsWindowAccent);
        });
    }

    [Fact]
    public void ApplyImportedSettings_WithBoundThemePresetCombo_RestoresNamedUiThemePreset()
    {
        StaThreadTestHelper.Run(() =>
        {
            var settingsManager = new SettingsManager(_tempDir);
            settingsManager.Load();
            var queueManager = new QueueManager(settingsManager);
            var viewModel = new SettingsViewModel(settingsManager, queueManager);
            var combo = CreateBoundSettingsThemePresetCombo(viewModel);
            var importedTheme = viewModel.BuiltInThemes.First(t => t.Name == "High Contrast");
            var imported = new AppSettings
            {
                SettingsDisplayMode = "Popup",
                PopupAutoClose = false,
                SettingsThemeMode = importedTheme.Name,
                SettingsWindowBg = importedTheme.SettingsWindowBg,
                SettingsWindowSurface = importedTheme.SettingsWindowSurface,
                SettingsWindowSurfaceLight = importedTheme.SettingsWindowSurfaceLight,
                SettingsWindowSurfaceHover = importedTheme.SettingsWindowSurfaceHover,
                SettingsWindowText = importedTheme.SettingsWindowText,
                SettingsWindowTextSecondary = importedTheme.SettingsWindowTextSecondary,
                SettingsWindowTextMuted = importedTheme.SettingsWindowTextMuted,
                SettingsWindowAccent = importedTheme.SettingsWindowAccent,
                SettingsWindowBorder = importedTheme.SettingsWindowBorder,
                SettingsWindowOpacity = importedTheme.SettingsWindowOpacity,
                SettingsSurfaceOpacity = importedTheme.SettingsSurfaceOpacity,
                SettingsElementOpacity = importedTheme.SettingsElementOpacity,
                SettingsWindowCornerRadius = importedTheme.SettingsWindowCornerRadius
            };

            InvokeApplyImportedSettings(viewModel, imported);
            PumpDispatcher();

            Assert.Equal(importedTheme.Name, viewModel.SettingsThemeMode);
            Assert.Equal(importedTheme.Name, settingsManager.Settings.SettingsThemeMode);
            Assert.Equal(importedTheme.Name, combo.SelectedItem);
            Assert.Equal(importedTheme.SettingsWindowBg, settingsManager.Settings.SettingsWindowBg);
            Assert.Equal(importedTheme.SettingsWindowAccent, settingsManager.Settings.SettingsWindowAccent);
        });
    }

    [Fact]
    public void LoadProfile_WithNamedSettingsTheme_RehydratesResolvedPopupThemeState()
    {
        StaThreadTestHelper.Run(() =>
        {
            var settingsDir = Path.Combine(_tempDir, "named-theme-settings");
            var profilesDir = Path.Combine(_tempDir, "named-theme-profiles");
            var settingsManager = new SettingsManager(settingsDir);
            settingsManager.Load();
            var profileManager = new ProfileManager(profilesDir);

            profileManager.SaveProfile("LightPopup", new AppSettings
            {
                SettingsDisplayMode = "Popup",
                PopupAutoClose = false,
                SettingsThemeMode = "Light",
                SettingsWindowBg = "#111111",
                SettingsWindowSurface = "#181818",
                SettingsWindowSurfaceLight = "#232323",
                SettingsWindowSurfaceHover = "#2C2C2C",
                SettingsWindowText = "#F2F4F8",
                SettingsWindowTextSecondary = "#C7CDDA",
                SettingsWindowTextMuted = "#8B93A7",
                SettingsWindowAccent = "#33AAFF",
                SettingsWindowBorder = "#394257",
                SettingsWindowOpacity = 0.31,
                SettingsSurfaceOpacity = 0.32,
                SettingsElementOpacity = 0.33,
                SettingsWindowCornerRadius = 7,
                CompactSettingsWindow = true
            });

            settingsManager.Apply(new AppSettings
            {
                SettingsDisplayMode = "Popup",
                SettingsThemeMode = "Windows Dark",
                SettingsWindowBg = "#111111",
                SettingsWindowAccent = "#0078D4"
            });

            var queueManager = new QueueManager(settingsManager);
            var viewModel = new SettingsViewModel(settingsManager, queueManager, profileManager);
            var refreshRequested = false;
            var bulkApplyStates = new List<bool>();
            viewModel.ConfigureRefreshSettingsWindow(() => refreshRequested = true);
            viewModel.ConfigureSettingsWindowBulkApplyState(isBulkApplying => bulkApplyStates.Add(isBulkApplying));

            InvokeLoadProfile(viewModel, "LightPopup");

            var lightTheme = viewModel.BuiltInThemes.First(t => t.Name == "Light");

            Assert.Equal("Light", viewModel.SettingsThemeMode);
            Assert.Equal(lightTheme.SettingsWindowBg, viewModel.SettingsWindowBg);
            Assert.Equal(lightTheme.SettingsWindowSurface, viewModel.SettingsWindowSurface);
            Assert.Equal(lightTheme.SettingsWindowAccent, viewModel.SettingsWindowAccent);
            Assert.Equal(lightTheme.SettingsWindowOpacity, viewModel.SettingsWindowOpacity);
            Assert.Equal(lightTheme.SettingsSurfaceOpacity, viewModel.SettingsSurfaceOpacity);
            Assert.Equal(lightTheme.SettingsElementOpacity, viewModel.SettingsElementOpacity);
            Assert.Equal(lightTheme.SettingsWindowCornerRadius, viewModel.SettingsWindowCornerRadius);

            Assert.Equal("Light", settingsManager.Settings.SettingsThemeMode);
            Assert.Equal(lightTheme.SettingsWindowBg, settingsManager.Settings.SettingsWindowBg);
            Assert.Equal(lightTheme.SettingsWindowSurface, settingsManager.Settings.SettingsWindowSurface);
            Assert.Equal(lightTheme.SettingsWindowAccent, settingsManager.Settings.SettingsWindowAccent);
            Assert.Equal(lightTheme.SettingsWindowOpacity, settingsManager.Settings.SettingsWindowOpacity);
            Assert.Equal(lightTheme.SettingsSurfaceOpacity, settingsManager.Settings.SettingsSurfaceOpacity);
            Assert.Equal(lightTheme.SettingsElementOpacity, settingsManager.Settings.SettingsElementOpacity);
            Assert.Equal(lightTheme.SettingsWindowCornerRadius, settingsManager.Settings.SettingsWindowCornerRadius);
            Assert.True(refreshRequested);
            Assert.Equal(new[] { true, false }, bulkApplyStates);
        });
    }

    [Fact]
    public void ApplyImportedSettings_PreservesDeviceState_AndLoadsSettingsWindowTheme()
    {
        StaThreadTestHelper.Run(() =>
        {
            var settingsManager = new SettingsManager(_tempDir);
            settingsManager.Load();
            settingsManager.Apply(new AppSettings
            {
                OverlayLeft = 120,
                OverlayTop = 240,
                MonitorIndex = 2,
                SelectedMonitorIndex = 3,
                SettingsWindowLeft = 360,
                SettingsWindowTop = 420,
                HasShownWelcome = true,
                OverlayVisible = false,
                NotificationsPaused = true,
                SettingsDisplayMode = "Window",
                PopupAutoClose = false,
                SettingsThemeMode = "Custom",
                SettingsWindowBg = "#111111",
                SettingsWindowAccent = "#0078D4"
            });

            var queueManager = new QueueManager(settingsManager);
            var viewModel = new SettingsViewModel(settingsManager, queueManager);
            var refreshRequested = false;
            var bulkApplyStates = new List<bool>();
            viewModel.ConfigureRefreshSettingsWindow(() => refreshRequested = true);
            viewModel.ConfigureSettingsWindowBulkApplyState(isBulkApplying => bulkApplyStates.Add(isBulkApplying));
            var imported = new AppSettings
            {
                OverlayLeft = 999,
                OverlayTop = 999,
                MonitorIndex = 9,
                SelectedMonitorIndex = 9,
                SettingsWindowLeft = 999,
                SettingsWindowTop = 999,
                HasShownWelcome = false,
                OverlayVisible = true,
                NotificationsPaused = false,
                SettingsDisplayMode = "Popup",
                PopupAutoClose = true,
                SettingsThemeMode = "Custom",
                SettingsWindowBg = "#10141B",
                SettingsWindowSurface = "#1A202A",
                SettingsWindowSurfaceLight = "#252D3A",
                SettingsWindowSurfaceHover = "#2E3748",
                SettingsWindowText = "#F4F6FA",
                SettingsWindowTextSecondary = "#C8CFDC",
                SettingsWindowTextMuted = "#8590A5",
                SettingsWindowAccent = "#3DAEFF",
                SettingsWindowBorder = "#3A445A",
                SettingsWindowOpacity = 0.78,
                SettingsSurfaceOpacity = 0.38,
                SettingsElementOpacity = 0.64,
                SettingsWindowCornerRadius = 26,
                CompactSettingsWindow = false
            };

            InvokeApplyImportedSettings(viewModel, imported);

            Assert.Equal(120, settingsManager.Settings.OverlayLeft);
            Assert.Equal(240, settingsManager.Settings.OverlayTop);
            Assert.Equal(2, settingsManager.Settings.MonitorIndex);
            Assert.Equal(3, settingsManager.Settings.SelectedMonitorIndex);
            Assert.Equal(360, settingsManager.Settings.SettingsWindowLeft);
            Assert.Equal(420, settingsManager.Settings.SettingsWindowTop);
            Assert.True(settingsManager.Settings.HasShownWelcome);
            Assert.False(settingsManager.Settings.OverlayVisible);
            Assert.True(settingsManager.Settings.NotificationsPaused);

            Assert.Equal("Popup", settingsManager.Settings.SettingsDisplayMode);
            Assert.True(settingsManager.Settings.PopupAutoClose);
            Assert.Equal("#10141B", settingsManager.Settings.SettingsWindowBg);
            Assert.Equal("#3DAEFF", settingsManager.Settings.SettingsWindowAccent);
            Assert.False(settingsManager.Settings.CompactSettingsWindow);

            Assert.Equal("Popup", viewModel.SettingsDisplayMode);
            Assert.True(viewModel.PopupAutoClose);
            Assert.Equal("#10141B", viewModel.SettingsWindowBg);
            Assert.Equal("#3DAEFF", viewModel.SettingsWindowAccent);
            Assert.Equal(0.78, viewModel.SettingsWindowOpacity);
            Assert.Equal(26, viewModel.SettingsWindowCornerRadius);
            Assert.False(viewModel.CompactSettingsWindow);
            Assert.True(refreshRequested);
            Assert.Equal(new[] { true, false }, bulkApplyStates);
        });
    }

    [Fact]
    public void ApplyImportedSettings_WithNamedSettingsTheme_RehydratesResolvedThemeState()
    {
        StaThreadTestHelper.Run(() =>
        {
            var settingsManager = new SettingsManager(_tempDir);
            settingsManager.Load();
            settingsManager.Apply(new AppSettings
            {
                SettingsDisplayMode = "Popup",
                SettingsThemeMode = "Windows Dark",
                SettingsWindowBg = "#111111",
                SettingsWindowAccent = "#0078D4"
            });

            var queueManager = new QueueManager(settingsManager);
            var viewModel = new SettingsViewModel(settingsManager, queueManager);
            var refreshRequested = false;
            var bulkApplyStates = new List<bool>();
            viewModel.ConfigureRefreshSettingsWindow(() => refreshRequested = true);
            viewModel.ConfigureSettingsWindowBulkApplyState(isBulkApplying => bulkApplyStates.Add(isBulkApplying));
            var imported = new AppSettings
            {
                SettingsDisplayMode = "Popup",
                PopupAutoClose = false,
                SettingsThemeMode = "Frosted Glass",
                SettingsWindowBg = "#111111",
                SettingsWindowSurface = "#181818",
                SettingsWindowSurfaceLight = "#232323",
                SettingsWindowSurfaceHover = "#2C2C2C",
                SettingsWindowText = "#F2F4F8",
                SettingsWindowTextSecondary = "#C7CDDA",
                SettingsWindowTextMuted = "#8B93A7",
                SettingsWindowAccent = "#33AAFF",
                SettingsWindowBorder = "#394257",
                SettingsWindowOpacity = 0.31,
                SettingsSurfaceOpacity = 0.32,
                SettingsElementOpacity = 0.33,
                SettingsWindowCornerRadius = 7,
                CompactSettingsWindow = true
            };

            InvokeApplyImportedSettings(viewModel, imported);

            var frostedTheme = viewModel.BuiltInThemes.First(t => t.Name == "Frosted Glass");

            Assert.Equal("Frosted Glass", viewModel.SettingsThemeMode);
            Assert.Equal(frostedTheme.SettingsWindowBg, viewModel.SettingsWindowBg);
            Assert.Equal(frostedTheme.SettingsWindowSurface, viewModel.SettingsWindowSurface);
            Assert.Equal(frostedTheme.SettingsWindowAccent, viewModel.SettingsWindowAccent);
            Assert.Equal(frostedTheme.SettingsWindowOpacity, viewModel.SettingsWindowOpacity);
            Assert.Equal(frostedTheme.SettingsSurfaceOpacity, viewModel.SettingsSurfaceOpacity);
            Assert.Equal(frostedTheme.SettingsElementOpacity, viewModel.SettingsElementOpacity);
            Assert.Equal(frostedTheme.SettingsWindowCornerRadius, viewModel.SettingsWindowCornerRadius);

            Assert.Equal("Frosted Glass", settingsManager.Settings.SettingsThemeMode);
            Assert.Equal(frostedTheme.SettingsWindowBg, settingsManager.Settings.SettingsWindowBg);
            Assert.Equal(frostedTheme.SettingsWindowSurface, settingsManager.Settings.SettingsWindowSurface);
            Assert.Equal(frostedTheme.SettingsWindowAccent, settingsManager.Settings.SettingsWindowAccent);
            Assert.Equal(frostedTheme.SettingsWindowOpacity, settingsManager.Settings.SettingsWindowOpacity);
            Assert.Equal(frostedTheme.SettingsSurfaceOpacity, settingsManager.Settings.SettingsSurfaceOpacity);
            Assert.Equal(frostedTheme.SettingsElementOpacity, settingsManager.Settings.SettingsElementOpacity);
            Assert.Equal(frostedTheme.SettingsWindowCornerRadius, settingsManager.Settings.SettingsWindowCornerRadius);
            Assert.True(refreshRequested);
            Assert.Equal(new[] { true, false }, bulkApplyStates);
        });
    }

    private static void InvokeSaveSettings(SettingsViewModel viewModel)
    {
        var method = typeof(SettingsViewModel).GetMethod("SaveSettings", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(viewModel, null);
    }

    private static AppSettings InvokeBuildSettingsThemePreviewSnapshot(SettingsViewModel viewModel)
    {
        var method = typeof(SettingsViewModel).GetMethod("BuildSettingsThemePreviewSnapshot", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return Assert.IsType<AppSettings>(method!.Invoke(viewModel, null));
    }

    private static void InvokeLoadProfile(SettingsViewModel viewModel, string name)
    {
        var method = typeof(SettingsViewModel).GetMethod("LoadProfile", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(viewModel, new object?[] { name });
    }

    private static void InvokeSaveProfile(SettingsViewModel viewModel)
    {
        var method = typeof(SettingsViewModel).GetMethod("SaveProfile", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(viewModel, null);
    }

    private static void InvokeApplyImportedSettings(SettingsViewModel viewModel, AppSettings settings)
    {
        var method = typeof(SettingsViewModel).GetMethod("ApplyImportedSettings", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(viewModel, new object?[] { settings });
    }

    private static void InvokeApplyTheme(SettingsViewModel viewModel, ThemePreset theme)
    {
        var method = typeof(SettingsViewModel).GetMethod("ApplyTheme", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(viewModel, new object?[] { theme });
    }

    private static ComboBox CreateBoundSettingsThemePresetCombo(SettingsViewModel viewModel)
    {
        var combo = new ComboBox();
        BindingOperations.SetBinding(combo, ItemsControl.ItemsSourceProperty, new Binding(nameof(SettingsViewModel.AvailableSettingsThemeModes))
        {
            Source = viewModel
        });
        BindingOperations.SetBinding(combo, Selector.SelectedItemProperty, new Binding(nameof(SettingsViewModel.SettingsThemeMode))
        {
            Source = viewModel,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        PumpDispatcher();
        return combo;
    }

    private static void PumpDispatcher()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(_ =>
        {
            frame.Continue = false;
            return null;
        }), null);
        Dispatcher.PushFrame(frame);
    }
}
