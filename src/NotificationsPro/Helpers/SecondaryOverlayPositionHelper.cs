namespace NotificationsPro.Helpers;

public static class SecondaryOverlayPositionHelper
{
    public const string TopLeft = "Top Left";
    public const string TopCenter = "Top Center";
    public const string TopRight = "Top Right";
    public const string MiddleLeft = "Middle Left";
    public const string MiddleCenter = "Middle Center";
    public const string MiddleRight = "Middle Right";
    public const string BottomLeft = "Bottom Left";
    public const string BottomCenter = "Bottom Center";
    public const string BottomRight = "Bottom Right";

    public static string Normalize(string? positionPreset)
    {
        var normalized = positionPreset?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "top-left" or "top left" => TopLeft,
            "top-center" or "top center" => TopCenter,
            "top-right" or "top right" => TopRight,
            "middle-left" or "middle left" => MiddleLeft,
            "middle-center" or "middle center" => MiddleCenter,
            "middle-right" or "middle right" => MiddleRight,
            "bottom-left" or "bottom left" => BottomLeft,
            "bottom-center" or "bottom center" => BottomCenter,
            "bottom-right" or "bottom right" => BottomRight,
            _ => TopLeft
        };
    }
}
