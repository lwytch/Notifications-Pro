using NotificationsPro.Helpers;
using NotificationsPro.Models;
using NotificationsPro.Services;

namespace NotificationsPro.Tests;

public class UxPolishTests
{
    // --- AppSettings: M8 default values ---

    [Fact]
    public void DefaultSettings_HasShownWelcome_IsFalse()
    {
        var s = new AppSettings();
        Assert.False(s.HasShownWelcome);
    }

    [Fact]
    public void DefaultSettings_SettingsWindowPosition_IsNull()
    {
        var s = new AppSettings();
        Assert.Null(s.SettingsWindowLeft);
        Assert.Null(s.SettingsWindowTop);
    }

    // --- AppSettings: M8 properties persist through clone ---

    [Fact]
    public void Clone_PreservesM8Properties()
    {
        var s = new AppSettings
        {
            HasShownWelcome = true,
            SettingsWindowLeft = 100,
            SettingsWindowTop = 200
        };

        var clone = s.Clone();
        Assert.True(clone.HasShownWelcome);
        Assert.Equal(100, clone.SettingsWindowLeft);
        Assert.Equal(200, clone.SettingsWindowTop);
    }

    // --- AppSettings: M8 properties round-trip through JSON ---

    [Fact]
    public void SettingsRoundTrip_M8Properties()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"NotificationsPro_Test_{Guid.NewGuid():N}");
        try
        {
            var sm = new SettingsManager(tempDir);
            sm.Settings.HasShownWelcome = true;
            sm.Settings.SettingsWindowLeft = 150;
            sm.Settings.SettingsWindowTop = 250;
            sm.Save();

            var sm2 = new SettingsManager(tempDir);
            sm2.Load();
            Assert.True(sm2.Settings.HasShownWelcome);
            Assert.Equal(150, sm2.Settings.SettingsWindowLeft);
            Assert.Equal(250, sm2.Settings.SettingsWindowTop);
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    // --- IconHelper: tray icon variants ---

    [Fact]
    public void CreateTrayIcon_ReturnsNonNull()
    {
        var icon = IconHelper.CreateTrayIcon();
        Assert.NotNull(icon);
        icon.Dispose();
    }

    [Fact]
    public void CreateDimmedTrayIcon_ReturnsNonNull()
    {
        var icon = IconHelper.CreateDimmedTrayIcon();
        Assert.NotNull(icon);
        icon.Dispose();
    }

    [Fact]
    public void CreateBadgedTrayIcon_ReturnsNonNull_ForVariousCounts()
    {
        var icon1 = IconHelper.CreateBadgedTrayIcon(1);
        Assert.NotNull(icon1);
        icon1.Dispose();

        var icon5 = IconHelper.CreateBadgedTrayIcon(5);
        Assert.NotNull(icon5);
        icon5.Dispose();

        var icon10 = IconHelper.CreateBadgedTrayIcon(10);
        Assert.NotNull(icon10);
        icon10.Dispose();

        // Zero count should still create a valid icon (no badge)
        var icon0 = IconHelper.CreateBadgedTrayIcon(0);
        Assert.NotNull(icon0);
        icon0.Dispose();
    }

    // --- QueueManager: empty state tracking ---

    [Fact]
    public void QueueManager_EmptyInitially()
    {
        var sm = new SettingsManager();
        var queue = new QueueManager(sm);
        Assert.Empty(queue.VisibleNotifications);
        Assert.Equal(0, queue.OverflowCount);
    }

    [Fact]
    public void QueueManager_ClearAll_ReturnsToEmpty()
    {
        var sm = new SettingsManager();
        sm.Settings.AnimationDurationMs = 0;
        var queue = new QueueManager(sm);

        queue.AddNotification("App", "Test", "Body");
        Assert.Single(queue.VisibleNotifications);

        queue.ClearAll();
        Assert.Empty(queue.VisibleNotifications);
    }
}
