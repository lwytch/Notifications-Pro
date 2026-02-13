using NotificationsPro.Helpers;
using NotificationsPro.Models;
using NotificationsPro.Services;

namespace NotificationsPro.Tests;

public class AccessibilityTests
{
    // --- ContrastHelper ---

    [Fact]
    public void ContrastRatio_WhiteOnBlack_IsMaximum()
    {
        var ratio = ContrastHelper.GetContrastRatio("#FFFFFF", "#000000");
        Assert.Equal(21.0, ratio, 0.1);
    }

    [Fact]
    public void ContrastRatio_SameColor_IsOne()
    {
        var ratio = ContrastHelper.GetContrastRatio("#888888", "#888888");
        Assert.Equal(1.0, ratio, 0.05);
    }

    [Fact]
    public void ContrastRatio_Symmetric()
    {
        var ratio1 = ContrastHelper.GetContrastRatio("#E4E4EF", "#1E1E2E");
        var ratio2 = ContrastHelper.GetContrastRatio("#1E1E2E", "#E4E4EF");
        Assert.Equal(ratio1, ratio2, 0.01);
    }

    [Fact]
    public void WcagLevel_AAA_WhenRatioAbove7()
    {
        Assert.Equal("AAA", ContrastHelper.GetWcagLevel(7.5));
    }

    [Fact]
    public void WcagLevel_AA_WhenRatioBetween4_5And7()
    {
        Assert.Equal("AA", ContrastHelper.GetWcagLevel(5.0));
    }

    [Fact]
    public void WcagLevel_AALarge_WhenRatioBetween3And4_5()
    {
        Assert.Equal("AA Large", ContrastHelper.GetWcagLevel(3.5));
    }

    [Fact]
    public void WcagLevel_Fail_WhenRatioBelow3()
    {
        Assert.Equal("Fail", ContrastHelper.GetWcagLevel(2.0));
    }

    [Fact]
    public void FormatRatio_ReturnsReadableString()
    {
        var result = ContrastHelper.FormatRatio("#FFFFFF", "#000000");
        Assert.Contains("21.0:1", result);
        Assert.Contains("AAA", result);
    }

    [Fact]
    public void ParseHexToRgb_ValidHex()
    {
        var (r, g, b) = ContrastHelper.ParseHexToRgb("#FF8040");
        Assert.Equal(255, r);
        Assert.Equal(128, g);
        Assert.Equal(64, b);
    }

    [Fact]
    public void ParseHexToRgb_InvalidHex_ReturnsWhite()
    {
        var (r, g, b) = ContrastHelper.ParseHexToRgb("bad");
        Assert.Equal(255, r);
        Assert.Equal(255, g);
        Assert.Equal(255, b);
    }

    // --- ContrastHelper: Default theme colors pass WCAG ---

    [Fact]
    public void DefaultTheme_TextOnBackground_PassesAA()
    {
        var ratio = ContrastHelper.GetContrastRatio("#E4E4EF", "#1E1E2E");
        Assert.True(ratio >= 4.5, $"Default text contrast {ratio:F1} fails WCAG AA");
    }

    [Fact]
    public void DefaultTheme_TitleOnBackground_PassesAA()
    {
        var ratio = ContrastHelper.GetContrastRatio("#FFFFFF", "#1E1E2E");
        Assert.True(ratio >= 4.5, $"Default title contrast {ratio:F1} fails WCAG AA");
    }

    // --- QueueManager: EstimateLineCount ---

    [Fact]
    public void EstimateLineCount_EmptyBody_ReturnsOne()
    {
        Assert.Equal(1, QueueManager.EstimateLineCount(""));
        Assert.Equal(1, QueueManager.EstimateLineCount(null!));
    }

    [Fact]
    public void EstimateLineCount_ShortText_ReturnsOne()
    {
        Assert.Equal(1, QueueManager.EstimateLineCount("Hello"));
    }

    [Fact]
    public void EstimateLineCount_MultiLineText()
    {
        var body = "Line one\nLine two\nLine three";
        var result = QueueManager.EstimateLineCount(body);
        Assert.True(result >= 3, $"Expected >= 3 lines, got {result}");
    }

    [Fact]
    public void EstimateLineCount_LongWrappedText()
    {
        var body = new string('x', 200); // ~4 lines at 50 chars per line
        var result = QueueManager.EstimateLineCount(body);
        Assert.True(result >= 4, $"Expected >= 4 lines for 200 chars, got {result}");
    }

    // --- QueueManager: Persistent mode ---

    [Fact]
    public void PersistentMode_NotificationDoesNotExpire()
    {
        var sm = new SettingsManager();
        sm.Settings.PersistentNotifications = true;
        sm.Settings.NotificationDuration = 0.1;
        sm.Settings.AnimationDurationMs = 0;
        var queue = new QueueManager(sm);

        queue.AddNotification("Test", "Persistent notification");
        Assert.Single(queue.VisibleNotifications);

        // Even with a very short duration, the notification shouldn't auto-remove
        // since persistent mode skips the timer entirely
        Thread.Sleep(200);
        Assert.Single(queue.VisibleNotifications);
    }

    // --- HotkeyManager: TryParseHotkey ---

    [Fact]
    public void TryParseHotkey_CtrlAltN_Succeeds()
    {
        Assert.True(HotkeyManager.TryParseHotkey("Ctrl+Alt+N", out var mod, out var vk));
        Assert.True(mod > 0);
        Assert.Equal((uint)'N', vk);
    }

    [Fact]
    public void TryParseHotkey_ShiftF1_Succeeds()
    {
        Assert.True(HotkeyManager.TryParseHotkey("Shift+F1", out _, out var vk));
        Assert.Equal(0x70u, vk); // VK_F1
    }

    [Fact]
    public void TryParseHotkey_InvalidCombo_Fails()
    {
        Assert.False(HotkeyManager.TryParseHotkey("", out _, out _));
        Assert.False(HotkeyManager.TryParseHotkey("N", out _, out _)); // no modifier
        Assert.False(HotkeyManager.TryParseHotkey("Ctrl+", out _, out _));
    }

    [Fact]
    public void TryParseHotkey_SingleDigit_Succeeds()
    {
        Assert.True(HotkeyManager.TryParseHotkey("Ctrl+5", out _, out var vk));
        Assert.Equal((uint)'5', vk);
    }

    // --- AppSettings: M7 default values ---

    [Fact]
    public void DefaultSettings_AccessibilityDefaults()
    {
        var s = new AppSettings();
        Assert.False(s.PersistentNotifications);
        Assert.False(s.AutoDurationEnabled);
        Assert.Equal(2.0, s.AutoDurationSecondsPerLine);
        Assert.Equal(5.0, s.AutoDurationBaseSeconds);
        Assert.True(s.RespectReduceMotion);
        Assert.True(s.RespectHighContrast);
        Assert.False(s.RespectTextScaling);
        Assert.False(s.GlobalHotkeysEnabled);
        Assert.Equal("Ctrl+Alt+N", s.HotkeyToggleOverlay);
        Assert.Equal("Ctrl+Alt+D", s.HotkeyDismissAll);
        Assert.Equal("Ctrl+Alt+P", s.HotkeyToggleDnd);
        Assert.Equal("Comfortable", s.DensityPreset);
    }

    // --- ThemePreset: Color-Blind Safe theme ---

    [Fact]
    public void ColorBlindSafeTheme_Exists()
    {
        var theme = ThemePreset.BuiltInThemes.FirstOrDefault(t => t.Name == "Color-Blind Safe");
        Assert.NotNull(theme);
        Assert.True(theme.ShowAccent);
        Assert.True(theme.ShowBorder);
    }

    [Fact]
    public void ColorBlindSafeTheme_PassesWCAG_AA()
    {
        var theme = ThemePreset.BuiltInThemes.First(t => t.Name == "Color-Blind Safe");
        var textRatio = ContrastHelper.GetContrastRatio(theme.TextColor, theme.BackgroundColor);
        var titleRatio = ContrastHelper.GetContrastRatio(theme.TitleColor, theme.BackgroundColor);
        Assert.True(textRatio >= 4.5, $"CB-safe text contrast {textRatio:F1} fails WCAG AA");
        Assert.True(titleRatio >= 4.5, $"CB-safe title contrast {titleRatio:F1} fails WCAG AA");
    }
}
