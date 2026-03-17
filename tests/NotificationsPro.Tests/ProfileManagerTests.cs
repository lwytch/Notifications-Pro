using System.IO;
using NotificationsPro.Helpers;
using NotificationsPro.Models;
using NotificationsPro.Services;

namespace NotificationsPro.Tests;

public class ProfileManagerTests : IDisposable
{
    private readonly string _tempDir;

    public ProfileManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "NotificationsProProfileTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public void SaveAndLoadProfile_RoundTripsFilteringAndSettingsWindowState()
    {
        var manager = new ProfileManager(_tempDir);
        var backgroundsDir = ManagedAssetPathHelper.GetRoot(ManagedAssetPathHelper.BackgroundsFolderName);
        var iconsDir = ManagedAssetPathHelper.GetRoot(ManagedAssetPathHelper.IconsFolderName);
        var soundsDir = ManagedAssetPathHelper.GetRoot(ManagedAssetPathHelper.SoundsFolderName);
        var profileBackgroundPath = Path.Combine(backgroundsDir, "profile.png");
        var profileIconPath = Path.Combine(iconsDir, "profile.png");
        var profileSoundPath = Path.Combine(soundsDir, "profile.wav");
        var settings = new AppSettings
        {
            HighlightBorderThickness = 2.5,
            HighlightAnimation = HighlightAnimationHelper.Pulse,
            HighlightBorderMode = HighlightBorderModeHelper.FullBorder,
            NotificationAnimationStyle = NotificationAnimationStyleHelper.ZoomFade,
            SlideInDirection = "Right",
            PersistentNotifications = true,
            AutoDurationEnabled = true,
            AutoDurationBaseSeconds = 9.5,
            AutoDurationSecondsPerLine = 3.5,
            AlwaysOnTop = false,
            ClickThrough = true,
            PresentationModeEnabled = true,
            PerAppTintEnabled = true,
            PerAppTintOpacity = 0.41,
            CompactSettingsWindow = false,
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
            CardBackgroundImagePath = profileBackgroundPath,
            DefaultIconPreset = profileIconPath,
            DefaultSound = profileSoundPath
        };
        settings.HighlightRules.Add(new HighlightRuleDefinition
        {
            Keyword = "urgent",
            Color = "#FF8800",
            Animation = HighlightAnimationHelper.Flash,
            BorderMode = HighlightBorderModeHelper.AccentSideOnly,
            OverlayOpacity = 0.61,
            BorderThickness = 4
        });
        settings.MuteRules.Add(new MuteRuleDefinition { Keyword = "spoiler", Scope = NotificationMatchScopeHelper.BodyOnly });
        settings.NarrationRules.Add(new NarrationRuleDefinition { Keyword = "@openai", ReadMode = SpokenNotificationTextFormatter.ModeTitleOnly });

        manager.SaveProfile("Streamer", settings);
        var rawJson = File.ReadAllText(Path.Combine(_tempDir, "Streamer.json"));
        Assert.Contains("\"CardBackgroundImagePath\": \"backgrounds/profile.png\"", rawJson);
        Assert.Contains("\"DefaultIconPreset\": \"icons/profile.png\"", rawJson);
        Assert.Contains("\"DefaultSound\": \"sounds/profile.wav\"", rawJson);
        var loaded = manager.LoadProfile("Streamer");

        Assert.NotNull(loaded);
        Assert.Equal(SettingsManager.CurrentSettingsSchemaVersion, loaded!.SettingsSchemaVersion);
        Assert.Equal(2.5, loaded.HighlightBorderThickness);
        Assert.Equal(NotificationAnimationStyleHelper.ZoomFade, loaded.NotificationAnimationStyle);
        Assert.Equal("Right", loaded.SlideInDirection);
        Assert.True(loaded.PersistentNotifications);
        Assert.True(loaded.AutoDurationEnabled);
        Assert.Equal(9.5, loaded.AutoDurationBaseSeconds);
        Assert.Equal(3.5, loaded.AutoDurationSecondsPerLine);
        Assert.False(loaded.AlwaysOnTop);
        Assert.True(loaded.ClickThrough);
        Assert.True(loaded.PresentationModeEnabled);
        Assert.True(loaded.PerAppTintEnabled);
        Assert.Equal(0.41, loaded.PerAppTintOpacity);
        Assert.False(loaded.CompactSettingsWindow);
        Assert.Equal("Custom", loaded.SettingsThemeMode);
        Assert.Equal("#0F1014", loaded.SettingsWindowBg);
        Assert.Equal("#181B21", loaded.SettingsWindowSurface);
        Assert.Equal("#232834", loaded.SettingsWindowSurfaceLight);
        Assert.Equal("#2C3140", loaded.SettingsWindowSurfaceHover);
        Assert.Equal("#F2F4F8", loaded.SettingsWindowText);
        Assert.Equal("#C7CDDA", loaded.SettingsWindowTextSecondary);
        Assert.Equal("#8B93A7", loaded.SettingsWindowTextMuted);
        Assert.Equal("#33AAFF", loaded.SettingsWindowAccent);
        Assert.Equal("#394257", loaded.SettingsWindowBorder);
        Assert.Equal(0.79, loaded.SettingsWindowOpacity);
        Assert.Equal(0.36, loaded.SettingsSurfaceOpacity);
        Assert.Equal(0.62, loaded.SettingsElementOpacity);
        Assert.Equal(29, loaded.SettingsWindowCornerRadius);
        Assert.Equal(profileBackgroundPath, loaded.CardBackgroundImagePath);
        Assert.Equal(profileIconPath, loaded.DefaultIconPreset);
        Assert.Equal(profileSoundPath, loaded.DefaultSound);
        Assert.Single(loaded.HighlightRules);
        Assert.Equal(HighlightAnimationHelper.Flash, loaded.HighlightRules[0].Animation);
        Assert.Equal(HighlightBorderModeHelper.AccentSideOnly, loaded.HighlightRules[0].BorderMode);
        Assert.Equal(0.61, loaded.HighlightRules[0].OverlayOpacity);
        Assert.Equal(4, loaded.HighlightRules[0].BorderThickness);
        Assert.Single(loaded.MuteRules);
        Assert.Single(loaded.NarrationRules);
    }
}
