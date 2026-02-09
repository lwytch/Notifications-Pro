using NotificationsPro.Models;
using NotificationsPro.Services;

namespace NotificationsPro.Tests;

public class SettingsManagerTests
{
    [Fact]
    public void Load_ReturnsDefaultsWhenNoFile()
    {
        var sm = new SettingsManager();
        // Load without a file existing — should return defaults
        sm.Load();

        Assert.Equal("Segoe UI", sm.Settings.FontFamily);
        Assert.Equal(14, sm.Settings.FontSize);
        Assert.Equal(0.92, sm.Settings.BackgroundOpacity);
        Assert.True(sm.Settings.AlwaysOnTop);
        Assert.Equal(3, sm.Settings.MaxVisibleNotifications);
    }

    [Fact]
    public void DefaultSettings_HaveExpectedValues()
    {
        var settings = new AppSettings();

        Assert.Equal("Segoe UI", settings.FontFamily);
        Assert.Equal(14, settings.FontSize);
        Assert.Equal("Normal", settings.FontWeight);
        Assert.Equal(1.5, settings.LineSpacing);
        Assert.Equal("#E4E4EF", settings.TextColor);
        Assert.Equal("#FFFFFF", settings.TitleColor);
        Assert.Equal("#1E1E2E", settings.BackgroundColor);
        Assert.Equal(0.92, settings.BackgroundOpacity);
        Assert.Equal(12, settings.CornerRadius);
        Assert.Equal(16, settings.Padding);
        Assert.True(settings.ShowBorder);
        Assert.Equal("#7C5CFC", settings.AccentColor);
        Assert.Equal(5, settings.NotificationDuration);
        Assert.Equal(3, settings.MaxVisibleNotifications);
        Assert.True(settings.AlwaysOnTop);
        Assert.False(settings.ClickThrough);
        Assert.True(settings.AnimationsEnabled);
        Assert.Equal(300, settings.AnimationDurationMs);
        Assert.Equal(380, settings.OverlayWidth);
        Assert.Equal(600, settings.OverlayMaxHeight);
        Assert.True(settings.SnapToEdges);
        Assert.Equal(20, settings.SnapDistance);
        Assert.True(settings.OverlayVisible);
        Assert.False(settings.NotificationsPaused);
    }

    [Fact]
    public void Clone_ReturnsIndependentCopy()
    {
        var original = new AppSettings { FontSize = 20, FontFamily = "Arial" };
        var clone = original.Clone();

        clone.FontSize = 30;
        clone.FontFamily = "Comic Sans MS";

        Assert.Equal(20, original.FontSize);
        Assert.Equal("Arial", original.FontFamily);
    }

    [Fact]
    public void ResetToDefaults_RestoresDefaultValues()
    {
        var sm = new SettingsManager();
        sm.Settings.FontSize = 99;
        sm.Settings.FontFamily = "Impact";
        sm.Settings.BackgroundOpacity = 0.1;

        sm.ResetToDefaults();

        Assert.Equal(14, sm.Settings.FontSize);
        Assert.Equal("Segoe UI", sm.Settings.FontFamily);
        Assert.Equal(0.92, sm.Settings.BackgroundOpacity);
    }

    [Fact]
    public void Apply_UpdatesSettings()
    {
        var sm = new SettingsManager();
        var updated = new AppSettings { FontSize = 22, FontFamily = "Consolas" };

        sm.Apply(updated);

        Assert.Equal(22, sm.Settings.FontSize);
        Assert.Equal("Consolas", sm.Settings.FontFamily);
    }

    [Fact]
    public void Apply_RaisesSettingsChanged()
    {
        var sm = new SettingsManager();
        bool eventFired = false;
        sm.SettingsChanged += () => eventFired = true;

        sm.Apply(new AppSettings());

        Assert.True(eventFired);
    }

    [Fact]
    public void Settings_NeverContainNotificationContent()
    {
        // Verify the AppSettings model has no fields that could hold notification content
        var props = typeof(AppSettings).GetProperties();
        var propNames = props.Select(p => p.Name).ToList();

        Assert.DoesNotContain("NotificationTitle", propNames);
        Assert.DoesNotContain("NotificationBody", propNames);
        Assert.DoesNotContain("NotificationContent", propNames);
        Assert.DoesNotContain("History", propNames);
        Assert.DoesNotContain("Log", propNames);
    }
}
