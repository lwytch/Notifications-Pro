using NotificationsPro.Helpers;
using NotificationsPro.Models;
using NotificationsPro.Services;

namespace NotificationsPro.Tests;

public class StartupSettingsMigrationHelperTests
{
    [Fact]
    public void Apply_FirstRun_UsesCurrentDefaultsAndMonitorHeight()
    {
        var settings = new AppSettings
        {
            MaxVisibleNotifications = 3,
            AnimationDurationMs = 300,
            OverlayMaxHeight = 480
        };

        var changed = StartupSettingsMigrationHelper.Apply(settings, 1440, hadExistingSettingsFile: false);

        Assert.True(changed);
        Assert.Equal(40, settings.MaxVisibleNotifications);
        Assert.Equal(1200, settings.AnimationDurationMs);
        Assert.Equal(1440, settings.OverlayMaxHeight);
        Assert.Equal(SettingsManager.CurrentSettingsSchemaVersion, settings.SettingsSchemaVersion);
    }

    [Fact]
    public void Apply_LegacySettings_MigratesOnlyOnce()
    {
        var settings = new AppSettings
        {
            SettingsSchemaVersion = null,
            MaxVisibleNotifications = 3,
            AnimationDurationMs = 300,
            OverlayMaxHeight = 480
        };

        var changed = StartupSettingsMigrationHelper.Apply(settings, 1600, hadExistingSettingsFile: true);

        Assert.True(changed);
        Assert.Equal(40, settings.MaxVisibleNotifications);
        Assert.Equal(1200, settings.AnimationDurationMs);
        Assert.Equal(1600, settings.OverlayMaxHeight);
        Assert.Equal(SettingsManager.CurrentSettingsSchemaVersion, settings.SettingsSchemaVersion);
    }

    [Fact]
    public void Apply_CurrentSettings_DoesNotOverrideIntentionalValues()
    {
        var settings = new AppSettings
        {
            SettingsSchemaVersion = SettingsManager.CurrentSettingsSchemaVersion,
            MaxVisibleNotifications = 3,
            AnimationDurationMs = 0,
            OverlayMaxHeight = 720
        };

        var changed = StartupSettingsMigrationHelper.Apply(settings, 2160, hadExistingSettingsFile: true);

        Assert.False(changed);
        Assert.Equal(3, settings.MaxVisibleNotifications);
        Assert.Equal(0, settings.AnimationDurationMs);
        Assert.Equal(720, settings.OverlayMaxHeight);
    }
}
