namespace NotificationsPro.Helpers;

public static class CardBackgroundModeHelper
{
    public const string Solid = "Solid";
    public const string Image = "Image";

    public static IReadOnlyList<string> KnownModes { get; } = new[]
    {
        Solid,
        Image
    };

    public static string Normalize(string? mode)
    {
        if (string.Equals(mode, Image, StringComparison.OrdinalIgnoreCase))
            return Image;

        return Solid;
    }
}
