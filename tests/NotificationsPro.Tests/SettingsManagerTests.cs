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

        Assert.False(sm.HadExistingSettingsFile);
        Assert.Equal("Segoe UI", sm.Settings.FontFamily);
        Assert.Equal(14, sm.Settings.FontSize);
        Assert.Equal("Left", sm.Settings.TextAlignment);
        Assert.Equal(0.94, sm.Settings.BackgroundOpacity);
        Assert.True(sm.Settings.AlwaysOnTop);
        Assert.Equal(AppSettings.DefaultMaxVisibleNotifications, sm.Settings.MaxVisibleNotifications);
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
        Assert.True(sm.Settings.OverlayScrollbarVisible);
        Assert.Equal(10, sm.Settings.OverlayScrollbarContentGap);
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
        sm.Settings.ReadNotificationsAloudTriggerMode = NarrationTriggerModeHelper.OnlyMatchingNarrationRules;
        sm.Settings.ReadNotificationsAloudMode = SpokenNotificationTextFormatter.ModeTitleBodyTimestamp;
        sm.Settings.SpokenMutedApps.Add("Teams");
        sm.Settings.VoiceAccessReadMode = VoiceAccessTextFormatter.ModeBodyOnly;
        sm.Settings.NotificationCaptureMode = NotificationCaptureModeHelper.ModeAccessibility;
        sm.Settings.AppGroupingStyle = "Header Chip";
        sm.Settings.ShowAppGroupCounts = false;
        sm.Settings.CardBackgroundMode = CardBackgroundModeHelper.Image;
        sm.Settings.CardBackgroundImagePath = @"C:\Users\demo\AppData\Roaming\NotificationsPro\backgrounds\social.png";
        sm.Settings.CardBackgroundImageOpacity = 0.6;
        sm.Settings.CardBackgroundImageHueDegrees = 30;
        sm.Settings.CardBackgroundImageBrightness = 0.8;
        sm.Settings.CardBackgroundImageSaturation = 0.55;
        sm.Settings.CardBackgroundImageContrast = 1.35;
        sm.Settings.CardBackgroundImageBlackAndWhite = true;
        sm.Settings.CardBackgroundImageFitMode = CardBackgroundImageFitModeHelper.FitInsideCard;
        sm.Settings.CardBackgroundImagePlacement = CardBackgroundImagePlacementHelper.FullCard;
        sm.Settings.CardBackgroundImageVerticalFocus = ImageVerticalFocusHelper.Top;
        sm.Settings.OverlayScrollbarTrackColor = "#101010";
        sm.Settings.OverlayScrollbarThumbColor = "#6A6A6A";
        sm.Settings.OverlayScrollbarThumbHoverColor = "#44AAFF";
        sm.Settings.OverlayScrollbarPadding = 2.5;
        sm.Settings.OverlayScrollbarContentGap = 12;
        sm.Settings.OverlayScrollbarCornerRadius = 7;
        sm.Settings.PerAppBackgroundImages["X"] = @"C:\Users\demo\AppData\Roaming\NotificationsPro\backgrounds\x.png";
        sm.Settings.FullscreenOverlayImagePath = @"C:\Users\demo\AppData\Roaming\NotificationsPro\backgrounds\wallpaper.png";
        sm.Settings.FullscreenOverlayImageFitMode = CardBackgroundImageFitModeHelper.OriginalSize;
        sm.Settings.FullscreenOverlayImageHueDegrees = -45;
        sm.Settings.FullscreenOverlayImageBrightness = 0.9;
        sm.Settings.FullscreenOverlayImageSaturation = 0.25;
        sm.Settings.FullscreenOverlayImageContrast = 1.4;
        sm.Settings.FullscreenOverlayImageBlackAndWhite = true;
        sm.Settings.FullscreenOverlayImageVerticalFocus = ImageVerticalFocusHelper.Bottom;
        sm.Settings.ShowQuickTips = false;
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
        sm.Save();

        var sm2 = CreateManager();
        sm2.Load();

        Assert.True(sm2.HadExistingSettingsFile);
        Assert.Equal(SettingsManager.CurrentSettingsSchemaVersion, sm2.Settings.SettingsSchemaVersion);
        Assert.Equal(22, sm2.Settings.FontSize);
        Assert.Equal("Consolas", sm2.Settings.FontFamily);
        Assert.Equal("Right", sm2.Settings.TextAlignment);
        Assert.Equal(0.5, sm2.Settings.BackgroundOpacity);
        Assert.True(sm2.Settings.ReadNotificationsAloudEnabled);
        Assert.Equal(NarrationTriggerModeHelper.OnlyMatchingNarrationRules, sm2.Settings.ReadNotificationsAloudTriggerMode);
        Assert.Equal(SpokenNotificationTextFormatter.ModeTitleBodyTimestamp, sm2.Settings.ReadNotificationsAloudMode);
        Assert.Equal(new[] { "Teams" }, sm2.Settings.SpokenMutedApps);
        Assert.Equal(VoiceAccessTextFormatter.ModeBodyOnly, sm2.Settings.VoiceAccessReadMode);
        Assert.Equal(NotificationCaptureModeHelper.ModeAccessibility, sm2.Settings.NotificationCaptureMode);
        Assert.Equal("Header Chip", sm2.Settings.AppGroupingStyle);
        Assert.False(sm2.Settings.ShowAppGroupCounts);
        Assert.Equal(CardBackgroundModeHelper.Image, sm2.Settings.CardBackgroundMode);
        Assert.Equal(@"C:\Users\demo\AppData\Roaming\NotificationsPro\backgrounds\social.png", sm2.Settings.CardBackgroundImagePath);
        Assert.Equal(0.6, sm2.Settings.CardBackgroundImageOpacity);
        Assert.Equal(30, sm2.Settings.CardBackgroundImageHueDegrees);
        Assert.Equal(0.8, sm2.Settings.CardBackgroundImageBrightness);
        Assert.Equal(0.55, sm2.Settings.CardBackgroundImageSaturation);
        Assert.Equal(1.35, sm2.Settings.CardBackgroundImageContrast);
        Assert.True(sm2.Settings.CardBackgroundImageBlackAndWhite);
        Assert.Equal(CardBackgroundImageFitModeHelper.FitInsideCard, sm2.Settings.CardBackgroundImageFitMode);
        Assert.Equal(CardBackgroundImagePlacementHelper.FullCard, sm2.Settings.CardBackgroundImagePlacement);
        Assert.Equal(ImageVerticalFocusHelper.Top, sm2.Settings.CardBackgroundImageVerticalFocus);
        Assert.Equal("#101010", sm2.Settings.OverlayScrollbarTrackColor);
        Assert.Equal("#6A6A6A", sm2.Settings.OverlayScrollbarThumbColor);
        Assert.Equal("#44AAFF", sm2.Settings.OverlayScrollbarThumbHoverColor);
        Assert.Equal(2.5, sm2.Settings.OverlayScrollbarPadding);
        Assert.Equal(12, sm2.Settings.OverlayScrollbarContentGap);
        Assert.Equal(7, sm2.Settings.OverlayScrollbarCornerRadius);
        Assert.Equal(@"C:\Users\demo\AppData\Roaming\NotificationsPro\backgrounds\x.png", sm2.Settings.PerAppBackgroundImages["X"]);
        Assert.Equal(@"C:\Users\demo\AppData\Roaming\NotificationsPro\backgrounds\wallpaper.png", sm2.Settings.FullscreenOverlayImagePath);
        Assert.Equal(CardBackgroundImageFitModeHelper.OriginalSize, sm2.Settings.FullscreenOverlayImageFitMode);
        Assert.Equal(-45, sm2.Settings.FullscreenOverlayImageHueDegrees);
        Assert.Equal(0.9, sm2.Settings.FullscreenOverlayImageBrightness);
        Assert.Equal(0.25, sm2.Settings.FullscreenOverlayImageSaturation);
        Assert.Equal(1.4, sm2.Settings.FullscreenOverlayImageContrast);
        Assert.True(sm2.Settings.FullscreenOverlayImageBlackAndWhite);
        Assert.Equal(ImageVerticalFocusHelper.Bottom, sm2.Settings.FullscreenOverlayImageVerticalFocus);
        Assert.False(sm2.Settings.ShowQuickTips);
        Assert.Single(sm2.Settings.HighlightRules);
        Assert.Equal(NotificationMatchScopeHelper.TitleOnly, sm2.Settings.HighlightRules[0].Scope);
        Assert.Equal("X", sm2.Settings.HighlightRules[0].AppFilter);
        Assert.Single(sm2.Settings.MuteRules);
        Assert.Equal(NotificationMatchScopeHelper.BodyOnly, sm2.Settings.MuteRules[0].Scope);
        Assert.Single(sm2.Settings.NarrationRules);
        Assert.Equal(SpokenNotificationTextFormatter.ModeTitleOnly, sm2.Settings.NarrationRules[0].ReadMode);
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
        Assert.Equal(CardBackgroundModeHelper.Solid, settings.CardBackgroundMode);
        Assert.Equal(string.Empty, settings.CardBackgroundImagePath);
        Assert.Equal(0.45, settings.CardBackgroundImageOpacity);
        Assert.Equal(0, settings.CardBackgroundImageHueDegrees);
        Assert.Equal(1.0, settings.CardBackgroundImageBrightness);
        Assert.Equal(1.0, settings.CardBackgroundImageSaturation);
        Assert.Equal(1.0, settings.CardBackgroundImageContrast);
        Assert.False(settings.CardBackgroundImageBlackAndWhite);
        Assert.Equal(CardBackgroundImageFitModeHelper.FillCard, settings.CardBackgroundImageFitMode);
        Assert.Equal(CardBackgroundImagePlacementHelper.InsidePadding, settings.CardBackgroundImagePlacement);
        Assert.Equal(ImageVerticalFocusHelper.Center, settings.CardBackgroundImageVerticalFocus);
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
        Assert.Equal(AppSettings.DefaultMaxVisibleNotifications, settings.MaxVisibleNotifications);
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
        Assert.Empty(settings.HighlightRules);
        Assert.Empty(settings.MuteRules);
        Assert.Empty(settings.NarrationRules);
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
        Assert.True(settings.AllowManualResize);
        Assert.True(settings.SnapToEdges);
        Assert.Equal(20, settings.SnapDistance);
        Assert.True(settings.OverlayScrollbarVisible);
        Assert.Equal(8, settings.OverlayScrollbarWidth);
        Assert.Equal(1.0, settings.OverlayScrollbarOpacity);
        Assert.Equal("#141414", settings.OverlayScrollbarTrackColor);
        Assert.Equal("#4F4F4F", settings.OverlayScrollbarThumbColor);
        Assert.Equal("#0078D4", settings.OverlayScrollbarThumbHoverColor);
        Assert.Equal(1.5, settings.OverlayScrollbarPadding);
        Assert.Equal(10, settings.OverlayScrollbarContentGap);
        Assert.Equal(6, settings.OverlayScrollbarCornerRadius);
        Assert.False(settings.ReadNotificationsAloudEnabled);
        Assert.Equal(NarrationTriggerModeHelper.AllAllowedNotifications, settings.ReadNotificationsAloudTriggerMode);
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
        Assert.Empty(settings.PerAppBackgroundImages);
        Assert.Equal(string.Empty, settings.FullscreenOverlayImagePath);
        Assert.Equal(CardBackgroundImageFitModeHelper.FillCard, settings.FullscreenOverlayImageFitMode);
        Assert.Equal(0, settings.FullscreenOverlayImageHueDegrees);
        Assert.Equal(1.0, settings.FullscreenOverlayImageBrightness);
        Assert.Equal(1.0, settings.FullscreenOverlayImageSaturation);
        Assert.Equal(1.0, settings.FullscreenOverlayImageContrast);
        Assert.False(settings.FullscreenOverlayImageBlackAndWhite);
        Assert.Equal(ImageVerticalFocusHelper.Center, settings.FullscreenOverlayImageVerticalFocus);
        Assert.True(settings.ShowQuickTips);
    }

    [Fact]
    public void Load_NormalizesLegacyAndOutOfRangeBackgroundSettings()
    {
        var settingsPath = Path.Combine(_tempDir, "settings.json");
        File.WriteAllText(settingsPath,
            """
            {
              "MaxVisibleNotifications": 5000,
              "CardBackgroundImagePath": "C:\\Users\\demo\\AppData\\Roaming\\NotificationsPro\\backgrounds\\legacy.png",
              "CardBackgroundImageSaturation": 5.0,
              "CardBackgroundImageContrast": -2.0,
              "CardBackgroundImageVerticalFocus": "Somewhere",
              "FullscreenOverlayImageSaturation": 3.0,
              "FullscreenOverlayImageContrast": 9.0,
              "FullscreenOverlayImageVerticalFocus": "Elsewhere",
              "OverlayScrollbarContentGap": 99.0
            }
            """);

        var sm = CreateManager();
        sm.Load();

        Assert.Equal(AppSettings.MaxVisibleNotificationsUpperBound, sm.Settings.MaxVisibleNotifications);
        Assert.Equal(CardBackgroundModeHelper.Image, sm.Settings.CardBackgroundMode);
        Assert.Equal(2.0, sm.Settings.CardBackgroundImageSaturation);
        Assert.Equal(0.2, sm.Settings.CardBackgroundImageContrast);
        Assert.Equal(ImageVerticalFocusHelper.Center, sm.Settings.CardBackgroundImageVerticalFocus);
        Assert.Equal(2.0, sm.Settings.FullscreenOverlayImageSaturation);
        Assert.Equal(2.0, sm.Settings.FullscreenOverlayImageContrast);
        Assert.Equal(ImageVerticalFocusHelper.Center, sm.Settings.FullscreenOverlayImageVerticalFocus);
        Assert.Equal(24.0, sm.Settings.OverlayScrollbarContentGap);
    }

    [Fact]
    public void Load_RepairsBuggySchemaThreeStartupDefaults()
    {
        var settingsPath = Path.Combine(_tempDir, "settings.json");
        File.WriteAllText(settingsPath,
            """
            {
              "SettingsSchemaVersion": 3,
              "MaxVisibleNotifications": 3,
              "AnimationDurationMs": 0,
              "OverlayMaxHeight": 480
            }
            """);

        var sm = CreateManager();
        sm.Load();

        Assert.Equal(SettingsManager.CurrentSettingsSchemaVersion, sm.Settings.SettingsSchemaVersion);
        Assert.Equal(AppSettings.DefaultMaxVisibleNotifications, sm.Settings.MaxVisibleNotifications);
        Assert.Equal(1200, sm.Settings.AnimationDurationMs);
        Assert.True(sm.Settings.OverlayMaxHeight > 480);
    }

    [Fact]
    public void Load_RepairsLegacyStartupSignatureEvenWhenSchemaAlreadyCurrent()
    {
        var settingsPath = Path.Combine(_tempDir, "settings.json");
        File.WriteAllText(settingsPath,
            $$"""
            {
              "SettingsSchemaVersion": {{SettingsManager.CurrentSettingsSchemaVersion}},
              "MaxVisibleNotifications": 3,
              "AnimationDurationMs": 0,
              "OverlayMaxHeight": 480
            }
            """);

        var sm = CreateManager();
        sm.Load();

        Assert.Equal(SettingsManager.CurrentSettingsSchemaVersion, sm.Settings.SettingsSchemaVersion);
        Assert.Equal(AppSettings.DefaultMaxVisibleNotifications, sm.Settings.MaxVisibleNotifications);
        Assert.Equal(1200, sm.Settings.AnimationDurationMs);
        Assert.True(sm.Settings.OverlayMaxHeight > 480);
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
        original.PerAppBackgroundImages["X"] = @"C:\Users\demo\AppData\Roaming\NotificationsPro\backgrounds\x.png";

        var clone = original.Clone();
        clone.MutedApps.Add("Slack");
        clone.SpokenMutedApps.Add("Discord");
        clone.HighlightKeywords.Add("critical");
        clone.MuteKeywords.Add("ad");
        clone.HighlightRules.Add(new HighlightRuleDefinition { Keyword = "body" });
        clone.MuteRules.Add(new MuteRuleDefinition { Keyword = "regex" });
        clone.NarrationRules.Add(new NarrationRuleDefinition { Keyword = "voice" });
        clone.PerAppBackgroundImages["Slack"] = @"C:\Users\demo\AppData\Roaming\NotificationsPro\backgrounds\slack.png";

        Assert.Single(original.MutedApps);
        Assert.Single(original.SpokenMutedApps);
        Assert.Single(original.HighlightKeywords);
        Assert.Single(original.MuteKeywords);
        Assert.Single(original.HighlightRules);
        Assert.Single(original.MuteRules);
        Assert.Single(original.NarrationRules);
        Assert.Equal(2, clone.MutedApps.Count);
        Assert.Equal(2, clone.SpokenMutedApps.Count);
        Assert.Equal(2, clone.HighlightKeywords.Count);
        Assert.Equal(2, clone.MuteKeywords.Count);
        Assert.Equal(2, clone.HighlightRules.Count);
        Assert.Equal(2, clone.MuteRules.Count);
        Assert.Equal(2, clone.NarrationRules.Count);
        Assert.Single(original.PerAppBackgroundImages);
        Assert.Equal(2, clone.PerAppBackgroundImages.Count);
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
        Assert.Equal(AppSettings.DefaultMaxVisibleNotifications, sm.Settings.MaxVisibleNotifications);
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
