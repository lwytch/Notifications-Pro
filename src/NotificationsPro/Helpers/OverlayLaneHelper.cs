namespace NotificationsPro.Helpers;

public static class OverlayLaneHelper
{
    public const string Main = "Main";
    public const string Secondary = "Secondary";

    public static IReadOnlyList<string> KnownLanes { get; } = new[]
    {
        Main,
        Secondary
    };

    public static string Normalize(string? lane)
    {
        if (string.Equals(lane, Secondary, StringComparison.OrdinalIgnoreCase))
            return Secondary;

        return Main;
    }
}
