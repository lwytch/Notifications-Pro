namespace NotificationsPro.Helpers;

public static class HighlightAnimationHelper
{
    public const string None = "None";
    public const string Flash = "Flash";
    public const string Pulse = "Pulse";
    public const string Shake = "Shake";

    public static IReadOnlyList<string> KnownModes { get; } =
    [
        None,
        Flash,
        Pulse,
        Shake
    ];

    public static string Normalize(string? mode)
    {
        if (string.Equals(mode, Flash, StringComparison.OrdinalIgnoreCase))
            return Flash;
        if (string.Equals(mode, Pulse, StringComparison.OrdinalIgnoreCase))
            return Pulse;
        if (string.Equals(mode, Shake, StringComparison.OrdinalIgnoreCase))
            return Shake;

        return None;
    }
}
