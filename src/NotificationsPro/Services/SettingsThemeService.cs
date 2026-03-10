using System.Windows;
using System.Windows.Media;
using NotificationsPro.Helpers;
using NotificationsPro.Models;
using MediaColor = System.Windows.Media.Color;
using MediaColorConverter = System.Windows.Media.ColorConverter;

namespace NotificationsPro.Services;

/// <summary>
/// Updates Application.Current.Resources at runtime so that DynamicResource references
/// in the settings window (and other UI) pick up the current theme colors.
/// </summary>
public static class SettingsThemeService
{
    private const string ModeWindowsDark = "Windows Dark";
    private const string ModeWindowsLight = "Windows Light";
    private const string ModeHighContrast = "High Contrast";
    private const string ModeSystem = "System";
    private const string ModeCustom = "Custom";

    private static readonly string[] WindowsDarkDefaults =
    {
        "#111111", "#1C1C1C", "#262626", "#303030",
        "#F3F3F3", "#C7C7C7", "#8A8A8A",
        "#0078D4", "#353535"
    };

    private static readonly string[] WindowsLightDefaults =
    {
        "#F0F0F5", "#FFFFFF", "#E2E2EC", "#D4D4E0",
        "#1A1A2E", "#4A4A60", "#7A7A90",
        "#5B5FD6", "#B8B8CC"
    };

    private static readonly string[] HighContrastDefaults =
    {
        "#000000", "#000000", "#101010", "#1A1A1A",
        "#FFFFFF", "#FFFF00", "#BFBFBF",
        "#00FFFF", "#FFFFFF"
    };

    /// <summary>
    /// Apply settings window theme colors to Application.Resources.
    /// DynamicResource references throughout the app will pick up these changes.
    /// </summary>
    public static void ApplySettingsTheme(AppSettings settings)
    {
        var resources = System.Windows.Application.Current.Resources;
        var mode = NormalizeThemeMode(settings.SettingsThemeMode);

        string[] colors;
        if (mode == ModeSystem)
        {
            colors = IsSystemLightTheme() ? WindowsLightDefaults : WindowsDarkDefaults;
        }
        else if (mode == ModeWindowsLight)
        {
            colors = WindowsLightDefaults;
        }
        else if (mode == ModeWindowsDark)
        {
            colors = WindowsDarkDefaults;
        }
        else if (mode == ModeHighContrast)
        {
            colors = HighContrastDefaults;
        }
        else
        {
            // Custom UI palette from AppSettings.
            colors = new[]
            {
                settings.SettingsWindowBg, settings.SettingsWindowSurface,
                settings.SettingsWindowSurfaceLight, settings.SettingsWindowSurfaceHover,
                settings.SettingsWindowText, settings.SettingsWindowTextSecondary,
                settings.SettingsWindowTextMuted,
                settings.SettingsWindowAccent, settings.SettingsWindowBorder,
            };
        }

        var bgAlpha = (byte)Math.Clamp(settings.SettingsWindowOpacity * 255.0, 0, 255);
        SetBrush(resources, "WindowBgBrush", WithAlpha(colors[0], bgAlpha));
        SetBrush(resources, "SurfaceBgBrush", colors[1]);
        SetBrush(resources, "SurfaceLightBrush", colors[2]);
        SetBrush(resources, "SurfaceHoverBrush", colors[3]);
        SetBrush(resources, "TextPrimaryBrush", colors[4]);
        SetBrush(resources, "TextSecondaryBrush", colors[5]);
        SetBrush(resources, "TextMutedBrush", colors[6]);
        SetBrush(resources, "AccentBrush", colors[7]);
        SetBrush(resources, "BorderBrush", colors[8]);

        // Derived brushes
        SetBrush(resources, "AccentHoverBrush", LightenColor(colors[7], 0.15));
        SetBrush(resources, "AccentMutedBrush", WithAlpha(colors[7], 0x33));
        SetBrush(resources, "BorderLightBrush", LightenColor(colors[8], 0.1));
        SetBrush(resources, "PrimaryButtonForegroundBrush", GetReadableTextColor(colors[7]));
    }

    public static string NormalizeThemeMode(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
            return ModeWindowsDark;

        var trimmed = mode.Trim();

        // Back-compat with earlier values.
        if (trimmed.Equals("Dark", StringComparison.OrdinalIgnoreCase))
            return ModeWindowsDark;
        if (trimmed.Equals("Light", StringComparison.OrdinalIgnoreCase))
            return "Light";

        if (trimmed.Equals(ModeWindowsDark, StringComparison.OrdinalIgnoreCase))
            return ModeWindowsDark;
        if (trimmed.Equals(ModeWindowsLight, StringComparison.OrdinalIgnoreCase))
            return ModeWindowsLight;
        if (trimmed.Equals(ModeHighContrast, StringComparison.OrdinalIgnoreCase))
            return ModeHighContrast;
        if (trimmed.Equals(ModeSystem, StringComparison.OrdinalIgnoreCase))
            return ModeSystem;
        if (trimmed.Equals(ModeCustom, StringComparison.OrdinalIgnoreCase))
            return ModeCustom;

        // Unknown values are treated as named theme selections where
        // SettingsWindow* color channels are already populated.
        return trimmed;
    }

    public static bool TryGetPresetColors(string? mode, out string[] colors)
    {
        var normalized = NormalizeThemeMode(mode);
        if (normalized == ModeWindowsDark)
        {
            colors = (string[])WindowsDarkDefaults.Clone();
            return true;
        }

        if (normalized == ModeWindowsLight)
        {
            colors = (string[])WindowsLightDefaults.Clone();
            return true;
        }

        if (normalized == ModeHighContrast)
        {
            colors = (string[])HighContrastDefaults.Clone();
            return true;
        }

        if (normalized == ModeSystem)
        {
            colors = IsSystemLightTheme()
                ? (string[])WindowsLightDefaults.Clone()
                : (string[])WindowsDarkDefaults.Clone();
            return true;
        }

        colors = Array.Empty<string>();
        return false;
    }

    private static void SetBrush(ResourceDictionary resources, string key, string hex)
    {
        try
        {
            var color = (MediaColor)MediaColorConverter.ConvertFromString(hex);
            resources[key] = new SolidColorBrush(color);
        }
        catch { /* Keep existing brush on parse failure */ }
    }

    private static string LightenColor(string hex, double factor)
    {
        try
        {
            var c = (MediaColor)MediaColorConverter.ConvertFromString(hex);
            var r = (byte)Math.Min(255, c.R + (255 - c.R) * factor);
            var g = (byte)Math.Min(255, c.G + (255 - c.G) * factor);
            var b = (byte)Math.Min(255, c.B + (255 - c.B) * factor);
            return $"#{c.A:X2}{r:X2}{g:X2}{b:X2}";
        }
        catch { return hex; }
    }

    private static string WithAlpha(string hex, byte alpha)
    {
        try
        {
            var c = (MediaColor)MediaColorConverter.ConvertFromString(hex);
            return $"#{alpha:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
        }
        catch { return hex; }
    }

    private static string GetReadableTextColor(string backgroundHex)
    {
        try
        {
            var bg = ParseToRgbHex(backgroundHex);
            var whiteRatio = ContrastHelper.GetContrastRatio("#FFFFFF", bg);
            var blackRatio = ContrastHelper.GetContrastRatio("#000000", bg);
            return blackRatio >= whiteRatio ? "#000000" : "#FFFFFF";
        }
        catch
        {
            return "#FFFFFF";
        }
    }

    private static string ParseToRgbHex(string hex)
    {
        var c = (MediaColor)MediaColorConverter.ConvertFromString(hex);
        return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
    }

    private static bool IsSystemLightTheme()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int value)
                return value == 1;
        }
        catch { /* Registry unavailable */ }
        return false; // Default to dark
    }
}

