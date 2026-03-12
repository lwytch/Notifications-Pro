using NotificationsPro.Helpers;

namespace NotificationsPro.Models;

public class AppProfile
{
    public string AppName { get; set; } = string.Empty;
    public bool IsReadAloudEnabled { get; set; } = true;
    public string OverlayLane { get; set; } = OverlayLaneHelper.Main;
    public string Sound { get; set; } = "Default";
    public string Icon { get; set; } = "Default";

    public AppProfile Clone()
    {
        return new AppProfile
        {
            AppName = AppName,
            IsReadAloudEnabled = IsReadAloudEnabled,
            OverlayLane = OverlayLaneHelper.Normalize(OverlayLane),
            Sound = Sound,
            Icon = Icon
        };
    }
}
