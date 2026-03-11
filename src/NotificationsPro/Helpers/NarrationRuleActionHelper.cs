namespace NotificationsPro.Helpers;

public static class NarrationRuleActionHelper
{
    public const string ReadAloud = "Read Aloud";
    public const string SkipReadAloud = "Skip Read Aloud";

    public static IReadOnlyList<string> KnownActions { get; } = new[]
    {
        ReadAloud,
        SkipReadAloud
    };

    public static string Normalize(string? action)
    {
        if (string.Equals(action, SkipReadAloud, StringComparison.OrdinalIgnoreCase))
            return SkipReadAloud;

        return ReadAloud;
    }
}
