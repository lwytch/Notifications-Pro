using System.Globalization;
using System.Text.RegularExpressions;

namespace NotificationsPro.Helpers;

public static partial class VoiceAccessTextFormatter
{
    public const string ModeOff = "Off";
    public const string ModeBodyOnly = "Body Only";
    public const string ModeTitleBodyTimestamp = "Title + Body + Timestamp";

    private const string GenericNotificationLabel = "Notification";

    public static string BuildAutomationName(
        string? title,
        string? body,
        string? relativeTimestamp,
        DateTime receivedAt,
        string? mode,
        string? timestampDisplayMode,
        CultureInfo? culture = null)
    {
        var normalizedMode = NormalizeMode(mode);

        if (normalizedMode == ModeOff)
            return GenericNotificationLabel;

        if (normalizedMode == ModeBodyOnly)
            return FirstNonEmpty(body, title) ?? GenericNotificationLabel;

        var timestamp = TimestampTextFormatter.Format(relativeTimestamp, receivedAt, timestampDisplayMode, culture);
        return JoinNonEmpty(title, body, timestamp) ?? FirstNonEmpty(body, title, timestamp) ?? GenericNotificationLabel;
    }

    public static bool IncludesTimestamp(string? mode) => NormalizeMode(mode) == ModeTitleBodyTimestamp;

    public static string NormalizeMode(string? mode)
    {
        if (string.Equals(mode, ModeBodyOnly, StringComparison.OrdinalIgnoreCase))
            return ModeBodyOnly;

        if (string.Equals(mode, ModeTitleBodyTimestamp, StringComparison.OrdinalIgnoreCase))
            return ModeTitleBodyTimestamp;

        return ModeOff;
    }

    private static string? JoinNonEmpty(params string?[] parts)
    {
        var segments = parts
            .Select(Sanitize)
            .Where(static part => !string.IsNullOrWhiteSpace(part))
            .ToList();

        return segments.Count == 0 ? null : string.Join(". ", segments);
    }

    private static string? FirstNonEmpty(params string?[] parts)
    {
        foreach (var part in parts)
        {
            var sanitized = Sanitize(part);
            if (!string.IsNullOrWhiteSpace(sanitized))
                return sanitized;
        }

        return null;
    }

    private static string? Sanitize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        return WhitespaceRegex().Replace(text, " ").Trim();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
