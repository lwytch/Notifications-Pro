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

    private static void InvokeSaveSettings(SettingsViewModel viewModel)
    {
        var method = typeof(SettingsViewModel).GetMethod("SaveSettings", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(viewModel, null);
    }

    private static void InvokeApplyTheme(SettingsViewModel viewModel, ThemePreset theme)
    {
        var method = typeof(SettingsViewModel).GetMethod("ApplyTheme", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(viewModel, new object?[] { theme });
    }
}
