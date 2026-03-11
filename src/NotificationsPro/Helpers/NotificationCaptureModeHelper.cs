namespace NotificationsPro.Helpers;

public static class NotificationCaptureModeHelper
{
    public const string ModeAuto = "Auto";
    public const string ModeWinRt = "Prefer WinRT";
    public const string ModeAccessibility = "Force Accessibility";

    public static string NormalizeMode(string? mode)
    {
        if (string.Equals(mode, ModeWinRt, StringComparison.OrdinalIgnoreCase))
            return ModeWinRt;

        if (string.Equals(mode, ModeAccessibility, StringComparison.OrdinalIgnoreCase))
            return ModeAccessibility;

        return ModeAuto;
    }
}
