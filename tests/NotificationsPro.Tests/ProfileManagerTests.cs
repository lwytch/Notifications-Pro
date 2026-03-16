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
        var settings = new AppSettings
        {
            HighlightBorderThickness = 2.5,
            HighlightAnimation = HighlightAnimationHelper.Pulse,
            HighlightBorderMode = HighlightBorderModeHelper.FullBorder,
            CompactSettingsWindow = false,
            SettingsThemeMode = "Custom"
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
        var loaded = manager.LoadProfile("Streamer");

        Assert.NotNull(loaded);
        Assert.Equal(SettingsManager.CurrentSettingsSchemaVersion, loaded!.SettingsSchemaVersion);
        Assert.Equal(2.5, loaded.HighlightBorderThickness);
        Assert.False(loaded.CompactSettingsWindow);
        Assert.Single(loaded.HighlightRules);
        Assert.Equal(HighlightAnimationHelper.Flash, loaded.HighlightRules[0].Animation);
        Assert.Equal(HighlightBorderModeHelper.AccentSideOnly, loaded.HighlightRules[0].BorderMode);
        Assert.Equal(0.61, loaded.HighlightRules[0].OverlayOpacity);
        Assert.Equal(4, loaded.HighlightRules[0].BorderThickness);
        Assert.Single(loaded.MuteRules);
        Assert.Single(loaded.NarrationRules);
    }
}
