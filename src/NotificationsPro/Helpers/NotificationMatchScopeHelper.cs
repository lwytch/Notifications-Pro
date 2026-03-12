namespace NotificationsPro.Helpers;

public static class NotificationMatchScopeHelper
{
    public const string TitleOnly = "Title Only";
    public const string BodyOnly = "Body Only";
    public const string TitleAndBody = "Title + Body";

    public static IReadOnlyList<string> KnownScopes { get; } = new[]
    {
        TitleOnly,
        BodyOnly,
        TitleAndBody
    };

    public static string Normalize(string? scope)
    {
        if (string.Equals(scope, TitleOnly, StringComparison.OrdinalIgnoreCase))
            return TitleOnly;

        if (string.Equals(scope, BodyOnly, StringComparison.OrdinalIgnoreCase))
            return BodyOnly;

        return TitleAndBody;
    }

    public static string GetTextForScope(string? title, string? body, string? scope)
    {
        var normalizedScope = Normalize(scope);
        var normalizedTitle = title?.Trim() ?? string.Empty;
        var normalizedBody = body?.Trim() ?? string.Empty;

        return normalizedScope switch
        {
            BodyOnly => normalizedBody,
            TitleOnly => normalizedTitle,
            _ => string.Join(" ", new[] { normalizedTitle, normalizedBody }
                .Where(value => !string.IsNullOrWhiteSpace(value)))
        };
    }
}
