using System.Globalization;

namespace NotificationsPro.Helpers;

public static class TimestampTextFormatter
{
    public static string Format(string? relative, DateTime receivedAt, string? mode, CultureInfo? culture = null)
    {
        var effectiveCulture = culture ?? CultureInfo.CurrentCulture;
        var normalizedMode = NormalizeMode(mode);

        if (normalizedMode == "Time")
            return receivedAt.ToString("HH:mm", effectiveCulture);

        if (normalizedMode == "DateTime")
            return receivedAt.ToString("g", effectiveCulture);

        return string.IsNullOrWhiteSpace(relative) ? "just now" : relative;
    }

    public static string NormalizeMode(string? mode)
    {
        if (string.Equals(mode, "Time", StringComparison.OrdinalIgnoreCase))
            return "Time";

        if (string.Equals(mode, "DateTime", StringComparison.OrdinalIgnoreCase))
            return "DateTime";

        return "Relative";
    }
}
