namespace NotificationsPro.Models;

/// <summary>
/// A named visual theme — colors, opacity, shape, and accent.
/// Does not include behavior, position, or filtering settings.
/// </summary>
public class ThemePreset
{
    public string Name { get; set; } = string.Empty;

    // Colors
    public string TextColor { get; set; } = "#E6E6E6";
    public string TitleColor { get; set; } = "#FFFFFF";
    public string AppNameColor { get; set; } = "#C8C8C8";
    public string BackgroundColor { get; set; } = "#202020";
    public double BackgroundOpacity { get; set; } = 0.94;
    public string AccentColor { get; set; } = "#0078D4";
    public string HighlightColor { get; set; } = "#FFD700";
    public string BorderColor { get; set; } = "#3A3A3A";

    // Settings window colors
    public string SettingsWindowBg { get; set; } = "#111111";
    public string SettingsWindowSurface { get; set; } = "#1C1C1C";
    public string SettingsWindowSurfaceLight { get; set; } = "#262626";
    public string SettingsWindowSurfaceHover { get; set; } = "#303030";
    public string SettingsWindowText { get; set; } = "#F3F3F3";
    public string SettingsWindowTextSecondary { get; set; } = "#C7C7C7";
    public string SettingsWindowTextMuted { get; set; } = "#8A8A8A";
    public string SettingsWindowAccent { get; set; } = "#0078D4";
    public string SettingsWindowBorder { get; set; } = "#353535";
    public double SettingsWindowOpacity { get; set; } = 0.95;

    // Shape
    public double CornerRadius { get; set; } = 12;
    public double Padding { get; set; } = 16;
    public double CardGap { get; set; } = 8;
    public double OuterMargin { get; set; } = 4;
    public bool ShowAccent { get; set; } = true;
    public double AccentThickness { get; set; } = 3;
    public bool ShowBorder { get; set; } = false;
    public double BorderThickness { get; set; } = 1;

    /// <summary>
    /// Apply only overlay visuals (cards/overlay colors + shape) onto settings.
    /// </summary>
    public void ApplyOverlayTo(AppSettings settings)
    {
        settings.TextColor = TextColor;
        settings.TitleColor = TitleColor;
        settings.AppNameColor = AppNameColor;
        settings.BackgroundColor = BackgroundColor;
        settings.BackgroundOpacity = BackgroundOpacity;
        settings.AccentColor = AccentColor;
        settings.HighlightColor = HighlightColor;
        settings.BorderColor = BorderColor;
        settings.CornerRadius = CornerRadius;
        settings.Padding = Padding;
        settings.CardGap = CardGap;
        settings.OuterMargin = OuterMargin;
        settings.ShowAccent = ShowAccent;
        settings.AccentThickness = AccentThickness;
        settings.ShowBorder = ShowBorder;
        settings.BorderThickness = BorderThickness;
    }

    /// <summary>
    /// Apply only settings window colors onto settings.
    /// </summary>
    public void ApplySettingsWindowTo(AppSettings settings)
    {
        settings.SettingsWindowBg = SettingsWindowBg;
        settings.SettingsWindowSurface = SettingsWindowSurface;
        settings.SettingsWindowSurfaceLight = SettingsWindowSurfaceLight;
        settings.SettingsWindowSurfaceHover = SettingsWindowSurfaceHover;
        settings.SettingsWindowText = SettingsWindowText;
        settings.SettingsWindowTextSecondary = SettingsWindowTextSecondary;
        settings.SettingsWindowTextMuted = SettingsWindowTextMuted;
        settings.SettingsWindowAccent = SettingsWindowAccent;
        settings.SettingsWindowBorder = SettingsWindowBorder;
        settings.SettingsWindowOpacity = SettingsWindowOpacity;
    }

    /// <summary>
    /// Apply both overlay and settings-window visuals.
    /// </summary>
    public void ApplyTo(AppSettings settings)
    {
        ApplyOverlayTo(settings);
        ApplySettingsWindowTo(settings);
    }

    /// <summary>
    /// Capture the current visual properties from an AppSettings instance.
    /// </summary>
    public static ThemePreset FromSettings(AppSettings settings, string name)
    {
        return new ThemePreset
        {
            Name = name,
            TextColor = settings.TextColor,
            TitleColor = settings.TitleColor,
            AppNameColor = settings.AppNameColor,
            BackgroundColor = settings.BackgroundColor,
            BackgroundOpacity = settings.BackgroundOpacity,
            AccentColor = settings.AccentColor,
            HighlightColor = settings.HighlightColor,
            BorderColor = settings.BorderColor,
            CornerRadius = settings.CornerRadius,
            Padding = settings.Padding,
            CardGap = settings.CardGap,
            OuterMargin = settings.OuterMargin,
            ShowAccent = settings.ShowAccent,
            AccentThickness = settings.AccentThickness,
            ShowBorder = settings.ShowBorder,
            BorderThickness = settings.BorderThickness,
            SettingsWindowBg = settings.SettingsWindowBg,
            SettingsWindowSurface = settings.SettingsWindowSurface,
            SettingsWindowSurfaceLight = settings.SettingsWindowSurfaceLight,
            SettingsWindowSurfaceHover = settings.SettingsWindowSurfaceHover,
            SettingsWindowText = settings.SettingsWindowText,
            SettingsWindowTextSecondary = settings.SettingsWindowTextSecondary,
            SettingsWindowTextMuted = settings.SettingsWindowTextMuted,
            SettingsWindowAccent = settings.SettingsWindowAccent,
            SettingsWindowBorder = settings.SettingsWindowBorder,
            SettingsWindowOpacity = settings.SettingsWindowOpacity,
        };
    }

    /// <summary>
    /// Built-in theme presets that ship with the app.
    /// </summary>
    public static readonly ThemePreset[] BuiltInThemes =
    {
        new()
        {
            Name = "Windows Dark",
            TextColor = "#E6E6E6", TitleColor = "#FFFFFF", AppNameColor = "#C8C8C8",
            BackgroundColor = "#202020", BackgroundOpacity = 0.94,
            AccentColor = "#0078D4", HighlightColor = "#FFD700", BorderColor = "#3A3A3A",
            SettingsWindowBg = "#111111", SettingsWindowSurface = "#1C1C1C",
            SettingsWindowSurfaceLight = "#262626", SettingsWindowSurfaceHover = "#303030",
            SettingsWindowText = "#F3F3F3", SettingsWindowTextSecondary = "#C7C7C7",
            SettingsWindowTextMuted = "#8A8A8A", SettingsWindowAccent = "#0078D4",
            SettingsWindowBorder = "#353535", SettingsWindowOpacity = 0.98,
            CornerRadius = 12, Padding = 16, CardGap = 8, OuterMargin = 4,
            ShowAccent = true, AccentThickness = 3, ShowBorder = false, BorderThickness = 1,
        },
        new()
        {
            Name = "Dark Purple",
            TextColor = "#E4E4EF", TitleColor = "#FFFFFF", AppNameColor = "#B8B8CC",
            BackgroundColor = "#1E1E2E", BackgroundOpacity = 0.92,
            AccentColor = "#7C5CFC", HighlightColor = "#FFD700", BorderColor = "#363650",
            SettingsWindowBg = "#151521", SettingsWindowSurface = "#1E1E2E",
            SettingsWindowSurfaceLight = "#282840", SettingsWindowSurfaceHover = "#343450",
            SettingsWindowText = "#E4E4EF", SettingsWindowTextSecondary = "#9898B0",
            SettingsWindowTextMuted = "#6B6B80", SettingsWindowAccent = "#7C5CFC",
            SettingsWindowBorder = "#363650", SettingsWindowOpacity = 0.98,
            CornerRadius = 12, Padding = 16, CardGap = 8, OuterMargin = 4,
            ShowAccent = true, AccentThickness = 3, ShowBorder = false, BorderThickness = 1,
        },
        new()
        {
            Name = "Light",
            TextColor = "#333333", TitleColor = "#111111", AppNameColor = "#666666",
            BackgroundColor = "#FFFFFF", BackgroundOpacity = 0.95,
            AccentColor = "#5B7FFF", HighlightColor = "#E67E00", BorderColor = "#D0D0D0",
            CornerRadius = 10, Padding = 16, CardGap = 8, OuterMargin = 4,
            ShowAccent = true, AccentThickness = 3, ShowBorder = true, BorderThickness = 1,
            SettingsWindowBg = "#F5F5F8", SettingsWindowSurface = "#FFFFFF",
            SettingsWindowSurfaceLight = "#E8E8F0", SettingsWindowSurfaceHover = "#DEDEE8",
            SettingsWindowText = "#222222", SettingsWindowTextSecondary = "#555555",
            SettingsWindowTextMuted = "#888888",
            SettingsWindowAccent = "#5B7FFF", SettingsWindowBorder = "#D0D0D0", SettingsWindowOpacity = 0.98,
        },
        new()
        {
            Name = "Frosted Glass",
            TextColor = "#E0E0F0", TitleColor = "#FFFFFF", AppNameColor = "#A0A0C0",
            BackgroundColor = "#2A2A40", BackgroundOpacity = 0.72,
            AccentColor = "#60A0FF", HighlightColor = "#FFD700", BorderColor = "#4A4A6A",
            SettingsWindowBg = "#121722", SettingsWindowSurface = "#1A2333",
            SettingsWindowSurfaceLight = "#243146", SettingsWindowSurfaceHover = "#2D3E59",
            SettingsWindowText = "#E7EEF8", SettingsWindowTextSecondary = "#B3C0D4",
            SettingsWindowTextMuted = "#7D8DA8", SettingsWindowAccent = "#60A0FF",
            SettingsWindowBorder = "#3A4B66", SettingsWindowOpacity = 0.85,
            CornerRadius = 16, Padding = 18, CardGap = 10, OuterMargin = 6,
            ShowAccent = false, AccentThickness = 3, ShowBorder = true, BorderThickness = 1,
        },
        new()
        {
            Name = "High Contrast",
            TextColor = "#FFFFFF", TitleColor = "#FFFF00", AppNameColor = "#00FF00",
            BackgroundColor = "#000000", BackgroundOpacity = 1.0,
            AccentColor = "#FFFF00", HighlightColor = "#FF4444", BorderColor = "#FFFFFF",
            SettingsWindowBg = "#000000", SettingsWindowSurface = "#000000",
            SettingsWindowSurfaceLight = "#101010", SettingsWindowSurfaceHover = "#1A1A1A",
            SettingsWindowText = "#FFFFFF", SettingsWindowTextSecondary = "#FFFF00",
            SettingsWindowTextMuted = "#BFBFBF", SettingsWindowAccent = "#00FFFF",
            SettingsWindowBorder = "#FFFFFF", SettingsWindowOpacity = 1.0,
            CornerRadius = 0, Padding = 14, CardGap = 6, OuterMargin = 2,
            ShowAccent = true, AccentThickness = 4, ShowBorder = true, BorderThickness = 2,
        },
        new()
        {
            Name = "Minimal",
            TextColor = "#C8C8D0", TitleColor = "#E0E0E8", AppNameColor = "#808090",
            BackgroundColor = "#181820", BackgroundOpacity = 0.88,
            AccentColor = "#181820", HighlightColor = "#FFD700", BorderColor = "#282838",
            SettingsWindowBg = "#101018", SettingsWindowSurface = "#171724",
            SettingsWindowSurfaceLight = "#222234", SettingsWindowSurfaceHover = "#2A2A3D",
            SettingsWindowText = "#E0E0E8", SettingsWindowTextSecondary = "#B0B0C0",
            SettingsWindowTextMuted = "#7A7A8C", SettingsWindowAccent = "#3F3F5E",
            SettingsWindowBorder = "#2B2B40", SettingsWindowOpacity = 0.98,
            CornerRadius = 6, Padding = 12, CardGap = 4, OuterMargin = 2,
            ShowAccent = false, AccentThickness = 2, ShowBorder = false, BorderThickness = 1,
        },
        new()
        {
            Name = "Color-Blind Safe",
            TextColor = "#FFFFFF", TitleColor = "#FFFFFF", AppNameColor = "#CCCCCC",
            BackgroundColor = "#1A1A2E", BackgroundOpacity = 0.95,
            AccentColor = "#0072B2", HighlightColor = "#E69F00", BorderColor = "#444466",
            SettingsWindowBg = "#101425", SettingsWindowSurface = "#1A2138",
            SettingsWindowSurfaceLight = "#252F4D", SettingsWindowSurfaceHover = "#2F3D63",
            SettingsWindowText = "#F5F5F5", SettingsWindowTextSecondary = "#D0D0D0",
            SettingsWindowTextMuted = "#9EA7BC", SettingsWindowAccent = "#0072B2",
            SettingsWindowBorder = "#425075", SettingsWindowOpacity = 0.98,
            CornerRadius = 10, Padding = 16, CardGap = 8, OuterMargin = 4,
            ShowAccent = true, AccentThickness = 4, ShowBorder = true, BorderThickness = 1,
        },
    };
}
