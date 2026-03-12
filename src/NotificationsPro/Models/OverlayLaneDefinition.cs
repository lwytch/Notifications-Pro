namespace NotificationsPro.Models;

public class OverlayLaneDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = "Overlay Lane";
    public bool IsEnabled { get; set; } = true;
    public int MonitorIndex { get; set; }
    public string PositionPreset { get; set; } = "Top Left";
    public double? Left { get; set; }
    public double? Top { get; set; }
    public double Width { get; set; } = 340;
    public double MaxHeight { get; set; } = 480;
    public string AccentColor { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = string.Empty;
    public string TitleColor { get; set; } = string.Empty;
    public string TextColor { get; set; } = string.Empty;
    public string AppNameColor { get; set; } = string.Empty;
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
            MonitorIndex = MonitorIndex,
            PositionPreset = PositionPreset,
            Left = Left,
            Top = Top,
            Width = Width,
            MaxHeight = MaxHeight,
            AccentColor = AccentColor,
            BackgroundColor = BackgroundColor,
            TitleColor = TitleColor,
            TextColor = TextColor,
            AppNameColor = AppNameColor,
            BackgroundImagePath = BackgroundImagePath,
            BackgroundImageOpacity = BackgroundImageOpacity,
            BackgroundImageHueDegrees = BackgroundImageHueDegrees,
            BackgroundImageBrightness = BackgroundImageBrightness
        };
    }
}
