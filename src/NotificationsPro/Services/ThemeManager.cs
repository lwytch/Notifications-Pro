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

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return sanitized.Trim();
    }
}
