using System.Text.Json;
using NotificationsPro.Models;
using NotificationsPro.Services;

namespace NotificationsPro.Tests;

public class M95Tests
{
    // ---- AppSettings defaults ----

    [Fact]
    public void AppSettings_M95_Defaults()
    {
        var s = new AppSettings();
        Assert.False(s.ShowNotificationIcons);
        Assert.Equal(24, s.IconSize);
        Assert.Equal("None", s.DefaultIconPreset);
        Assert.NotNull(s.PerAppIcons);
        Assert.Empty(s.PerAppIcons);
        Assert.False(s.SoundEnabled);
        Assert.Equal("None", s.DefaultSound);
        Assert.NotNull(s.PerAppSounds);
        Assert.Empty(s.PerAppSounds);
        Assert.Equal(40, s.MaxVisibleNotifications);
        Assert.False(s.SuppressToastPopups);
        Assert.Equal("Popup", s.SettingsDisplayMode);
        Assert.False(s.PopupAutoClose);
        Assert.Equal("Windows Dark", s.SettingsThemeMode);
        Assert.Equal("#111111", s.SettingsWindowBg);
        Assert.False(s.LinkOverlayThemeAndUiTheme);
        Assert.Equal(11, s.TimestampFontSize);
        Assert.Equal("Relative", s.TimestampDisplayMode);
        Assert.Equal("Normal", s.TimestampFontWeight);
        Assert.Equal("#C8C8C8", s.TimestampColor);
        Assert.True(s.OverlayScrollbarVisible);
        Assert.Equal(8, s.OverlayScrollbarWidth);
        Assert.Equal(1.0, s.OverlayScrollbarOpacity);
        Assert.Equal("#141414", s.OverlayScrollbarTrackColor);
        Assert.Equal("#4F4F4F", s.OverlayScrollbarThumbColor);
        Assert.Equal("#0078D4", s.OverlayScrollbarThumbHoverColor);
        Assert.Equal(1.5, s.OverlayScrollbarPadding);
        Assert.Equal(6, s.OverlayScrollbarCornerRadius);
        Assert.False(s.FullscreenOverlayMode);
        Assert.Equal(0.5, s.FullscreenOverlayOpacity);
        Assert.False(s.AccessibilityModeEnabled);
        Assert.Equal("Framed Group", s.AppGroupingStyle);
        Assert.True(s.ShowAppGroupCounts);
    }

    // ---- Clone / deep-copy ----

    [Fact]
    public void AppSettings_Clone_DeepCopies_PerAppSounds()
    {
        var s = new AppSettings();
        s.PerAppSounds["Discord"] = "Asterisk";
        var clone = s.Clone();
        clone.PerAppSounds["Discord"] = "Beep";
        Assert.Equal("Asterisk", s.PerAppSounds["Discord"]);
    }

    [Fact]
    public void AppSettings_Clone_DeepCopies_PerAppIcons()
    {
        var s = new AppSettings();
        s.PerAppIcons["Slack"] = "Bell";
        var clone = s.Clone();
        clone.PerAppIcons["Slack"] = "Star";
        Assert.Equal("Bell", s.PerAppIcons["Slack"]);
    }

    [Fact]
    public void AppSettings_Clone_Copies_M95_Scalars()
    {
        var s = new AppSettings
        {
            SoundEnabled = true,
            DefaultSound = "Asterisk",
            SuppressToastPopups = true,
            ShowNotificationIcons = true,
            IconSize = 32,
            DefaultIconPreset = "Bell",
            SettingsDisplayMode = "Popup",
            PopupAutoClose = true,
            SettingsThemeMode = "Windows Light",
            LinkOverlayThemeAndUiTheme = true,
            TimestampFontSize = 16,
            TimestampDisplayMode = "DateTime",
            TimestampFontWeight = "SemiBold",
            TimestampColor = "#AABBCC",
            OverlayScrollbarVisible = false,
            OverlayScrollbarWidth = 12,
            OverlayScrollbarOpacity = 0.5,
            OverlayScrollbarTrackColor = "#111111",
            OverlayScrollbarThumbColor = "#555555",
            OverlayScrollbarThumbHoverColor = "#77AAFF",
            OverlayScrollbarPadding = 2.5,
            OverlayScrollbarCornerRadius = 8,
            FullscreenOverlayMode = true,
            FullscreenOverlayOpacity = 0.8,
            AccessibilityModeEnabled = true,
        };
        var clone = s.Clone();
        Assert.True(clone.SoundEnabled);
        Assert.Equal("Asterisk", clone.DefaultSound);
        Assert.True(clone.SuppressToastPopups);
        Assert.True(clone.ShowNotificationIcons);
        Assert.Equal(32, clone.IconSize);
        Assert.Equal("Bell", clone.DefaultIconPreset);
        Assert.Equal("Popup", clone.SettingsDisplayMode);
        Assert.True(clone.PopupAutoClose);
        Assert.Equal("Windows Light", clone.SettingsThemeMode);
        Assert.True(clone.LinkOverlayThemeAndUiTheme);
        Assert.Equal(16, clone.TimestampFontSize);
        Assert.Equal("DateTime", clone.TimestampDisplayMode);
        Assert.Equal("SemiBold", clone.TimestampFontWeight);
        Assert.Equal("#AABBCC", clone.TimestampColor);
        Assert.False(clone.OverlayScrollbarVisible);
        Assert.Equal(12, clone.OverlayScrollbarWidth);
        Assert.Equal(0.5, clone.OverlayScrollbarOpacity);
        Assert.Equal("#111111", clone.OverlayScrollbarTrackColor);
        Assert.Equal("#555555", clone.OverlayScrollbarThumbColor);
        Assert.Equal("#77AAFF", clone.OverlayScrollbarThumbHoverColor);
        Assert.Equal(2.5, clone.OverlayScrollbarPadding);
        Assert.Equal(8, clone.OverlayScrollbarCornerRadius);
        Assert.True(clone.FullscreenOverlayMode);
        Assert.Equal(0.8, clone.FullscreenOverlayOpacity);
        Assert.True(clone.AccessibilityModeEnabled);
    }

    // ---- JSON round-trip ----

    [Fact]
    public void AppSettings_M95_JsonRoundTrip()
    {
        var s = new AppSettings
        {
            SoundEnabled = true,
            DefaultSound = "Exclamation",
            SuppressToastPopups = true,
            ShowNotificationIcons = true,
            IconSize = 36,
            DefaultIconPreset = "Star",
            SettingsDisplayMode = "Popup",
            SettingsThemeMode = "System",
            TimestampFontSize = 18,
            TimestampDisplayMode = "Time",
            TimestampFontWeight = "Medium",
            TimestampColor = "#334455",
            OverlayScrollbarWidth = 16,
            OverlayScrollbarTrackColor = "#121212",
            OverlayScrollbarThumbColor = "#666666",
            OverlayScrollbarThumbHoverColor = "#88BBFF",
            OverlayScrollbarPadding = 2,
            OverlayScrollbarCornerRadius = 7,
            FullscreenOverlayMode = true,
        };
        s.PerAppSounds["Teams"] = "Hand";
        s.PerAppIcons["Teams"] = "Chat";

        var json = JsonSerializer.Serialize(s);
        var deserialized = JsonSerializer.Deserialize<AppSettings>(json)!;

        Assert.True(deserialized.SoundEnabled);
        Assert.Equal("Exclamation", deserialized.DefaultSound);
        Assert.Equal("Hand", deserialized.PerAppSounds["Teams"]);
        Assert.True(deserialized.ShowNotificationIcons);
        Assert.Equal(36, deserialized.IconSize);
        Assert.Equal("Star", deserialized.DefaultIconPreset);
        Assert.Equal("Chat", deserialized.PerAppIcons["Teams"]);
        Assert.Equal("Popup", deserialized.SettingsDisplayMode);
        Assert.Equal("System", deserialized.SettingsThemeMode);
        Assert.Equal(18, deserialized.TimestampFontSize);
        Assert.Equal("Time", deserialized.TimestampDisplayMode);
        Assert.Equal("Medium", deserialized.TimestampFontWeight);
        Assert.Equal("#334455", deserialized.TimestampColor);
        Assert.Equal(16, deserialized.OverlayScrollbarWidth);
        Assert.Equal("#121212", deserialized.OverlayScrollbarTrackColor);
        Assert.Equal("#666666", deserialized.OverlayScrollbarThumbColor);
        Assert.Equal("#88BBFF", deserialized.OverlayScrollbarThumbHoverColor);
        Assert.Equal(2, deserialized.OverlayScrollbarPadding);
        Assert.Equal(7, deserialized.OverlayScrollbarCornerRadius);
        Assert.True(deserialized.FullscreenOverlayMode);
    }

    // ---- IconPreset ----

    [Fact]
    public void IconPreset_HasBuiltInIcons()
    {
        var names = IconPreset.PresetNames;
        Assert.True(names.Length >= 10);
        Assert.Contains("None", names);
        Assert.Contains("Bell", names);
        Assert.Contains("Star", names);
        Assert.Contains("Heart", names);
        Assert.Contains("Chat", names);
        Assert.Contains("Megaphone", names);
    }

    [Fact]
    public void IconPreset_NoneHasEmptyPath()
    {
        Assert.Equal("", IconPreset.BuiltInIcons["None"]);
    }

    [Fact]
    public void IconPreset_NonNoneHaveGeometryPaths()
    {
        foreach (var kvp in IconPreset.BuiltInIcons)
        {
            if (kvp.Key == "None") continue;
            Assert.False(string.IsNullOrWhiteSpace(kvp.Value), $"{kvp.Key} has empty geometry path");
        }
    }

    // ---- SoundService ----

    [Fact]
    public void SoundService_GetWindowsSounds_ReturnsValidEntries()
    {
        var sounds = SoundService.GetWindowsSounds();
        // All entries must have non-empty names and WAV paths
        foreach (var s in sounds)
        {
            Assert.False(string.IsNullOrWhiteSpace(s.Name));
            Assert.False(string.IsNullOrWhiteSpace(s.WavPath));
        }
        // Paths (if any) should exist on disk (registry enumeration may return 0 sounds in CI)
        foreach (var s in sounds)
            Assert.True(System.IO.File.Exists(s.WavPath), $"WAV not found: {s.WavPath}");
    }

    [Fact]
    public void SoundService_DisabledDoesNotThrow()
    {
        var s = new AppSettings { SoundEnabled = false, DefaultSound = "Asterisk" };
        SoundService.PlaySound("TestApp", s);
    }

    [Fact]
    public void SoundService_NoneDoesNotThrow()
    {
        var s = new AppSettings { SoundEnabled = true, DefaultSound = "None" };
        SoundService.PlaySound("TestApp", s);
    }

    // ---- QueueManager NotificationAdded event ----

    [Fact]
    public void QueueManager_NotificationAdded_FiresOnAdd()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            var sm = new SettingsManager(tempDir);
            sm.Load();
            var qm = new QueueManager(sm);

            string? firedAppName = null;
            qm.NotificationAdded += appName => firedAppName = appName;

            qm.AddNotification("TestApp", "Hello", "World");
            Assert.Equal("TestApp", firedAppName);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void QueueManager_NotificationAdded_DoesNotFireWhenPaused()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            var sm = new SettingsManager(tempDir);
            sm.Load();
            sm.Settings.NotificationsPaused = true;
            var qm = new QueueManager(sm);

            bool fired = false;
            qm.NotificationAdded += _ => fired = true;

            qm.AddNotification("TestApp", "Hello", "World");
            Assert.False(fired);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    // ---- ThemePreset settings window colors ----

    [Fact]
    public void ThemePreset_LightHasSettingsColors()
    {
        var light = ThemePreset.BuiltInThemes.First(t => t.Name == "Light");
        Assert.Equal("#F5F5F8", light.SettingsWindowBg);
        Assert.Equal("#FFFFFF", light.SettingsWindowSurface);
        Assert.Equal("#222222", light.SettingsWindowText);
    }

    [Fact]
    public void ThemePreset_ApplyTo_IncludesSettingsColors()
    {
        var preset = new ThemePreset
        {
            SettingsWindowBg = "#AABBCC",
            SettingsWindowAccent = "#112233",
            OverlayScrollbarThumbColor = "#778899",
        };
        var settings = new AppSettings();
        preset.ApplyTo(settings);
        Assert.Equal("#AABBCC", settings.SettingsWindowBg);
        Assert.Equal("#112233", settings.SettingsWindowAccent);
        Assert.Equal("#778899", settings.OverlayScrollbarThumbColor);
    }

    [Fact]
    public void ThemePreset_FromSettings_CapturesSettingsColors()
    {
        var settings = new AppSettings
        {
            SettingsWindowBg = "#001122",
            SettingsWindowBorder = "#334455",
            OverlayScrollbarTrackColor = "#202020",
        };
        var preset = ThemePreset.FromSettings(settings, "Test");
        Assert.Equal("#001122", preset.SettingsWindowBg);
        Assert.Equal("#334455", preset.SettingsWindowBorder);
        Assert.Equal("#202020", preset.OverlayScrollbarTrackColor);
    }
}
