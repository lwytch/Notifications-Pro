using System.IO;

namespace NotificationsPro.Helpers;

public static class ManagedAssetPathHelper
{
    public const string IconsFolderName = "icons";
    public const string SoundsFolderName = "sounds";
    public const string BackgroundsFolderName = "backgrounds";

    private static readonly string AppDataRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "NotificationsPro");

    public static string GetRoot(string folderName)
    {
        return Path.Combine(AppDataRoot, folderName);
    }

    public static string ResolveManagedPathOrEmpty(string? value, string folderName)
    {
        return TryResolveManagedPath(value, folderName, out var resolvedPath)
            ? resolvedPath
            : string.Empty;
    }

    public static string ToPortableManagedPathOrEmpty(string? value, string folderName)
    {
        if (!TryResolveManagedPath(value, folderName, out var resolvedPath))
            return string.Empty;

        var root = Path.GetFullPath(GetRoot(folderName));
        var relative = Path.GetRelativePath(root, resolvedPath)
            .Replace('\\', '/')
            .TrimStart('/');

        return string.IsNullOrWhiteSpace(relative)
            ? string.Empty
            : $"{folderName}/{relative}";
    }

    private static bool TryResolveManagedPath(string? value, string folderName, out string resolvedPath)
    {
        resolvedPath = string.Empty;

        var trimmed = Environment.ExpandEnvironmentVariables(value?.Trim() ?? string.Empty);
        if (string.IsNullOrWhiteSpace(trimmed))
            return false;

        var root = Path.GetFullPath(GetRoot(folderName));
        string candidatePath;

        if (Path.IsPathRooted(trimmed))
        {
            candidatePath = Path.GetFullPath(trimmed);
        }
        else
        {
            var normalizedRelative = trimmed
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);

            var folderPrefix = folderName + Path.DirectorySeparatorChar;
            if (normalizedRelative.StartsWith(folderPrefix, StringComparison.OrdinalIgnoreCase))
                normalizedRelative = normalizedRelative[folderPrefix.Length..];
            else if (string.Equals(normalizedRelative, folderName, StringComparison.OrdinalIgnoreCase))
                normalizedRelative = string.Empty;

            if (string.IsNullOrWhiteSpace(normalizedRelative))
                return false;

            candidatePath = Path.GetFullPath(Path.Combine(root, normalizedRelative));
        }

        if (candidatePath.StartsWith(@"\\", StringComparison.Ordinal))
            return false;

        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;

        if (!candidatePath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            return false;

        resolvedPath = candidatePath;
        return true;
    }
}
