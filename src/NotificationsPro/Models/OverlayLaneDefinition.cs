namespace NotificationsPro.Models;

public class OverlayLaneDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = "Overlay Lane";
    public bool IsEnabled { get; set; } = true;

    // Typography
    public string FontFamily { get; set; } = string.Empty;
    public double FontSize { get; set; }
    public string FontWeight { get; set; } = string.Empty;
    public double AppNameFontSize { get; set; }
    public string AppNameFontWeight { get; set; } = string.Empty;
    public double TitleFontSize { get; set; }
    public string TitleFontWeight { get; set; } = string.Empty;
    public double LineSpacing { get; set; }
    public string TextAlignment { get; set; } = string.Empty;

    // Card visuals
    public int MonitorIndex { get; set; }
    public string PositionPreset { get; set; } = "Top Left";
    public double? Left { get; set; }
    public double? Top { get; set; }
    public double Width { get; set; } = 340;
    public double MaxHeight { get; set; } = 480;
    public string AccentColor { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = string.Empty;
    public double BackgroundOpacity { get; set; }
    public string TitleColor { get; set; } = string.Empty;
    public string TextColor { get; set; } = string.Empty;
    public string AppNameColor { get; set; } = string.Empty;
    public double CornerRadius { get; set; }
    public double Padding { get; set; }
    public double CardGap { get; set; }
    public double OuterMargin { get; set; }
    public bool ShowAccent { get; set; }
    public double AccentThickness { get; set; }
    public bool ShowBorder { get; set; }
    public string BorderColor { get; set; } = string.Empty;
    public double BorderThickness { get; set; } = 1;

    // Content / display
    public bool ShowAppName { get; set; } = true;
    public bool ShowNotificationTitle { get; set; } = true;
    public bool ShowNotificationBody { get; set; } = true;
    public bool LimitTextLines { get; set; }
    public int MaxAppNameLines { get; set; }
    public int MaxTitleLines { get; set; }
    public int MaxBodyLines { get; set; }
    public bool SingleLineMode { get; set; }
    public bool SingleLineWrapText { get; set; }
    public int SingleLineMaxLines { get; set; }
    public bool SingleLineAutoFullWidth { get; set; }
    public bool ShowTimestamp { get; set; } = true;
    public double TimestampFontSize { get; set; }
    public string TimestampDisplayMode { get; set; } = string.Empty;
    public string TimestampFontWeight { get; set; } = string.Empty;
    public string TimestampColor { get; set; } = string.Empty;
    public string DensityPreset { get; set; } = string.Empty;

    // Background image
    public string BackgroundImagePath { get; set; } = string.Empty;
    public double BackgroundImageOpacity { get; set; } = 0.45;
    public double BackgroundImageHueDegrees { get; set; }
    public double BackgroundImageBrightness { get; set; } = 1.0;

    public OverlayLaneDefinition Clone()
    {
        return new OverlayLaneDefinition
        {
            Id = Id,
            Name = Name,
            IsEnabled = IsEnabled,
            FontFamily = FontFamily,
            FontSize = FontSize,
            FontWeight = FontWeight,
            AppNameFontSize = AppNameFontSize,
            AppNameFontWeight = AppNameFontWeight,
            TitleFontSize = TitleFontSize,
            TitleFontWeight = TitleFontWeight,
            LineSpacing = LineSpacing,
            TextAlignment = TextAlignment,
            MonitorIndex = MonitorIndex,
            PositionPreset = PositionPreset,
            Left = Left,
            Top = Top,
            Width = Width,
            MaxHeight = MaxHeight,
            AccentColor = AccentColor,
            BackgroundColor = BackgroundColor,
            BackgroundOpacity = BackgroundOpacity,
            TitleColor = TitleColor,
            TextColor = TextColor,
            AppNameColor = AppNameColor,
            CornerRadius = CornerRadius,
            Padding = Padding,
            CardGap = CardGap,
            OuterMargin = OuterMargin,
            ShowAccent = ShowAccent,
            AccentThickness = AccentThickness,
            ShowBorder = ShowBorder,
            BorderColor = BorderColor,
            BorderThickness = BorderThickness,
            ShowAppName = ShowAppName,
            ShowNotificationTitle = ShowNotificationTitle,
            ShowNotificationBody = ShowNotificationBody,
            LimitTextLines = LimitTextLines,
            MaxAppNameLines = MaxAppNameLines,
            MaxTitleLines = MaxTitleLines,
            MaxBodyLines = MaxBodyLines,
            SingleLineMode = SingleLineMode,
            SingleLineWrapText = SingleLineWrapText,
            SingleLineMaxLines = SingleLineMaxLines,
            SingleLineAutoFullWidth = SingleLineAutoFullWidth,
            ShowTimestamp = ShowTimestamp,
            TimestampFontSize = TimestampFontSize,
            TimestampDisplayMode = TimestampDisplayMode,
            TimestampFontWeight = TimestampFontWeight,
            TimestampColor = TimestampColor,
            DensityPreset = DensityPreset,
            BackgroundImagePath = BackgroundImagePath,
            BackgroundImageOpacity = BackgroundImageOpacity,
            BackgroundImageHueDegrees = BackgroundImageHueDegrees,
            BackgroundImageBrightness = BackgroundImageBrightness
        };
    }
}
