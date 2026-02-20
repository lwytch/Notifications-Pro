using System.Windows;
using System.Windows.Media;
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
    private static readonly string[] DarkDefaults =
    {
        "#111111", "#1C1C1C", "#262626", "#303030",
        "#F3F3F3", "#C7C7C7", "#8A8A8A",
        "#0078D4", "#353535"
    };

    private static readonly string[] LightDefaults =
    {
        "#F0F0F5", "#FFFFFF", "#E2E2EC", "#D4D4E0",
        "#1A1A2E", "#4A4A60", "#7A7A90",
        "#5B5FD6", "#B8B8CC"
    };

    /// <summary>
    /// Apply settings window theme colors to Application.Resources.
    /// DynamicResource references throughout the app will pick up these changes.
    /// </summary>
    public static void ApplySettingsTheme(AppSettings settings)
    {
        var resources = System.Windows.Application.Current.Resources;

        string[] colors;
        if (settings.SettingsThemeMode == "System")
        {
            colors = IsSystemLightTheme() ? LightDefaults : DarkDefaults;
        }
        else if (settings.SettingsThemeMode == "Light")
        {
            colors = LightDefaults;
        }
        else
        {
            // Use the custom settings window colors from AppSettings
            colors = new[]
            {
                settings.SettingsWindowBg, settings.SettingsWindowSurface,
                settings.SettingsWindowSurfaceLight, settings.SettingsWindowSurfaceHover,
                settings.SettingsWindowText, settings.SettingsWindowTextSecondary,
                settings.SettingsWindowTextMuted,
                settings.SettingsWindowAccent, settings.SettingsWindowBorder,
            };
        }

        SetBrush(resources, "WindowBgBrush", colors[0]);
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
