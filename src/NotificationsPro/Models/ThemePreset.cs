namespace NotificationsPro.Models;

/// <summary>
/// A named visual theme — colors, opacity, shape, and accent.
/// Does not include behavior, position, or filtering settings.
/// </summary>
public class ThemePreset
{
    public string Name { get; set; } = string.Empty;

    // Colors
    public string TextColor { get; set; } = "#E4E4EF";
    public string TitleColor { get; set; } = "#FFFFFF";
    public string AppNameColor { get; set; } = "#B8B8CC";
    public string BackgroundColor { get; set; } = "#1E1E2E";
    public double BackgroundOpacity { get; set; } = 0.92;
    public string AccentColor { get; set; } = "#7C5CFC";
    public string HighlightColor { get; set; } = "#FFD700";
    public string BorderColor { get; set; } = "#363650";

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
    /// Apply this theme's visual properties onto an AppSettings instance.
    /// </summary>
    public void ApplyTo(AppSettings settings)
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
        };
    }

    /// <summary>
    /// Built-in theme presets that ship with the app.
    /// </summary>
    public static readonly ThemePreset[] BuiltInThemes =
    {
        new()
        {
            Name = "Dark Purple",
            TextColor = "#E4E4EF", TitleColor = "#FFFFFF", AppNameColor = "#B8B8CC",
            BackgroundColor = "#1E1E2E", BackgroundOpacity = 0.92,
            AccentColor = "#7C5CFC", HighlightColor = "#FFD700", BorderColor = "#363650",
            CornerRadius = 12, Padding = 16, CardGap = 8, OuterMargin = 4,
            ShowAccent = true, AccentThickness = 3, ShowBorder = false, BorderThickness = 1,
        },
        new()
        {
            Name = "Dark Neutral",
            TextColor = "#D4D4D4", TitleColor = "#E8E8E8", AppNameColor = "#999999",
            BackgroundColor = "#1A1A1A", BackgroundOpacity = 0.94,
            AccentColor = "#4A9EFF", HighlightColor = "#FFA500", BorderColor = "#333333",
            CornerRadius = 8, Padding = 14, CardGap = 6, OuterMargin = 4,
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
        },
        new()
        {
            Name = "Frosted Glass",
            TextColor = "#E0E0F0", TitleColor = "#FFFFFF", AppNameColor = "#A0A0C0",
            BackgroundColor = "#2A2A40", BackgroundOpacity = 0.72,
            AccentColor = "#60A0FF", HighlightColor = "#FFD700", BorderColor = "#4A4A6A",
            CornerRadius = 16, Padding = 18, CardGap = 10, OuterMargin = 6,
            ShowAccent = false, AccentThickness = 3, ShowBorder = true, BorderThickness = 1,
        },
        new()
        {
            Name = "High Contrast",
            TextColor = "#FFFFFF", TitleColor = "#FFFF00", AppNameColor = "#00FF00",
            BackgroundColor = "#000000", BackgroundOpacity = 1.0,
            AccentColor = "#FFFF00", HighlightColor = "#FF4444", BorderColor = "#FFFFFF",
            CornerRadius = 0, Padding = 14, CardGap = 6, OuterMargin = 2,
            ShowAccent = true, AccentThickness = 4, ShowBorder = true, BorderThickness = 2,
        },
        new()
        {
            Name = "Minimal",
            TextColor = "#C8C8D0", TitleColor = "#E0E0E8", AppNameColor = "#808090",
            BackgroundColor = "#181820", BackgroundOpacity = 0.88,
            AccentColor = "#181820", HighlightColor = "#FFD700", BorderColor = "#282838",
            CornerRadius = 6, Padding = 12, CardGap = 4, OuterMargin = 2,
            ShowAccent = false, AccentThickness = 2, ShowBorder = false, BorderThickness = 1,
        },
    };
}
