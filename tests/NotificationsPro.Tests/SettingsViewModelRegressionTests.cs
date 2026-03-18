using System.Reflection;
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
}
