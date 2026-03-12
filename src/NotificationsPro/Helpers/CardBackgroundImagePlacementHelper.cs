namespace NotificationsPro.Helpers;

public static class CardBackgroundImagePlacementHelper
{
    public const string InsidePadding = "Inside Padding";
    public const string FullCard = "Full Card";

    public static IReadOnlyList<string> KnownPlacements { get; } = new[]
    {
        InsidePadding,
        FullCard
    };

    public static string Normalize(string? placement)
    {
        if (string.Equals(placement, FullCard, StringComparison.OrdinalIgnoreCase))
            return FullCard;

        return InsidePadding;
    }
}
