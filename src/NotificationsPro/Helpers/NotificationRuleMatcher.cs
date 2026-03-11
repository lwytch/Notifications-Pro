using System.Text.RegularExpressions;
using NotificationsPro.Models;

namespace NotificationsPro.Helpers;

public static class NotificationRuleMatcher
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    public static bool Matches(
        string appName,
        string title,
        string body,
        string keyword,
        bool isRegex,
        string? scope,
        string? appFilter)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return false;

        if (!MatchesAppFilter(appName, appFilter))
            return false;

        var sourceText = NotificationMatchScopeHelper.GetTextForScope(title, body, scope);
        if (string.IsNullOrWhiteSpace(sourceText))
            return false;

        return IsPatternMatch(sourceText, keyword, isRegex);
    }

    public static string? FindMatchingHighlightColor(
        IEnumerable<HighlightRuleDefinition> rules,
        string appName,
        string title,
        string body,
        string fallbackColor)
    {
        foreach (var rule in rules)
        {
            if (!Matches(appName, title, body, rule.Keyword, rule.IsRegex, rule.Scope, rule.AppFilter))
                continue;

            return string.IsNullOrWhiteSpace(rule.Color) ? fallbackColor : rule.Color;
        }

        return null;
    }

    public static NarrationRuleDefinition? FindMatchingNarrationRule(
        IEnumerable<NarrationRuleDefinition> rules,
        string appName,
        string title,
        string body)
    {
        return rules.FirstOrDefault(rule =>
            Matches(appName, title, body, rule.Keyword, rule.IsRegex, rule.Scope, rule.AppFilter));
    }

    public static bool MatchesAny(
        IEnumerable<MuteRuleDefinition> rules,
        string appName,
        string title,
        string body)
    {
        return rules.Any(rule =>
            Matches(appName, title, body, rule.Keyword, rule.IsRegex, rule.Scope, rule.AppFilter));
    }

    public static bool MatchesAppFilter(string appName, string? appFilter)
    {
        if (string.IsNullOrWhiteSpace(appFilter))
            return true;

        var normalizedAppName = appName?.Trim() ?? string.Empty;
        var normalizedFilter = appFilter.Trim();
        if (string.IsNullOrWhiteSpace(normalizedAppName))
            return false;

        return normalizedAppName.IndexOf(normalizedFilter, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsPatternMatch(string sourceText, string keyword, bool isRegex)
    {
        var pattern = isRegex
            ? keyword
            : BuildLiteralPattern(keyword);

        try
        {
            return Regex.IsMatch(sourceText, pattern, RegexOptions.IgnoreCase, RegexTimeout);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static string BuildLiteralPattern(string keyword)
    {
        var escaped = Regex.Escape(keyword);
        var startsWithWordChar = keyword.Length > 0 && (char.IsLetterOrDigit(keyword[0]) || keyword[0] == '_');
        var endsWithWordChar = keyword.Length > 0 && (char.IsLetterOrDigit(keyword[^1]) || keyword[^1] == '_');

        if (startsWithWordChar && endsWithWordChar)
            return $@"\b{escaped}\b";

        return escaped;
    }
}
