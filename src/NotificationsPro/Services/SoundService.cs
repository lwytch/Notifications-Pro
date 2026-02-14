using System.IO;
using System.Media;
using NotificationsPro.Models;

namespace NotificationsPro.Services;

/// <summary>
/// Plays notification sounds — system sounds or custom WAV files.
/// Per-app sounds override the default sound.
/// </summary>
public static class SoundService
{
    private static readonly string CustomSoundsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "NotificationsPro", "sounds");

    public static readonly string[] SystemSoundNames =
    {
        "None", "Asterisk", "Beep", "Exclamation", "Hand", "Question"
    };

    public static void PlaySound(string appName, AppSettings settings)
    {
        if (!settings.SoundEnabled) return;

        // Check per-app override first
        var soundName = settings.DefaultSound;
        if (!string.IsNullOrWhiteSpace(appName) && settings.PerAppSounds.TryGetValue(appName, out var appSound))
            soundName = appSound;

        if (string.IsNullOrWhiteSpace(soundName) || soundName == "None") return;

        // System sounds
        switch (soundName)
        {
            case "Asterisk": SystemSounds.Asterisk.Play(); return;
            case "Beep": SystemSounds.Beep.Play(); return;
            case "Exclamation": SystemSounds.Exclamation.Play(); return;
            case "Hand": SystemSounds.Hand.Play(); return;
            case "Question": SystemSounds.Question.Play(); return;
        }

        // Custom WAV file (path stored in settings)
        try
        {
            if (File.Exists(soundName))
            {
                using var player = new SoundPlayer(soundName);
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
