namespace NotificationsPro.Helpers;

public static class HighlightBorderModeHelper
{
    public const string FullBorder = "Full Border";
    public const string AccentSideOnly = "Accent Side Only";
    public const string NoBorder = "No Border";

    public static IReadOnlyList<string> KnownModes { get; } =
    [
        FullBorder,
        AccentSideOnly,
        NoBorder
    ];

    public static string Normalize(string? mode)
    {
        if (string.Equals(mode, AccentSideOnly, StringComparison.OrdinalIgnoreCase))
            return AccentSideOnly;
        if (string.Equals(mode, NoBorder, StringComparison.OrdinalIgnoreCase))
            return NoBorder;

        return FullBorder;
    }
}
