namespace NotificationsPro.Helpers;

public static class AnimationEasingHelper
{
    public const string EaseOut = "EaseOut";
    public const string Bounce = "Bounce";
    public const string Elastic = "Elastic";
    public const string Linear = "Linear";

    public static IReadOnlyList<string> KnownModes { get; } =
    [
        EaseOut,
        Bounce,
        Elastic,
        Linear
    ];

    public static string Normalize(string? mode)
    {
        if (string.Equals(mode, Bounce, StringComparison.OrdinalIgnoreCase))
            return Bounce;
        if (string.Equals(mode, Elastic, StringComparison.OrdinalIgnoreCase))
            return Elastic;
        if (string.Equals(mode, Linear, StringComparison.OrdinalIgnoreCase))
            return Linear;

        return EaseOut;
    }
}
