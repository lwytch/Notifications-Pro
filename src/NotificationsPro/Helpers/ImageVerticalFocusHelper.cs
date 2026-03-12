using System.Windows;

namespace NotificationsPro.Helpers;

public static class ImageVerticalFocusHelper
{
    public const string Top = "Top";
    public const string Center = "Center";
    public const string Bottom = "Bottom";

    public static IReadOnlyList<string> KnownModes { get; } = new[]
    {
        Top,
        Center,
        Bottom
    };

    public static string Normalize(string? mode)
    {
        if (string.Equals(mode, Top, StringComparison.OrdinalIgnoreCase))
            return Top;

        if (string.Equals(mode, Bottom, StringComparison.OrdinalIgnoreCase))
            return Bottom;

        return Center;
    }

    public static VerticalAlignment ToVerticalAlignment(string? mode)
    {
        return Normalize(mode) switch
        {
            Top => VerticalAlignment.Top,
            Bottom => VerticalAlignment.Bottom,
            _ => VerticalAlignment.Center
        };
    }
}
