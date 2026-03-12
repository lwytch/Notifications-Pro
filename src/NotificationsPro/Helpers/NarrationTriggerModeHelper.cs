namespace NotificationsPro.Helpers;

public static class NarrationTriggerModeHelper
{
    public const string AllAllowedNotifications = "All Allowed Notifications";
    public const string OnlyMatchingNarrationRules = "Only Matching Narration Rules";

    public static IReadOnlyList<string> KnownModes { get; } =
    [
        AllAllowedNotifications,
        OnlyMatchingNarrationRules
    ];

    public static string Normalize(string? mode)
    {
        if (string.Equals(mode, OnlyMatchingNarrationRules, StringComparison.OrdinalIgnoreCase))
            return OnlyMatchingNarrationRules;

        return AllAllowedNotifications;
    }
}
