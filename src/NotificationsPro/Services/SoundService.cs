using System.IO;
using System.Media;
using Microsoft.Win32;
using NotificationsPro.Models;

namespace NotificationsPro.Services;

/// <summary>
/// Plays notification sounds — Windows registry sounds or custom WAV files.
/// Per-app sounds override the default sound.
/// Sound values stored in settings are WAV file paths (or "None").
/// </summary>
public static class SoundService
{
    private static readonly string CustomSoundsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "NotificationsPro", "sounds");

    /// <summary>
    /// Represents a Windows sound event with its display name and resolved WAV path.
    /// </summary>
    public record WindowsSound(string Name, string WavPath);

    private static List<WindowsSound>? _cachedSounds;

    /// <summary>
    /// Enumerates available Windows sounds from the registry.
    /// Returns sounds sorted by display name, deduplicated by WAV path.
    /// </summary>
    public static IReadOnlyList<WindowsSound> GetWindowsSounds()
    {
        if (_cachedSounds != null)
            return _cachedSounds;

        var sounds = new List<WindowsSound>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            // Enumerate all sound events for the default app (.Default covers system-wide sounds)
            using var eventsKey = Registry.CurrentUser.OpenSubKey(
                @"AppEvents\Schemes\Apps\.Default", writable: false);

            if (eventsKey != null)
            {
                foreach (var eventName in eventsKey.GetSubKeyNames())
                {
                    // Get the WAV path from the .Current subkey (active scheme)
                    var wavPath = ResolveWavPath(eventsKey, eventName);
                    if (string.IsNullOrWhiteSpace(wavPath) || !File.Exists(wavPath))
                        continue;

                    if (!seenPaths.Add(wavPath))
                        continue; // Skip duplicate WAV files

                    // Get the human-readable display name from EventLabels
                    var displayName = GetEventLabel(eventName) ?? FormatEventName(eventName);
                    sounds.Add(new WindowsSound(displayName, wavPath));
                }
            }
        }
        catch { /* Registry access failed — return empty list */ }

        _cachedSounds = sounds
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return _cachedSounds;
    }

    private static string? ResolveWavPath(RegistryKey eventsKey, string eventName)
    {
        try
        {
            using var eventKey = eventsKey.OpenSubKey(eventName, writable: false);
            if (eventKey == null) return null;

            // Try .Current first (active scheme), fall back to .Default
            foreach (var schemeKey in new[] { ".Current", ".Default" })
            {
                using var schemeSubKey = eventKey.OpenSubKey(schemeKey, writable: false);
                var raw = schemeSubKey?.GetValue(null)?.ToString();
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    var expanded = Environment.ExpandEnvironmentVariables(raw);
                    if (File.Exists(expanded))
                        return expanded;
                }
            }
        }
        catch { }
        return null;
    }

    private static string? GetEventLabel(string eventName)
    {
        try
        {
            using var labelKey = Registry.CurrentUser.OpenSubKey(
                $@"AppEvents\EventLabels\{eventName}", writable: false)
                ?? Registry.LocalMachine.OpenSubKey(
                $@"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\{eventName}", writable: false);

            // EventLabels stores the display name as the default value
            using var labelsRoot = Registry.CurrentUser.OpenSubKey(
                $@"AppEvents\EventLabels\{eventName}", writable: false);
            var label = labelsRoot?.GetValue(null)?.ToString();
            return string.IsNullOrWhiteSpace(label) ? null : label;
        }
        catch { return null; }
    }

    /// <summary>Converts a registry event key name to a readable label (e.g. "SystemAsterisk" → "System Asterisk").</summary>
    private static string FormatEventName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var sb = new System.Text.StringBuilder();
        sb.Append(char.ToUpperInvariant(name[0]));
        for (int i = 1; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                sb.Append(' ');
            sb.Append(name[i]);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Invalidates the cached sound list so the next call to GetWindowsSounds re-enumerates.
    /// </summary>
    public static void InvalidateCache() => _cachedSounds = null;

    public static void PlaySound(string appName, AppSettings settings)
    {
        if (!settings.SoundEnabled) return;

        var wavPath = settings.DefaultSound;
        if (!string.IsNullOrWhiteSpace(appName) && settings.PerAppSounds.TryGetValue(appName, out var appSound))
            wavPath = appSound;
        else if (!string.IsNullOrWhiteSpace(appName))
        {
            var profileSound = settings.AppProfiles
                .FirstOrDefault(profile => string.Equals(profile.AppName, appName, StringComparison.OrdinalIgnoreCase))
                ?.Sound;
            if (!string.IsNullOrWhiteSpace(profileSound) && !string.Equals(profileSound, "Default", StringComparison.OrdinalIgnoreCase))
                wavPath = profileSound;
        }

        PlayWav(wavPath);
    }

    /// <summary>Plays a WAV file by path. "None" or empty string = no sound.</summary>
    public static void PlayWav(string? wavPath)
    {
        if (string.IsNullOrWhiteSpace(wavPath) || wavPath == "None") return;

        var expanded = Environment.ExpandEnvironmentVariables(wavPath);
        try
        {
            if (File.Exists(expanded))
            {
                using var player = new SoundPlayer(expanded);
                player.Play();
            }
        }
        catch { /* Ignore playback failures */ }
    }

    public static void EnsureSoundsDirExists()
    {
        try { Directory.CreateDirectory(CustomSoundsDir); } catch { }
    }

    public static string GetCustomSoundsDir() => CustomSoundsDir;
}
