using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using NotificationsPro.Models;

namespace NotificationsPro.Services;

public class SettingsManager
{
    private static readonly string DefaultSettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "NotificationsPro");

    private readonly string _settingsDir;
    private readonly string _settingsPath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AppSettings Settings { get; private set; } = new();

    public event Action? SettingsChanged;

    public SettingsManager() : this(DefaultSettingsDir) { }

    /// <summary>
    /// Constructor accepting a custom settings directory (used for testing).
    /// </summary>
    public SettingsManager(string settingsDir)
    {
        _settingsDir = settingsDir;
        _settingsPath = Path.Combine(settingsDir, "settings.json");
    }

    public void Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (loaded != null)
                    Settings = loaded;
            }
        }
        catch
        {
            // If settings file is corrupted, use defaults silently.
            // Never log notification content — settings file never contains any.
            Settings = new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(_settingsDir);
            var json = JsonSerializer.Serialize(Settings, JsonOptions);
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Fail silently — settings are UI preferences only.
        }
    }

    public void Apply(AppSettings updated)
    {
        Settings = updated;
        Save();
        SettingsChanged?.Invoke();
    }

    public void ResetToDefaults()
    {
        Settings = new AppSettings();
        Save();
        SettingsChanged?.Invoke();
    }
}
