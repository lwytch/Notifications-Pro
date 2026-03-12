using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using NotificationsPro.Models;

namespace NotificationsPro.Services;

/// <summary>
/// Manages custom user themes stored in %AppData%\NotificationsPro\themes\.
/// Each theme is a separate JSON file. Never stores notification content.
/// </summary>
public class ThemeManager
{
    private readonly string _themesDir;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ThemeManager() : this(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "NotificationsPro", "themes"))
    { }

    public ThemeManager(string themesDir)
    {
        _themesDir = themesDir;
    }

    public List<ThemePreset> LoadCustomThemes()
    {
        var themes = new List<ThemePreset>();
        if (!Directory.Exists(_themesDir))
            return themes;

        foreach (var file in Directory.GetFiles(_themesDir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var theme = JsonSerializer.Deserialize<ThemePreset>(json, JsonOptions);
                if (theme != null && !string.IsNullOrWhiteSpace(theme.Name))
                    themes.Add(theme);
            }
            catch
            {
                // Skip corrupted theme files
            }
        }

        return themes.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public void SaveCustomTheme(ThemePreset theme)
    {
        if (string.IsNullOrWhiteSpace(theme.Name))
            return;

        Directory.CreateDirectory(_themesDir);
        var filename = SanitizeFileName(theme.Name) + ".json";
        var path = Path.Combine(_themesDir, filename);
        var json = JsonSerializer.Serialize(theme, JsonOptions);
        File.WriteAllText(path, json);
    }

    public void DeleteCustomTheme(string themeName)
    {
        if (string.IsNullOrWhiteSpace(themeName) || !Directory.Exists(_themesDir))
            return;

        var filename = SanitizeFileName(themeName) + ".json";
        var path = Path.Combine(_themesDir, filename);
        if (File.Exists(path))
            File.Delete(path);
    }

    public static void ExportSettings(AppSettings settings, string filePath)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    public static AppSettings? ImportSettings(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        // Limit import file size to 1 MB to prevent DoS
        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length > 1_048_576)
            return null;

        var json = File.ReadAllText(filePath);
        var imported = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

        if (imported != null)
            SanitizeImportedSettings(imported);

        return imported;
    }

    /// <summary>Clamp imported settings to valid ranges to prevent corrupt/malicious values.</summary>
    private static void SanitizeImportedSettings(AppSettings s)
    {
        s.MaxVisibleNotifications = Math.Clamp(s.MaxVisibleNotifications, 1, 40);
        s.NotificationDuration = Math.Clamp(s.NotificationDuration, 1, 300);
        s.OverlayWidth = double.IsNaN(s.OverlayWidth) ? 340 : Math.Clamp(s.OverlayWidth, 220, 7680);
        s.OverlayMaxHeight = double.IsNaN(s.OverlayMaxHeight) ? 800 : Math.Clamp(s.OverlayMaxHeight, 200, 4320);
        s.BurstLimitCount = Math.Clamp(s.BurstLimitCount, 1, 100);
        s.BurstLimitWindowSeconds = Math.Clamp(s.BurstLimitWindowSeconds, 1, 60);
        s.FontSize = double.IsNaN(s.FontSize) ? 14 : Math.Clamp(s.FontSize, 6, 72);
        s.TitleFontSize = double.IsNaN(s.TitleFontSize) ? 16 : Math.Clamp(s.TitleFontSize, 6, 72);
        s.AppNameFontSize = double.IsNaN(s.AppNameFontSize) ? 14 : Math.Clamp(s.AppNameFontSize, 6, 72);
        s.CardBackgroundImageOpacity = double.IsNaN(s.CardBackgroundImageOpacity) ? 0.45 : Math.Clamp(s.CardBackgroundImageOpacity, 0.0, 1.0);
        s.CardBackgroundImageHueDegrees = double.IsNaN(s.CardBackgroundImageHueDegrees) ? 0.0 : Math.Clamp(s.CardBackgroundImageHueDegrees, -180, 180);
        s.CardBackgroundImageBrightness = double.IsNaN(s.CardBackgroundImageBrightness) ? 1.0 : Math.Clamp(s.CardBackgroundImageBrightness, 0.2, 2.0);
        s.HighlightRules ??= new List<HighlightRuleDefinition>();
        s.MuteRules ??= new List<MuteRuleDefinition>();
        s.NarrationRules ??= new List<NarrationRuleDefinition>();
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return sanitized.Trim();
    }
}
