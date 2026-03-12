using NotificationsPro.Helpers;

namespace NotificationsPro.Models;

public class AppProfile
{
    public string AppName { get; set; } = string.Empty;
    public bool IsReadAloudEnabled { get; set; } = true;
    public string OverlayLane { get; set; } = OverlayLaneHelper.Main;
    public string Sound { get; set; } = "Default";
    public string Icon { get; set; } = "Default";
    public string AccentColor { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = string.Empty;
    public string TitleColor { get; set; } = string.Empty;
    public string TextColor { get; set; } = string.Empty;
    public string AppNameColor { get; set; } = string.Empty;
    public string BackgroundImagePath { get; set; } = string.Empty;
    public double BackgroundImageOpacity { get; set; } = 0.45;
    public double BackgroundImageHueDegrees { get; set; }
    public double BackgroundImageBrightness { get; set; } = 1.0;

    public AppProfile Clone()
    {
        return new AppProfile
        {
            AppName = AppName,
            IsReadAloudEnabled = IsReadAloudEnabled,
            OverlayLane = OverlayLaneHelper.Normalize(OverlayLane),
            Sound = Sound,
            Icon = Icon,
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
