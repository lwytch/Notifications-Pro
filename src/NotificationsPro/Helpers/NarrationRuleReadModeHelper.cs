namespace NotificationsPro.Helpers;

public static class NarrationRuleReadModeHelper
{
    public const string UseGlobal = "Use Global";

    public static string Normalize(string? readMode)
    {
        if (string.IsNullOrWhiteSpace(readMode))
            return UseGlobal;

        if (string.Equals(readMode, UseGlobal, StringComparison.OrdinalIgnoreCase))
            return UseGlobal;

        return SpokenNotificationTextFormatter.NormalizeMode(readMode);
    }
}
