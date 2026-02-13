namespace NotificationsPro.Helpers;

/// <summary>
/// Generates a deterministic color tint from an app name for per-app card background coloring.
/// </summary>
public static class AppTintHelper
{
    private static readonly string[] TintPalette =
    {
        "#3B82F6", // blue
        "#8B5CF6", // violet
        "#EC4899", // pink
        "#EF4444", // red
        "#F97316", // orange
        "#EAB308", // yellow
        "#22C55E", // green
        "#14B8A6", // teal
        "#06B6D4", // cyan
        "#6366F1", // indigo
    };

    /// <summary>
    /// Returns a hex color string for the given app name, deterministic based on hash.
    /// </summary>
    public static string GetTintColor(string appName)
    {
        if (string.IsNullOrWhiteSpace(appName))
            return TintPalette[0];

        var hash = GetStableHash(appName);
        var index = Math.Abs(hash) % TintPalette.Length;
        return TintPalette[index];
    }

    private static int GetStableHash(string value)
    {
        // Simple FNV-1a hash for deterministic cross-session results
        unchecked
        {
            int hash = unchecked((int)2166136261);
            foreach (var c in value)
            {
                hash ^= c;
                hash *= 16777619;
            }
            return hash;
        }
    }
}
