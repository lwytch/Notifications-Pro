using System.Text.RegularExpressions;

namespace NotificationsPro.Helpers;

public static partial class SpokenNotificationTextFormatter
{
    public const string ModeBodyOnly = "Body Only";
    public const string ModeTitleBodyTimestamp = "Title + Body + Timestamp";

    public static string NormalizeMode(string? mode)
    {
        if (string.Equals(mode, ModeTitleBodyTimestamp, StringComparison.OrdinalIgnoreCase))
            return ModeTitleBodyTimestamp;

        return ModeBodyOnly;
    }

    public static string BuildText(
        string? title,
        string? body,
        DateTime receivedAt,
        string? mode,
        string? timestampDisplayMode)
    {
        var normalizedMode = NormalizeMode(mode);
        var normalizedTitle = NormalizeWhitespace(title);
        var normalizedBody = NormalizeWhitespace(body);

        if (normalizedMode == ModeBodyOnly)
            return !string.IsNullOrWhiteSpace(normalizedBody) ? normalizedBody : normalizedTitle;

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(normalizedTitle))
            parts.Add(normalizedTitle);
        if (!string.IsNullOrWhiteSpace(normalizedBody))
            parts.Add(normalizedBody);

        var relativeTimestamp = BuildRelativeTimestamp(receivedAt);
        var timestamp = NormalizeWhitespace(TimestampTextFormatter.Format(relativeTimestamp, receivedAt, timestampDisplayMode));
        if (!string.IsNullOrWhiteSpace(timestamp))
            parts.Add(timestamp);

        return string.Join(". ", parts);
    }

    private static string BuildRelativeTimestamp(DateTime receivedAt)
    {
        var elapsed = DateTime.Now - receivedAt;
        if (elapsed.TotalSeconds < 10)
            return "just now";
        if (elapsed.TotalSeconds < 60)
            return $"{(int)elapsed.TotalSeconds}s ago";
        if (elapsed.TotalMinutes < 60)
            return $"{(int)elapsed.TotalMinutes}m ago";

        return $"{(int)elapsed.TotalHours}h ago";
    }

    private static string NormalizeWhitespace(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return WhitespaceRegex().Replace(value.Trim(), " ");
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
