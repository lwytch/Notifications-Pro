using System.IO;
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
        Assert.Equal(6, ThemePreset.BuiltInThemes.Length);
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
            TextColor = "#ABCDEF",
            CornerRadius = 99,
            MaxVisibleNotifications = 5,
        };

        var filePath = Path.Combine(_tempDir, "export.json");
        ThemeManager.ExportSettings(original, filePath);
        var imported = ThemeManager.ImportSettings(filePath);

        Assert.NotNull(imported);
        Assert.Equal(22, imported!.FontSize);
        Assert.Equal("#ABCDEF", imported.TextColor);
        Assert.Equal(99, imported.CornerRadius);
        Assert.Equal(5, imported.MaxVisibleNotifications);
    }

    [Fact]
    public void ImportSettings_ReturnsNull_WhenFileMissing()
    {
        var result = ThemeManager.ImportSettings(Path.Combine(_tempDir, "nope.json"));
        Assert.Null(result);
    }
}
