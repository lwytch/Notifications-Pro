using NotificationsPro.Models;

namespace NotificationsPro.Helpers;

public static class OverlayLaneHelper
{
    public const string Main = "main";
    public const string Secondary = "secondary";
    public const string MainDisplayName = "Lane 1";
    public const string SecondaryDisplayName = "Lane 2";

    public static string Normalize(string? laneId)
    {
        if (string.IsNullOrWhiteSpace(laneId))
            return Main;

        if (string.Equals(laneId, Main, StringComparison.OrdinalIgnoreCase)
            || string.Equals(laneId, "Main", StringComparison.OrdinalIgnoreCase)
            || string.Equals(laneId, MainDisplayName, StringComparison.OrdinalIgnoreCase))
        {
            return Main;
        }

        if (string.Equals(laneId, Secondary, StringComparison.OrdinalIgnoreCase)
            || string.Equals(laneId, "Secondary", StringComparison.OrdinalIgnoreCase)
            || string.Equals(laneId, SecondaryDisplayName, StringComparison.OrdinalIgnoreCase))
        {
            return Secondary;
        }

        return laneId.Trim();
    }

    public static string NormalizeOrMain(string? laneId, IEnumerable<OverlayLaneDefinition> lanes)
    {
        var normalized = Normalize(laneId);
        if (string.Equals(normalized, Main, StringComparison.OrdinalIgnoreCase))
            return Main;

        return lanes.Any(lane => string.Equals(lane.Id, normalized, StringComparison.OrdinalIgnoreCase))
            ? normalized
            : Main;
    }

    public static OverlayLaneDefinition? FindLane(IEnumerable<OverlayLaneDefinition> lanes, string? laneId)
    {
        var normalized = Normalize(laneId);
        return lanes.FirstOrDefault(lane => string.Equals(lane.Id, normalized, StringComparison.OrdinalIgnoreCase));
    }

    public static string GetDisplayName(IEnumerable<OverlayLaneDefinition> lanes, string? laneId)
    {
        var normalized = Normalize(laneId);
        if (string.Equals(normalized, Main, StringComparison.OrdinalIgnoreCase))
            return MainDisplayName;

        var match = FindLane(lanes, normalized);
        if (match != null && !string.IsNullOrWhiteSpace(match.Name))
            return match.Name;

        return string.Equals(normalized, Secondary, StringComparison.OrdinalIgnoreCase)
            ? SecondaryDisplayName
            : normalized;
    }

    public static string Slugify(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "lane";

        var normalized = new string(name.Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray());

        while (normalized.Contains("--", StringComparison.Ordinal))
            normalized = normalized.Replace("--", "-", StringComparison.Ordinal);

        normalized = normalized.Trim('-');
        return string.IsNullOrWhiteSpace(normalized) ? "lane" : normalized;
    }
}
