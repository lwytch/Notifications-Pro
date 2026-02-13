using NotificationsPro.Helpers;
using NotificationsPro.Models;
using NotificationsPro.Services;

namespace NotificationsPro.Tests;

public class StreamingPresentationTests
{
    // --- AppSettings: M10 default values ---

    [Fact]
    public void DefaultSettings_ChromaKeyEnabled_IsFalse()
    {
        var s = new AppSettings();
        Assert.False(s.ChromaKeyEnabled);
    }

    [Fact]
    public void DefaultSettings_ChromaKeyColor_IsGreen()
    {
        var s = new AppSettings();
        Assert.Equal("#00FF00", s.ChromaKeyColor);
    }

    [Fact]
    public void DefaultSettings_ObsFixedWindowMode_IsFalse()
    {
        var s = new AppSettings();
        Assert.False(s.ObsFixedWindowMode);
        Assert.Equal(400, s.ObsFixedWidth);
        Assert.Equal(600, s.ObsFixedHeight);
    }

    [Fact]
    public void DefaultSettings_PresentationMode_Defaults()
    {
        var s = new AppSettings();
        Assert.False(s.PresentationModeEnabled);
        Assert.Equal(4, s.PresentationApps.Count);
        Assert.Contains("PowerPoint", s.PresentationApps);
        Assert.Contains("Zoom", s.PresentationApps);
        Assert.Contains("Google Meet", s.PresentationApps);
        Assert.Contains("Microsoft Teams", s.PresentationApps);
    }

    [Fact]
    public void DefaultSettings_PerAppTint_Defaults()
    {
        var s = new AppSettings();
        Assert.False(s.PerAppTintEnabled);
        Assert.Equal(0.15, s.PerAppTintOpacity);
    }

    // --- Clone ---

    [Fact]
    public void Clone_PreservesM10Properties()
    {
        var s = new AppSettings
        {
            ChromaKeyEnabled = true,
            ChromaKeyColor = "#0000FF",
            ObsFixedWindowMode = true,
            ObsFixedWidth = 500,
            ObsFixedHeight = 700,
            PresentationModeEnabled = true,
            PerAppTintEnabled = true,
            PerAppTintOpacity = 0.25,
        };

        var clone = s.Clone();
        Assert.True(clone.ChromaKeyEnabled);
        Assert.Equal("#0000FF", clone.ChromaKeyColor);
        Assert.True(clone.ObsFixedWindowMode);
        Assert.Equal(500, clone.ObsFixedWidth);
        Assert.Equal(700, clone.ObsFixedHeight);
        Assert.True(clone.PresentationModeEnabled);
        Assert.True(clone.PerAppTintEnabled);
        Assert.Equal(0.25, clone.PerAppTintOpacity);
    }

    [Fact]
    public void Clone_DeepCopiesPresentationApps()
    {
        var original = new AppSettings();
        original.PresentationApps.Add("OBS");

        var clone = original.Clone();
        clone.PresentationApps.Add("Discord");

        Assert.Equal(5, original.PresentationApps.Count); // 4 defaults + "OBS"
        Assert.Equal(6, clone.PresentationApps.Count); // 5 + "Discord"
        Assert.DoesNotContain("Discord", original.PresentationApps);
    }

    // --- JSON round-trip ---

    [Fact]
    public void SettingsRoundTrip_M10Properties()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"NotificationsPro_Test_{Guid.NewGuid():N}");
        try
        {
            var sm = new SettingsManager(tempDir);
            sm.Settings.ChromaKeyEnabled = true;
            sm.Settings.ChromaKeyColor = "#FF00FF";
            sm.Settings.ObsFixedWindowMode = true;
            sm.Settings.ObsFixedWidth = 640;
            sm.Settings.ObsFixedHeight = 480;
            sm.Settings.PresentationModeEnabled = true;
            sm.Settings.PresentationApps = new List<string> { "OBS", "Keynote" };
            sm.Settings.PerAppTintEnabled = true;
            sm.Settings.PerAppTintOpacity = 0.30;
            sm.Save();

            var sm2 = new SettingsManager(tempDir);
            sm2.Load();
            Assert.True(sm2.Settings.ChromaKeyEnabled);
            Assert.Equal("#FF00FF", sm2.Settings.ChromaKeyColor);
            Assert.True(sm2.Settings.ObsFixedWindowMode);
            Assert.Equal(640, sm2.Settings.ObsFixedWidth);
            Assert.Equal(480, sm2.Settings.ObsFixedHeight);
            Assert.True(sm2.Settings.PresentationModeEnabled);
            Assert.Equal(2, sm2.Settings.PresentationApps.Count);
            Assert.Contains("OBS", sm2.Settings.PresentationApps);
            Assert.Contains("Keynote", sm2.Settings.PresentationApps);
            Assert.True(sm2.Settings.PerAppTintEnabled);
            Assert.Equal(0.30, sm2.Settings.PerAppTintOpacity);
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    // --- AppTintHelper ---

    [Fact]
    public void AppTintHelper_ReturnsDeterministicColor()
    {
        var color1 = AppTintHelper.GetTintColor("Discord");
        var color2 = AppTintHelper.GetTintColor("Discord");
        Assert.Equal(color1, color2);
    }

    [Fact]
    public void AppTintHelper_DifferentAppsGetDifferentColors()
    {
        var discord = AppTintHelper.GetTintColor("Discord");
        var slack = AppTintHelper.GetTintColor("Slack");
        var teams = AppTintHelper.GetTintColor("Microsoft Teams");

        // At least some should differ (statistically extremely likely with 3 different strings)
        Assert.False(discord == slack && slack == teams,
            "All three apps mapped to the same color — hash distribution issue");
    }

    [Fact]
    public void AppTintHelper_EmptyOrNull_ReturnsPaletteZero()
    {
        var empty = AppTintHelper.GetTintColor("");
        var whitespace = AppTintHelper.GetTintColor("   ");
        Assert.Equal(empty, whitespace);
    }

    [Fact]
    public void AppTintHelper_ReturnsValidHexColor()
    {
        var color = AppTintHelper.GetTintColor("TestApp");
        Assert.Matches(@"^#[0-9A-Fa-f]{6}$", color);
    }

    // --- FullscreenHelper ---

    [Fact]
    public void FullscreenHelper_IsPresentationApp_EmptyList_ReturnsFalse()
    {
        // With an empty list, no app should match
        var result = FullscreenHelper.IsPresentationAppFullscreen(Array.Empty<string>());
        Assert.False(result);
    }
}
