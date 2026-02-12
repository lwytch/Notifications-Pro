using System.IO;
using NotificationsPro.Models;
using NotificationsPro.Services;

namespace NotificationsPro.Tests;

public class SettingsManagerTests : IDisposable
{
    private readonly string _tempDir;

    public SettingsManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "NotificationsProTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    private SettingsManager CreateManager() => new SettingsManager(_tempDir);

    [Fact]
    public void Load_ReturnsDefaultsWhenNoFile()
    {
        var sm = CreateManager();
        sm.Load();

        Assert.Equal("Segoe UI", sm.Settings.FontFamily);
        Assert.Equal(14, sm.Settings.FontSize);
        Assert.Equal(0.92, sm.Settings.BackgroundOpacity);
        Assert.True(sm.Settings.AlwaysOnTop);
        Assert.Equal(3, sm.Settings.MaxVisibleNotifications);
        Assert.True(sm.Settings.ShowAppName);
        Assert.True(sm.Settings.ShowNotificationTitle);
        Assert.True(sm.Settings.ShowNotificationBody);
        Assert.False(sm.Settings.LimitTextLines);
        Assert.Equal(2, sm.Settings.MaxAppNameLines);
        Assert.Equal(2, sm.Settings.MaxTitleLines);
        Assert.Equal(4, sm.Settings.MaxBodyLines);
        Assert.False(sm.Settings.SingleLineMode);
        Assert.False(sm.Settings.SingleLineWrapText);
        Assert.Equal(3, sm.Settings.SingleLineMaxLines);
        Assert.False(sm.Settings.SingleLineAutoFullWidth);
        Assert.True(sm.Settings.NewestOnTop);
    }

    [Fact]
    public void SaveAndLoad_RoundTrips()
    {
        var sm = CreateManager();
        sm.Settings.FontSize = 22;
        sm.Settings.FontFamily = "Consolas";
        sm.Settings.BackgroundOpacity = 0.5;
        sm.Save();

        var sm2 = CreateManager();
        sm2.Load();

        Assert.Equal(22, sm2.Settings.FontSize);
        Assert.Equal("Consolas", sm2.Settings.FontFamily);
        Assert.Equal(0.5, sm2.Settings.BackgroundOpacity);
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
        Assert.Equal("#B8B8CC", settings.AppNameColor);
        Assert.Equal("#1E1E2E", settings.BackgroundColor);
        Assert.Equal(0.92, settings.BackgroundOpacity);
        Assert.Equal(14, settings.AppNameFontSize);
        Assert.Equal("SemiBold", settings.AppNameFontWeight);
        Assert.Equal(16, settings.TitleFontSize);
        Assert.Equal("SemiBold", settings.TitleFontWeight);
        Assert.Equal(12, settings.CornerRadius);
        Assert.Equal(16, settings.Padding);
        Assert.Equal(8, settings.CardGap);
        Assert.Equal(4, settings.OuterMargin);
        Assert.True(settings.ShowAccent);
        Assert.Equal(3, settings.AccentThickness);
        Assert.False(settings.ShowBorder);
        Assert.Equal("#363650", settings.BorderColor);
        Assert.Equal(1, settings.BorderThickness);
        Assert.Equal("#7C5CFC", settings.AccentColor);
        Assert.Equal(5, settings.NotificationDuration);
        Assert.Equal(3, settings.MaxVisibleNotifications);
        Assert.True(settings.ShowAppName);
        Assert.True(settings.ShowNotificationTitle);
        Assert.True(settings.ShowNotificationBody);
        Assert.False(settings.LimitTextLines);
        Assert.Equal(2, settings.MaxAppNameLines);
        Assert.Equal(2, settings.MaxTitleLines);
        Assert.Equal(4, settings.MaxBodyLines);
        Assert.False(settings.SingleLineMode);
        Assert.False(settings.SingleLineWrapText);
        Assert.Equal(3, settings.SingleLineMaxLines);
        Assert.False(settings.SingleLineAutoFullWidth);
        Assert.True(settings.NewestOnTop);
        Assert.True(settings.AlwaysOnTop);
        Assert.False(settings.ClickThrough);
        Assert.True(settings.AnimationsEnabled);
        Assert.False(settings.FadeOnlyAnimation);
        Assert.Equal("Left", settings.SlideInDirection);
        Assert.Equal(300, settings.AnimationDurationMs);
        Assert.False(settings.ShowTimestamp);
        Assert.True(settings.DeduplicationEnabled);
        Assert.Equal(2, settings.DeduplicationWindowSeconds);
        Assert.Empty(settings.MutedApps);
        Assert.Empty(settings.HighlightKeywords);
        Assert.Empty(settings.MuteKeywords);
        Assert.Equal("#FFD700", settings.HighlightColor);
        Assert.False(settings.QuietHoursEnabled);
        Assert.Equal("22:00", settings.QuietHoursStart);
        Assert.Equal("08:00", settings.QuietHoursEnd);
        Assert.False(settings.BurstLimitEnabled);
        Assert.Equal(10, settings.BurstLimitCount);
        Assert.Equal(5, settings.BurstLimitWindowSeconds);
        Assert.Equal(380, settings.OverlayWidth);
        Assert.Equal(380, settings.LastManualOverlayWidth);
        Assert.Equal(600, settings.OverlayMaxHeight);
        Assert.True(settings.AllowManualResize);
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
    public void Clone_DeepCopiesLists()
    {
        var original = new AppSettings();
        original.MutedApps.Add("Teams");
        original.HighlightKeywords.Add("urgent");
        original.MuteKeywords.Add("spam");

        var clone = original.Clone();
        clone.MutedApps.Add("Slack");
        clone.HighlightKeywords.Add("critical");
        clone.MuteKeywords.Add("ad");

        Assert.Single(original.MutedApps);
        Assert.Single(original.HighlightKeywords);
        Assert.Single(original.MuteKeywords);
        Assert.Equal(2, clone.MutedApps.Count);
        Assert.Equal(2, clone.HighlightKeywords.Count);
        Assert.Equal(2, clone.MuteKeywords.Count);
    }

    [Fact]
    public void ResetToDefaults_RestoresDefaultValues()
    {
        var sm = CreateManager();
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
        var sm = CreateManager();
        var updated = new AppSettings { FontSize = 22, FontFamily = "Consolas" };

        sm.Apply(updated);

        Assert.Equal(22, sm.Settings.FontSize);
        Assert.Equal("Consolas", sm.Settings.FontFamily);
    }

    [Fact]
    public void Apply_RaisesSettingsChanged()
    {
        var sm = CreateManager();
        bool eventFired = false;
        sm.SettingsChanged += () => eventFired = true;

        sm.Apply(new AppSettings());

        Assert.True(eventFired);
    }

    [Fact]
    public void Load_HandlesCorruptedFile()
    {
        File.WriteAllText(Path.Combine(_tempDir, "settings.json"), "NOT VALID JSON {{{");

        var sm = CreateManager();
        sm.Load();

        // Should fall back to defaults
        Assert.Equal("Segoe UI", sm.Settings.FontFamily);
        Assert.Equal(14, sm.Settings.FontSize);
    }

    [Fact]
    public void Settings_NeverContainNotificationContent()
    {
        var props = typeof(AppSettings).GetProperties();
        var propNames = props.Select(p => p.Name).ToList();

        Assert.DoesNotContain("NotificationTitle", propNames);
        Assert.DoesNotContain("NotificationBody", propNames);
        Assert.DoesNotContain("NotificationContent", propNames);
        Assert.DoesNotContain("History", propNames);
        Assert.DoesNotContain("Log", propNames);
    }
}
