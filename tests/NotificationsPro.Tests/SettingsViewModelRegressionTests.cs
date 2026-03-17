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

    private static void InvokeSaveSettings(SettingsViewModel viewModel)
    {
        var method = typeof(SettingsViewModel).GetMethod("SaveSettings", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(viewModel, null);
    }
}
