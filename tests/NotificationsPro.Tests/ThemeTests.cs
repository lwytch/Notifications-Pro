using System.IO;
using NotificationsPro.Helpers;
using NotificationsPro.Models;
using NotificationsPro.Services;

namespace NotificationsPro.Tests;

public class ThemeTests : IDisposable
{
    private readonly string _tempDir;

    public ThemeTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "NotificationsProThemeTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    // --- ThemePreset ---

    [Fact]
    public void BuiltInThemes_HasExpectedCount()
    {
        Assert.Equal(7, ThemePreset.BuiltInThemes.Length);
    }

    [Fact]
    public void BuiltInThemes_AllHaveNames()
    {
        foreach (var theme in ThemePreset.BuiltInThemes)
            Assert.False(string.IsNullOrWhiteSpace(theme.Name));
    }

    [Fact]
    public void ApplyTo_SetsAllVisualProperties()
    {
        var theme = new ThemePreset
        {
            Name = "Test",
            TextColor = "#111111",
            TitleColor = "#222222",
            AppNameColor = "#333333",
            BackgroundColor = "#444444",
            BackgroundOpacity = 0.5,
            AccentColor = "#555555",
            HighlightColor = "#666666",
            BorderColor = "#777777",
            CornerRadius = 20,
            Padding = 24,
            CardGap = 12,
            OuterMargin = 8,
            ShowAccent = false,
            AccentThickness = 5,
            ShowBorder = true,
            BorderThickness = 3,
        };

        var settings = new AppSettings();
        theme.ApplyTo(settings);

        Assert.Equal("#111111", settings.TextColor);
        Assert.Equal("#222222", settings.TitleColor);
        Assert.Equal("#333333", settings.AppNameColor);
        Assert.Equal("#444444", settings.BackgroundColor);
        Assert.Equal(0.5, settings.BackgroundOpacity);
        Assert.Equal("#555555", settings.AccentColor);
        Assert.Equal("#666666", settings.HighlightColor);
        Assert.Equal("#777777", settings.BorderColor);
        Assert.Equal(20, settings.CornerRadius);
        Assert.Equal(24, settings.Padding);
        Assert.Equal(12, settings.CardGap);
        Assert.Equal(8, settings.OuterMargin);
        Assert.False(settings.ShowAccent);
        Assert.Equal(5, settings.AccentThickness);
        Assert.True(settings.ShowBorder);
        Assert.Equal(3, settings.BorderThickness);
    }

    [Fact]
    public void ApplyTo_DoesNotChangeBehaviorSettings()
    {
        var settings = new AppSettings
        {
            FontSize = 22,
            FontFamily = "Consolas",
            NotificationDuration = 10,
            MaxVisibleNotifications = 5,
            ClickThrough = true,
            AlwaysOnTop = false,
        };

        ThemePreset.BuiltInThemes[0].ApplyTo(settings);

        Assert.Equal(22, settings.FontSize);
        Assert.Equal("Consolas", settings.FontFamily);
        Assert.Equal(10, settings.NotificationDuration);
        Assert.Equal(5, settings.MaxVisibleNotifications);
        Assert.True(settings.ClickThrough);
        Assert.False(settings.AlwaysOnTop);
    }

    [Fact]
    public void ApplyOverlayTo_DoesNotChangeSettingsWindowColors()
    {
        var settings = new AppSettings
        {
            SettingsThemeMode = "Windows Light",
            SettingsWindowBg = "#010101",
            SettingsWindowSurface = "#111111",
            SettingsWindowSurfaceLight = "#212121",
            SettingsWindowSurfaceHover = "#313131",
            SettingsWindowText = "#F1F1F1",
            SettingsWindowTextSecondary = "#D2D2D2",
            SettingsWindowTextMuted = "#A3A3A3",
            SettingsWindowAccent = "#020202",
            SettingsWindowBorder = "#565656",
        };

        var theme = ThemePreset.BuiltInThemes[0];
        theme.ApplyOverlayTo(settings);

        Assert.Equal("Windows Light", settings.SettingsThemeMode);
        Assert.Equal("#010101", settings.SettingsWindowBg);
        Assert.Equal("#111111", settings.SettingsWindowSurface);
        Assert.Equal("#212121", settings.SettingsWindowSurfaceLight);
        Assert.Equal("#313131", settings.SettingsWindowSurfaceHover);
        Assert.Equal("#F1F1F1", settings.SettingsWindowText);
        Assert.Equal("#D2D2D2", settings.SettingsWindowTextSecondary);
        Assert.Equal("#A3A3A3", settings.SettingsWindowTextMuted);
        Assert.Equal("#020202", settings.SettingsWindowAccent);
        Assert.Equal("#565656", settings.SettingsWindowBorder);
    }

    [Fact]
    public void HighContrastTheme_HasSettingsWindowPalette()
    {
        var theme = ThemePreset.BuiltInThemes.First(t => t.Name == "High Contrast");

        Assert.Equal("#000000", theme.SettingsWindowBg);
        Assert.Equal("#FFFFFF", theme.SettingsWindowText);
        Assert.Equal("#00FFFF", theme.SettingsWindowAccent);
    }

    [Fact]
    public void ApplySettingsWindowTo_CopiesOpacityFields()
    {
        var theme = new ThemePreset
        {
            SettingsWindowOpacity = 0.81,
            SettingsSurfaceOpacity = 0.42,
            SettingsElementOpacity = 0.66
        };

        var settings = new AppSettings();
        theme.ApplySettingsWindowTo(settings);

        Assert.Equal(0.81, settings.SettingsWindowOpacity);
        Assert.Equal(0.42, settings.SettingsSurfaceOpacity);
        Assert.Equal(0.66, settings.SettingsElementOpacity);
    }

    [Fact]
    public void ResolveThemeModeForLoadedSettings_FallsBackToCustom_WhenPaletteDiffersFromPreset()
    {
        var settings = new AppSettings
        {
            SettingsThemeMode = "Windows Dark",
            SettingsWindowBg = "#010101"
        };

        var resolved = SettingsThemeService.ResolveThemeModeForLoadedSettings(settings);

        Assert.Equal("Custom", resolved);
    }

    [Fact]
    public void ResolveThemeModeForLoadedSettings_KeepsPreset_WhenPaletteMatchesPreset()
    {
        var settings = new AppSettings();

        var resolved = SettingsThemeService.ResolveThemeModeForLoadedSettings(settings);

        Assert.Equal("Windows Dark", resolved);
    }

    [Fact]
    public void ResolveThemeModeForLoadedSettings_PreservesSystemMode()
    {
        var settings = new AppSettings
        {
            SettingsThemeMode = "System",
            SettingsWindowBg = "#010101"
        };

        var resolved = SettingsThemeService.ResolveThemeModeForLoadedSettings(settings);

        Assert.Equal("System", resolved);
    }

    [Fact]
    public void FromSettings_CapturesVisualProperties()
    {
        var settings = new AppSettings
        {
            TextColor = "#AABBCC",
            BackgroundColor = "#DDEEFF",
            CornerRadius = 99,
        };

        var theme = ThemePreset.FromSettings(settings, "MyTheme");

        Assert.Equal("MyTheme", theme.Name);
        Assert.Equal("#AABBCC", theme.TextColor);
        Assert.Equal("#DDEEFF", theme.BackgroundColor);
        Assert.Equal(99, theme.CornerRadius);
    }

    [Fact]
    public void FromSettings_RoundTrips_WithApplyTo()
    {
        var original = new AppSettings
        {
            TextColor = "#112233",
            TitleColor = "#445566",
            AppNameColor = "#778899",
            BackgroundColor = "#AABBCC",
            BackgroundOpacity = 0.77,
            AccentColor = "#DDEEFF",
            HighlightColor = "#FF0000",
            BorderColor = "#00FF00",
            CornerRadius = 15,
            Padding = 20,
            CardGap = 10,
            OuterMargin = 6,
            ShowAccent = false,
            AccentThickness = 4,
            ShowBorder = true,
            BorderThickness = 2,
        };

        var theme = ThemePreset.FromSettings(original, "RoundTrip");
        var target = new AppSettings();
        theme.ApplyTo(target);

        Assert.Equal(original.TextColor, target.TextColor);
        Assert.Equal(original.TitleColor, target.TitleColor);
        Assert.Equal(original.AppNameColor, target.AppNameColor);
        Assert.Equal(original.BackgroundColor, target.BackgroundColor);
        Assert.Equal(original.BackgroundOpacity, target.BackgroundOpacity);
        Assert.Equal(original.AccentColor, target.AccentColor);
        Assert.Equal(original.HighlightColor, target.HighlightColor);
        Assert.Equal(original.BorderColor, target.BorderColor);
        Assert.Equal(original.CornerRadius, target.CornerRadius);
        Assert.Equal(original.Padding, target.Padding);
        Assert.Equal(original.CardGap, target.CardGap);
        Assert.Equal(original.OuterMargin, target.OuterMargin);
        Assert.Equal(original.ShowAccent, target.ShowAccent);
        Assert.Equal(original.AccentThickness, target.AccentThickness);
        Assert.Equal(original.ShowBorder, target.ShowBorder);
        Assert.Equal(original.BorderThickness, target.BorderThickness);
    }

    // --- ThemeManager ---

    [Fact]
    public void LoadCustomThemes_ReturnsEmpty_WhenDirDoesNotExist()
    {
        var mgr = new ThemeManager(Path.Combine(_tempDir, "nonexistent"));
        var themes = mgr.LoadCustomThemes();
        Assert.Empty(themes);
    }

    [Fact]
    public void SaveAndLoad_RoundTrips()
    {
        var mgr = new ThemeManager(_tempDir);
        var theme = new ThemePreset { Name = "My Custom", TextColor = "#ABCDEF", CornerRadius = 42 };

        mgr.SaveCustomTheme(theme);
        var loaded = mgr.LoadCustomThemes();

        Assert.Single(loaded);
        Assert.Equal("My Custom", loaded[0].Name);
        Assert.Equal("#ABCDEF", loaded[0].TextColor);
        Assert.Equal(42, loaded[0].CornerRadius);
    }

    [Fact]
    public void SaveCustomTheme_IgnoresEmptyName()
    {
        var mgr = new ThemeManager(_tempDir);
        mgr.SaveCustomTheme(new ThemePreset { Name = "" });
        mgr.SaveCustomTheme(new ThemePreset { Name = "   " });

        Assert.Empty(mgr.LoadCustomThemes());
    }

    [Fact]
    public void DeleteCustomTheme_RemovesFile()
    {
        var mgr = new ThemeManager(_tempDir);
        mgr.SaveCustomTheme(new ThemePreset { Name = "ToDelete" });
        Assert.Single(mgr.LoadCustomThemes());

        mgr.DeleteCustomTheme("ToDelete");
        Assert.Empty(mgr.LoadCustomThemes());
    }

    [Fact]
    public void DeleteCustomTheme_NoOpIfMissing()
    {
        var mgr = new ThemeManager(_tempDir);
        mgr.DeleteCustomTheme("DoesNotExist"); // should not throw
    }

    [Fact]
    public void LoadCustomThemes_SkipsCorruptedFiles()
    {
        var mgr = new ThemeManager(_tempDir);
        mgr.SaveCustomTheme(new ThemePreset { Name = "Good" });
        File.WriteAllText(Path.Combine(_tempDir, "bad.json"), "NOT VALID JSON {{{");

        var themes = mgr.LoadCustomThemes();
        Assert.Single(themes);
        Assert.Equal("Good", themes[0].Name);
    }

    [Fact]
    public void LoadCustomThemes_SortsByName()
    {
        var mgr = new ThemeManager(_tempDir);
        mgr.SaveCustomTheme(new ThemePreset { Name = "Zebra" });
        mgr.SaveCustomTheme(new ThemePreset { Name = "Alpha" });
        mgr.SaveCustomTheme(new ThemePreset { Name = "Middle" });

        var themes = mgr.LoadCustomThemes();
        Assert.Equal(3, themes.Count);
        Assert.Equal("Alpha", themes[0].Name);
        Assert.Equal("Middle", themes[1].Name);
        Assert.Equal("Zebra", themes[2].Name);
    }

    // --- Import/Export ---

    [Fact]
    public void ExportAndImport_RoundTrips()
    {
        var original = new AppSettings
        {
            FontSize = 22,
            TextAlignment = "Center",
            TextColor = "#ABCDEF",
            CornerRadius = 99,
            MaxVisibleNotifications = 5,
            AppGroupingStyle = "Minimal Label",
            ShowAppGroupCounts = false,
            AnimationEasing = AnimationEasingHelper.Elastic,
            HighlightOverlayOpacity = 0.31,
            HighlightAnimation = HighlightAnimationHelper.Pulse,
            HighlightBorderMode = HighlightBorderModeHelper.NoBorder,
            HighlightBorderThickness = 2.5,
            ReadNotificationsAloudMode = SpokenNotificationTextFormatter.ModeTitleTimestamp,
            SpokenMutedApps = new() { "Teams", "Outlook" },
            NotificationCaptureMode = NotificationCaptureModeHelper.ModeAccessibility,
            CardBackgroundImagePath = @"C:\Users\demo\AppData\Roaming\NotificationsPro\backgrounds\social.png",
            CardBackgroundImageOpacity = 0.55,
            CardBackgroundImageHueDegrees = 12,
            CardBackgroundImageBrightness = 0.9,
            CardBackgroundImageFitMode = CardBackgroundImageFitModeHelper.FitInsideCard,
            CardBackgroundImagePlacement = CardBackgroundImagePlacementHelper.FullCard,
            FullscreenOverlayImagePath = @"C:\Users\demo\AppData\Roaming\NotificationsPro\backgrounds\wallpaper.png",
            FullscreenOverlayImageFitMode = CardBackgroundImageFitModeHelper.OriginalSize,
            CompactSettingsWindow = false,
            ShowQuickTips = false,
        };
        original.PerAppBackgroundImages["X"] = @"C:\Users\demo\AppData\Roaming\NotificationsPro\backgrounds\x.png";
        original.HighlightRules.Add(new HighlightRuleDefinition
        {
            Keyword = "headline",
            Scope = NotificationMatchScopeHelper.TitleOnly,
            AppFilter = "X",
            Animation = HighlightAnimationHelper.Shake,
            BorderMode = HighlightBorderModeHelper.AccentSideOnly,
            OverlayOpacity = 0.52,
            BorderThickness = 3.5
        });
        original.MuteRules.Add(new MuteRuleDefinition { Keyword = "spoiler", Scope = NotificationMatchScopeHelper.BodyOnly });
        original.NarrationRules.Add(new NarrationRuleDefinition { Keyword = "@openai", Scope = NotificationMatchScopeHelper.BodyOnly, ReadMode = SpokenNotificationTextFormatter.ModeTitleOnly });

        var filePath = Path.Combine(_tempDir, "export.json");
        ThemeManager.ExportSettings(original, filePath);
        var imported = ThemeManager.ImportSettings(filePath);

        Assert.NotNull(imported);
        Assert.Equal(22, imported!.FontSize);
        Assert.Equal("Center", imported.TextAlignment);
        Assert.Equal("#ABCDEF", imported.TextColor);
        Assert.Equal(99, imported.CornerRadius);
        Assert.Equal(5, imported.MaxVisibleNotifications);
        Assert.Equal("Minimal Label", imported.AppGroupingStyle);
        Assert.False(imported.ShowAppGroupCounts);
        Assert.Equal(AnimationEasingHelper.Elastic, imported.AnimationEasing);
        Assert.Equal(0.31, imported.HighlightOverlayOpacity);
        Assert.Equal(HighlightAnimationHelper.Pulse, imported.HighlightAnimation);
        Assert.Equal(HighlightBorderModeHelper.NoBorder, imported.HighlightBorderMode);
        Assert.Equal(2.5, imported.HighlightBorderThickness);
        Assert.Equal(SpokenNotificationTextFormatter.ModeTitleTimestamp, imported.ReadNotificationsAloudMode);
        Assert.Equal(new[] { "Teams", "Outlook" }, imported.SpokenMutedApps);
        Assert.Equal(NotificationCaptureModeHelper.ModeAccessibility, imported.NotificationCaptureMode);
        Assert.Equal(@"C:\Users\demo\AppData\Roaming\NotificationsPro\backgrounds\social.png", imported.CardBackgroundImagePath);
        Assert.Equal(0.55, imported.CardBackgroundImageOpacity);
        Assert.Equal(12, imported.CardBackgroundImageHueDegrees);
        Assert.Equal(0.9, imported.CardBackgroundImageBrightness);
        Assert.Equal(CardBackgroundImageFitModeHelper.FitInsideCard, imported.CardBackgroundImageFitMode);
        Assert.Equal(CardBackgroundImagePlacementHelper.FullCard, imported.CardBackgroundImagePlacement);
        Assert.Equal(@"C:\Users\demo\AppData\Roaming\NotificationsPro\backgrounds\wallpaper.png", imported.FullscreenOverlayImagePath);
        Assert.Equal(CardBackgroundImageFitModeHelper.OriginalSize, imported.FullscreenOverlayImageFitMode);
        Assert.Equal(@"C:\Users\demo\AppData\Roaming\NotificationsPro\backgrounds\x.png", imported.PerAppBackgroundImages["X"]);
        Assert.False(imported.CompactSettingsWindow);
        Assert.False(imported.ShowQuickTips);
        Assert.Single(imported.HighlightRules);
        Assert.Single(imported.MuteRules);
        Assert.Single(imported.NarrationRules);
        Assert.Equal(HighlightAnimationHelper.Shake, imported.HighlightRules[0].Animation);
        Assert.Equal(HighlightBorderModeHelper.AccentSideOnly, imported.HighlightRules[0].BorderMode);
        Assert.Equal(0.52, imported.HighlightRules[0].OverlayOpacity);
        Assert.Equal(3.5, imported.HighlightRules[0].BorderThickness);
    }

    [Fact]
    public void ImportSettings_ReturnsNull_WhenFileMissing()
    {
        var result = ThemeManager.ImportSettings(Path.Combine(_tempDir, "nope.json"));
        Assert.Null(result);
    }
}
