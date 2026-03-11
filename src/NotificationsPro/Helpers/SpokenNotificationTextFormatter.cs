using System.Text.RegularExpressions;

namespace NotificationsPro.Helpers;

public static partial class SpokenNotificationTextFormatter
{
    public const string ModeBodyOnly = "Body Only";
    public const string ModeTitleOnly = "Title Only";
    public const string ModeTitleBody = "Title + Body";
    public const string ModeBodyTimestamp = "Body + Timestamp";
    public const string ModeTitleTimestamp = "Title + Timestamp";
    public const string ModeTitleBodyTimestamp = "Title + Body + Timestamp";

    public static string NormalizeMode(string? mode)
    {
        if (string.Equals(mode, ModeTitleOnly, StringComparison.OrdinalIgnoreCase))
            return ModeTitleOnly;

        if (string.Equals(mode, ModeTitleBody, StringComparison.OrdinalIgnoreCase))
            return ModeTitleBody;

        if (string.Equals(mode, ModeBodyTimestamp, StringComparison.OrdinalIgnoreCase))
            return ModeBodyTimestamp;

        if (string.Equals(mode, ModeTitleTimestamp, StringComparison.OrdinalIgnoreCase))
            return ModeTitleTimestamp;

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
        var includeTitle = normalizedMode is ModeTitleOnly or ModeTitleBody or ModeTitleTimestamp or ModeTitleBodyTimestamp;
        var includeBody = normalizedMode is ModeBodyOnly or ModeTitleBody or ModeBodyTimestamp or ModeTitleBodyTimestamp;
        var includeTimestamp = normalizedMode is ModeBodyTimestamp or ModeTitleTimestamp or ModeTitleBodyTimestamp;

        var parts = new List<string>();
        if (includeTitle && !string.IsNullOrWhiteSpace(normalizedTitle))
            parts.Add(normalizedTitle);
        if (includeBody && !string.IsNullOrWhiteSpace(normalizedBody))
            parts.Add(normalizedBody);

        // If the requested text field is missing, still read whichever card text exists.
        if (parts.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(normalizedBody))
                parts.Add(normalizedBody);
            else if (!string.IsNullOrWhiteSpace(normalizedTitle))
                parts.Add(normalizedTitle);
        }

        if (includeTimestamp)
        {
            var relativeTimestamp = BuildRelativeTimestamp(receivedAt);
            var timestamp = NormalizeWhitespace(TimestampTextFormatter.Format(relativeTimestamp, receivedAt, timestampDisplayMode));
            if (!string.IsNullOrWhiteSpace(timestamp))
                parts.Add(timestamp);
        }

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
