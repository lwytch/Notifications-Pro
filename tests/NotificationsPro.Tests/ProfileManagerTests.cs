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
            CompactSettingsWindow = false,
            SettingsThemeMode = "Custom",
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
        Assert.False(loaded.CompactSettingsWindow);
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
