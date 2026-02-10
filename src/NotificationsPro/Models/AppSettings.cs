namespace NotificationsPro.Models;

public class AppSettings
{
    // Appearance
    public string FontFamily { get; set; } = "Segoe UI";
    public double FontSize { get; set; } = 14;
    public string FontWeight { get; set; } = "Normal";
    public double LineSpacing { get; set; } = 1.5;
    public string TextColor { get; set; } = "#E4E4EF";
    public string TitleColor { get; set; } = "#FFFFFF";
    public string AppNameColor { get; set; } = "#B8B8CC";
    public string BackgroundColor { get; set; } = "#1E1E2E";
    public double BackgroundOpacity { get; set; } = 0.92;
    public double CornerRadius { get; set; } = 12;
    public double Padding { get; set; } = 16;
    public bool ShowBorder { get; set; } = true;
    public string BorderColor { get; set; } = "#7C5CFC";
    public double BorderThickness { get; set; } = 1;
    public string AccentColor { get; set; } = "#7C5CFC";

    // Behavior
    public double NotificationDuration { get; set; } = 5;
    public int MaxVisibleNotifications { get; set; } = 3;
    public bool ShowAppName { get; set; } = true;
    public bool ShowNotificationTitle { get; set; } = true;
    public bool ShowNotificationBody { get; set; } = true;
    public bool LimitTextLines { get; set; } = false;
    public int MaxAppNameLines { get; set; } = 2;
    public int MaxTitleLines { get; set; } = 2;
    public int MaxBodyLines { get; set; } = 4;
    public bool SingleLineMode { get; set; } = false;
    public bool AlwaysOnTop { get; set; } = true;
    public bool ClickThrough { get; set; } = false;
    public bool AnimationsEnabled { get; set; } = true;
    public bool FadeOnlyAnimation { get; set; } = false;
    public double AnimationDurationMs { get; set; } = 300;
    public bool NotificationsPaused { get; set; } = false;

    // Position & Size (null = not yet positioned, use default)
    public double? OverlayLeft { get; set; }
    public double? OverlayTop { get; set; }
    public double OverlayWidth { get; set; } = 380;
    public double OverlayMaxHeight { get; set; } = 600;
    public bool AllowManualResize { get; set; } = true;
    public int MonitorIndex { get; set; } = 0;
    public bool SnapToEdges { get; set; } = true;
    public double SnapDistance { get; set; } = 20;

    // Overlay visibility
    public bool OverlayVisible { get; set; } = true;

    public AppSettings Clone()
    {
        return (AppSettings)MemberwiseClone();
    }
}
