namespace NotificationsPro.Helpers;

public static class CardBackgroundImageFitModeHelper
{
    public const string FillCard = "Fill Card";
    public const string FitInsideCard = "Fit Inside Card";
    public const string OriginalSize = "Original Size";

    public static IReadOnlyList<string> KnownModes { get; } = new[]
    {
        FillCard,
        FitInsideCard,
        OriginalSize
    };

    public static string Normalize(string? mode)
    {
        if (string.Equals(mode, FitInsideCard, StringComparison.OrdinalIgnoreCase))
            return FitInsideCard;

        if (string.Equals(mode, OriginalSize, StringComparison.OrdinalIgnoreCase))
            return OriginalSize;

        return FillCard;
    }

    public static System.Windows.Media.Stretch ToStretch(string? mode)
    {
        return Normalize(mode) switch
        {
            FitInsideCard => System.Windows.Media.Stretch.Uniform,
            OriginalSize => System.Windows.Media.Stretch.None,
            _ => System.Windows.Media.Stretch.UniformToFill
        };
    }
}
