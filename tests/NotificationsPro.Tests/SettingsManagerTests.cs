using System.IO;
using NotificationsPro.Helpers;
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
        Assert.Equal("Left", sm.Settings.TextAlignment);
        Assert.Equal(0.94, sm.Settings.BackgroundOpacity);
        Assert.True(sm.Settings.AlwaysOnTop);
        Assert.Equal(40, sm.Settings.MaxVisibleNotifications);
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
        sm.Settings.TextAlignment = "Right";
        sm.Settings.BackgroundOpacity = 0.5;
        sm.Settings.ReadNotificationsAloudEnabled = true;
        sm.Settings.ReadNotificationsAloudMode = SpokenNotificationTextFormatter.ModeTitleBodyTimestamp;
        sm.Settings.SpokenMutedApps.Add("Teams");
        sm.Settings.VoiceAccessReadMode = VoiceAccessTextFormatter.ModeBodyOnly;
        sm.Settings.NotificationCaptureMode = NotificationCaptureModeHelper.ModeAccessibility;
        sm.Settings.AppGroupingStyle = "Header Chip";
        sm.Settings.ShowAppGroupCounts = false;
        sm.Settings.ShowQuickTips = false;
        sm.Settings.SecondaryOverlayEnabled = true;
        sm.Settings.SecondaryOverlayMonitorIndex = 1;
        sm.Settings.SecondaryOverlayPositionPreset = "Bottom Right";
        sm.Settings.SecondaryOverlayWidth = 512;
        sm.Settings.SecondaryOverlayMaxHeight = 720;
        sm.Settings.HighlightRules.Add(new HighlightRuleDefinition
        {
            Keyword = "urgent",
            Color = "#FF8800",
            Scope = NotificationMatchScopeHelper.TitleOnly,
            AppFilter = "X"
        });
        sm.Settings.MuteRules.Add(new MuteRuleDefinition
        {
            Keyword = "spoiler",
            Scope = NotificationMatchScopeHelper.BodyOnly
        });
        sm.Settings.NarrationRules.Add(new NarrationRuleDefinition
        {
            Keyword = "@openai",
            Scope = NotificationMatchScopeHelper.BodyOnly,
            Action = NarrationRuleActionHelper.ReadAloud,
            ReadMode = SpokenNotificationTextFormatter.ModeTitleOnly
        });
        sm.Settings.AppProfiles.Add(new AppProfile
        {
            AppName = "Codex",
            OverlayLane = OverlayLaneHelper.Secondary,
            IsReadAloudEnabled = false,
            Sound = "Default",
            Icon = "Mail",
            AccentColor = "#00AAFF",
            BackgroundImagePath = @"C:\NotificationsPro\backgrounds\codex.png"
        });
        sm.Save();

        var sm2 = CreateManager();
        sm2.Load();

        Assert.Equal(22, sm2.Settings.FontSize);
        Assert.Equal("Consolas", sm2.Settings.FontFamily);
        Assert.Equal("Right", sm2.Settings.TextAlignment);
        Assert.Equal(0.5, sm2.Settings.BackgroundOpacity);
        Assert.True(sm2.Settings.ReadNotificationsAloudEnabled);
        Assert.Equal(SpokenNotificationTextFormatter.ModeTitleBodyTimestamp, sm2.Settings.ReadNotificationsAloudMode);
        Assert.Equal(new[] { "Teams" }, sm2.Settings.SpokenMutedApps);
        Assert.Equal(VoiceAccessTextFormatter.ModeBodyOnly, sm2.Settings.VoiceAccessReadMode);
        Assert.Equal(NotificationCaptureModeHelper.ModeAccessibility, sm2.Settings.NotificationCaptureMode);
        Assert.Equal("Header Chip", sm2.Settings.AppGroupingStyle);
        Assert.False(sm2.Settings.ShowAppGroupCounts);
        Assert.False(sm2.Settings.ShowQuickTips);
        Assert.True(sm2.Settings.SecondaryOverlayEnabled);
        Assert.Equal(1, sm2.Settings.SecondaryOverlayMonitorIndex);
        Assert.Equal("Bottom Right", sm2.Settings.SecondaryOverlayPositionPreset);
        Assert.Equal(512, sm2.Settings.SecondaryOverlayWidth);
        Assert.Equal(720, sm2.Settings.SecondaryOverlayMaxHeight);
        Assert.Single(sm2.Settings.HighlightRules);
        Assert.Equal(NotificationMatchScopeHelper.TitleOnly, sm2.Settings.HighlightRules[0].Scope);
        Assert.Equal("X", sm2.Settings.HighlightRules[0].AppFilter);
        Assert.Single(sm2.Settings.MuteRules);
        Assert.Equal(NotificationMatchScopeHelper.BodyOnly, sm2.Settings.MuteRules[0].Scope);
        Assert.Single(sm2.Settings.NarrationRules);
        Assert.Equal(SpokenNotificationTextFormatter.ModeTitleOnly, sm2.Settings.NarrationRules[0].ReadMode);
        Assert.Single(sm2.Settings.AppProfiles);
        Assert.Equal(OverlayLaneHelper.Secondary, sm2.Settings.AppProfiles[0].OverlayLane);
    }

    [Fact]
    public void DefaultSettings_HaveExpectedValues()
    {
        var settings = new AppSettings();

        Assert.Equal("Segoe UI", settings.FontFamily);
        Assert.Equal(14, settings.FontSize);
        Assert.Equal("Normal", settings.FontWeight);
        Assert.Equal(1.5, settings.LineSpacing);
        Assert.Equal("Left", settings.TextAlignment);
        Assert.Equal("#E6E6E6", settings.TextColor);
        Assert.Equal("#FFFFFF", settings.TitleColor);
        Assert.Equal("#C8C8C8", settings.AppNameColor);
        Assert.Equal("#202020", settings.BackgroundColor);
        Assert.Equal(0.94, settings.BackgroundOpacity);
        Assert.Equal(14, settings.AppNameFontSize);
        Assert.Equal("SemiBold", settings.AppNameFontWeight);
        Assert.Equal(16, settings.TitleFontSize);
        Assert.Equal("SemiBold", settings.TitleFontWeight);
        Assert.Equal(20, settings.CornerRadius);
        Assert.Equal(16, settings.Padding);
        Assert.Equal(8, settings.CardGap);
        Assert.Equal(4, settings.OuterMargin);
        Assert.True(settings.ShowAccent);
        Assert.Equal(3, settings.AccentThickness);
        Assert.False(settings.ShowBorder);
        Assert.Equal("#3A3A3A", settings.BorderColor);
        Assert.Equal(1, settings.BorderThickness);
        Assert.Equal("#0078D4", settings.AccentColor);
        Assert.Equal(5, settings.NotificationDuration);
        Assert.Equal(40, settings.MaxVisibleNotifications);
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
        Assert.Equal(1200, settings.AnimationDurationMs);
        Assert.True(settings.ShowTimestamp);
        Assert.Equal(11, settings.TimestampFontSize);
        Assert.Equal("Relative", settings.TimestampDisplayMode);
        Assert.Equal("Normal", settings.TimestampFontWeight);
        Assert.Equal("#C8C8C8", settings.TimestampColor);
        Assert.True(settings.DeduplicationEnabled);
        Assert.Equal(2, settings.DeduplicationWindowSeconds);
        Assert.Empty(settings.MutedApps);
        Assert.Empty(settings.HighlightKeywords);
        Assert.Empty(settings.MuteKeywords);
        Assert.Equal("#FFD700", settings.HighlightColor);
        Assert.Empty(settings.SpokenMutedApps);
        Assert.False(settings.QuietHoursEnabled);
        Assert.Equal("22:00", settings.QuietHoursStart);
        Assert.Equal("08:00", settings.QuietHoursEnd);
        Assert.False(settings.BurstLimitEnabled);
        Assert.Equal(10, settings.BurstLimitCount);
        Assert.Equal(5, settings.BurstLimitWindowSeconds);
        Assert.Equal(340, settings.OverlayWidth);
        Assert.Equal(340, settings.LastManualOverlayWidth);
        Assert.Equal(480, settings.OverlayMaxHeight);
        Assert.False(settings.SecondaryOverlayEnabled);
        Assert.Equal("Top Left", settings.SecondaryOverlayPositionPreset);
        Assert.Equal(340, settings.SecondaryOverlayWidth);
        Assert.Equal(480, settings.SecondaryOverlayMaxHeight);
        Assert.True(settings.AllowManualResize);
        Assert.True(settings.SnapToEdges);
        Assert.Equal(20, settings.SnapDistance);
        Assert.False(settings.ReadNotificationsAloudEnabled);
        Assert.Equal(SpokenNotificationTextFormatter.ModeBodyOnly, settings.ReadNotificationsAloudMode);
        Assert.Equal(string.Empty, settings.ReadNotificationsAloudVoiceId);
        Assert.Equal(1.0, settings.ReadNotificationsAloudRate);
        Assert.Equal(1.0, settings.ReadNotificationsAloudVolume);
        Assert.True(settings.OverlayVisible);
        Assert.False(settings.NotificationsPaused);
        Assert.Equal(VoiceAccessTextFormatter.ModeOff, settings.VoiceAccessReadMode);
        Assert.Equal(NotificationCaptureModeHelper.ModeAuto, settings.NotificationCaptureMode);
        Assert.Equal("Framed Group", settings.AppGroupingStyle);
        Assert.True(settings.ShowAppGroupCounts);
        Assert.True(settings.ShowQuickTips);
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
        original.SpokenMutedApps.Add("Outlook");
        original.HighlightKeywords.Add("urgent");
        original.MuteKeywords.Add("spam");
        original.HighlightRules.Add(new HighlightRuleDefinition { Keyword = "headline" });
        original.MuteRules.Add(new MuteRuleDefinition { Keyword = "muted" });
        original.NarrationRules.Add(new NarrationRuleDefinition { Keyword = "@team" });
        original.AppProfiles.Add(new AppProfile { AppName = "Codex" });

        var clone = original.Clone();
        clone.MutedApps.Add("Slack");
        clone.SpokenMutedApps.Add("Discord");
        clone.HighlightKeywords.Add("critical");
        clone.MuteKeywords.Add("ad");
        clone.HighlightRules.Add(new HighlightRuleDefinition { Keyword = "body" });
        clone.MuteRules.Add(new MuteRuleDefinition { Keyword = "regex" });
        clone.NarrationRules.Add(new NarrationRuleDefinition { Keyword = "voice" });
        clone.AppProfiles.Add(new AppProfile { AppName = "Antigravity" });

        Assert.Single(original.MutedApps);
        Assert.Single(original.SpokenMutedApps);
        Assert.Single(original.HighlightKeywords);
        Assert.Single(original.MuteKeywords);
        Assert.Single(original.HighlightRules);
        Assert.Single(original.MuteRules);
        Assert.Single(original.NarrationRules);
        Assert.Single(original.AppProfiles);
        Assert.Equal(2, clone.MutedApps.Count);
        Assert.Equal(2, clone.SpokenMutedApps.Count);
        Assert.Equal(2, clone.HighlightKeywords.Count);
        Assert.Equal(2, clone.MuteKeywords.Count);
        Assert.Equal(2, clone.HighlightRules.Count);
        Assert.Equal(2, clone.MuteRules.Count);
        Assert.Equal(2, clone.NarrationRules.Count);
        Assert.Equal(2, clone.AppProfiles.Count);
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
        Assert.Equal(0.94, sm.Settings.BackgroundOpacity);
        Assert.Equal(40, sm.Settings.MaxVisibleNotifications);
    }

    [Theory]
    [InlineData("Bottom Right", "Bottom Right")]
    [InlineData("bottom-right", "Bottom Right")]
    [InlineData("middle center", "Middle Center")]
    [InlineData("unexpected", "Top Left")]
    public void SecondaryOverlayPositionHelper_NormalizesValues(string input, string expected)
    {
        Assert.Equal(expected, SecondaryOverlayPositionHelper.Normalize(input));
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
