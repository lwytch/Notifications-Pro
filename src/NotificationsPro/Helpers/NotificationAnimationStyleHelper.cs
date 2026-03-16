namespace NotificationsPro.Helpers;

public static class NotificationAnimationStyleHelper
{
    public const string SlideFade = "Slide + Fade";
    public const string Slide = "Slide";
    public const string Fade = "Fade";
    public const string DriftFade = "Drift + Fade";
    public const string ZoomFade = "Zoom + Fade";
    public const string Pop = "Pop";

    public static IReadOnlyList<string> KnownModes { get; } =
    [
        SlideFade,
        Slide,
        Fade,
        DriftFade,
        ZoomFade,
        Pop
    ];

    public static string Normalize(string? mode)
    {
        if (string.Equals(mode, Slide, StringComparison.OrdinalIgnoreCase))
            return Slide;
        if (string.Equals(mode, Fade, StringComparison.OrdinalIgnoreCase))
            return Fade;
        if (string.Equals(mode, DriftFade, StringComparison.OrdinalIgnoreCase))
            return DriftFade;
        if (string.Equals(mode, ZoomFade, StringComparison.OrdinalIgnoreCase))
            return ZoomFade;
        if (string.Equals(mode, Pop, StringComparison.OrdinalIgnoreCase))
            return Pop;

        return SlideFade;
    }

    public static string FromLegacyFadeOnly(bool fadeOnly)
        => fadeOnly ? Fade : SlideFade;

    public static bool UsesDirection(string? mode)
    {
        return Normalize(mode) switch
        {
            Slide => true,
            SlideFade => true,
            DriftFade => true,
            _ => false
        };
    }

    public static bool ShouldDelayHighlight(string? mode)
        => Normalize(mode) != Fade;

    public static bool IsLegacyFadeOnly(string? mode)
        => Normalize(mode) == Fade;
}
