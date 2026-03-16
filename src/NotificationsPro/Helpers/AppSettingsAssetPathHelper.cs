using NotificationsPro.Models;
using NotificationsPro.Services;

namespace NotificationsPro.Helpers;

public static class AppSettingsAssetPathHelper
{
    public static AppSettings CreatePortableSnapshot(AppSettings settings)
    {
        var snapshot = settings.Clone();
        NormalizeForRuntime(snapshot);

        snapshot.DefaultSound = ToPortableSoundReference(snapshot.DefaultSound);
        snapshot.PerAppSounds = snapshot.PerAppSounds
            .Select(entry => new KeyValuePair<string, string>(entry.Key, ToPortableSoundReference(entry.Value)))
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Key) && !IsDefaultSound(entry.Value))
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);

        snapshot.DefaultIconPreset = ToPortableIconReference(snapshot.DefaultIconPreset);
        snapshot.PerAppIcons = snapshot.PerAppIcons
            .Select(entry => new KeyValuePair<string, string>(entry.Key, ToPortableIconReference(entry.Value)))
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Key)
                && !string.IsNullOrWhiteSpace(entry.Value)
                && !string.Equals(entry.Value, "None", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);

        snapshot.CardBackgroundImagePath = ManagedAssetPathHelper.ToPortableManagedPathOrEmpty(
            snapshot.CardBackgroundImagePath,
            ManagedAssetPathHelper.BackgroundsFolderName);
        snapshot.FullscreenOverlayImagePath = ManagedAssetPathHelper.ToPortableManagedPathOrEmpty(
            snapshot.FullscreenOverlayImagePath,
            ManagedAssetPathHelper.BackgroundsFolderName);
        snapshot.PerAppBackgroundImages = snapshot.PerAppBackgroundImages
            .Select(entry => new KeyValuePair<string, string>(
                entry.Key,
                ManagedAssetPathHelper.ToPortableManagedPathOrEmpty(
                    entry.Value,
                    ManagedAssetPathHelper.BackgroundsFolderName)))
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Key) && !string.IsNullOrWhiteSpace(entry.Value))
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);

        return snapshot;
    }

    public static void NormalizeForRuntime(AppSettings settings)
    {
        settings.DefaultSound = NormalizeSoundReference(settings.DefaultSound);
        settings.PerAppSounds = settings.PerAppSounds
            .Select(entry => new KeyValuePair<string, string>(entry.Key, NormalizeSoundReference(entry.Value)))
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Key) && !IsDefaultSound(entry.Value))
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);

        settings.DefaultIconPreset = NormalizeIconReference(settings.DefaultIconPreset);
        settings.PerAppIcons = settings.PerAppIcons
            .Select(entry => new KeyValuePair<string, string>(entry.Key, NormalizeIconReference(entry.Value)))
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Key)
                && !string.IsNullOrWhiteSpace(entry.Value)
                && !string.Equals(entry.Value, "None", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);

        settings.CardBackgroundImagePath = ManagedAssetPathHelper.ResolveManagedPathOrEmpty(
            settings.CardBackgroundImagePath,
            ManagedAssetPathHelper.BackgroundsFolderName);
        settings.FullscreenOverlayImagePath = ManagedAssetPathHelper.ResolveManagedPathOrEmpty(
            settings.FullscreenOverlayImagePath,
            ManagedAssetPathHelper.BackgroundsFolderName);
        settings.PerAppBackgroundImages = settings.PerAppBackgroundImages
            .Select(entry => new KeyValuePair<string, string>(
                entry.Key,
                ManagedAssetPathHelper.ResolveManagedPathOrEmpty(
                    entry.Value,
                    ManagedAssetPathHelper.BackgroundsFolderName)))
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Key) && !string.IsNullOrWhiteSpace(entry.Value))
            .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeSoundReference(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (IsDefaultSound(trimmed))
            return "None";

        var expanded = Environment.ExpandEnvironmentVariables(trimmed);
        var trustedSystemSound = SoundService.GetWindowsSounds()
            .FirstOrDefault(sound => string.Equals(sound.WavPath, expanded, StringComparison.OrdinalIgnoreCase));
        if (trustedSystemSound != null)
            return trustedSystemSound.WavPath;

        var managedSoundPath = ManagedAssetPathHelper.ResolveManagedPathOrEmpty(
            expanded,
            ManagedAssetPathHelper.SoundsFolderName);

        return string.IsNullOrWhiteSpace(managedSoundPath)
            ? "None"
            : managedSoundPath;
    }

    private static string ToPortableSoundReference(string? value)
    {
        var normalized = NormalizeSoundReference(value);
        if (IsDefaultSound(normalized))
            return "None";

        var portableManagedSound = ManagedAssetPathHelper.ToPortableManagedPathOrEmpty(
            normalized,
            ManagedAssetPathHelper.SoundsFolderName);

        return string.IsNullOrWhiteSpace(portableManagedSound)
            ? normalized
            : portableManagedSound;
    }

    private static string NormalizeIconReference(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed) || string.Equals(trimmed, "None", StringComparison.OrdinalIgnoreCase))
            return "None";

        if (IconPreset.BuiltInIcons.ContainsKey(trimmed))
            return trimmed;

        return ManagedAssetPathHelper.ResolveManagedPathOrEmpty(trimmed, ManagedAssetPathHelper.IconsFolderName);
    }

    private static string ToPortableIconReference(string? value)
    {
        var normalized = NormalizeIconReference(value);
        if (string.IsNullOrWhiteSpace(normalized) || string.Equals(normalized, "None", StringComparison.OrdinalIgnoreCase))
            return "None";

        if (IconPreset.BuiltInIcons.ContainsKey(normalized))
            return normalized;

        return ManagedAssetPathHelper.ToPortableManagedPathOrEmpty(normalized, ManagedAssetPathHelper.IconsFolderName);
    }

    private static bool IsDefaultSound(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || string.Equals(value, "None", StringComparison.OrdinalIgnoreCase);
    }
}
