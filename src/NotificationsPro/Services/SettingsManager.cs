using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using NotificationsPro.Helpers;
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
                {
                    NormalizeLoadedSettings(loaded);
                    Settings = loaded;
                }
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
        NormalizeLoadedSettings(updated);
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

    private static void NormalizeLoadedSettings(AppSettings settings)
    {
        settings.MutedApps ??= new List<string>();
        settings.HighlightKeywords ??= new List<string>();
        settings.PerKeywordColors ??= new Dictionary<string, string>();
        settings.MuteKeywords ??= new List<string>();
        settings.HighlightKeywordRegexFlags ??= new Dictionary<string, bool>();
        settings.MuteKeywordRegexFlags ??= new Dictionary<string, bool>();
        settings.HighlightRules ??= new List<HighlightRuleDefinition>();
        settings.MuteRules ??= new List<MuteRuleDefinition>();
        settings.NarrationRules ??= new List<NarrationRuleDefinition>();
        settings.SpokenMutedApps ??= new List<string>();
        settings.PerAppIcons ??= new Dictionary<string, string>();
        settings.PerAppSounds ??= new Dictionary<string, string>();
        settings.PerAppBackgroundImages ??= new Dictionary<string, string>();
        settings.PresentationApps ??= new List<string>();
        settings.CardBackgroundImageOpacity = double.IsNaN(settings.CardBackgroundImageOpacity)
            ? 0.45
            : Math.Clamp(settings.CardBackgroundImageOpacity, 0.0, 1.0);
        settings.CardBackgroundImageHueDegrees = double.IsNaN(settings.CardBackgroundImageHueDegrees)
            ? 0.0
            : Math.Clamp(settings.CardBackgroundImageHueDegrees, -180, 180);
        settings.CardBackgroundImageBrightness = double.IsNaN(settings.CardBackgroundImageBrightness)
            ? 1.0
            : Math.Clamp(settings.CardBackgroundImageBrightness, 0.2, 2.0);
        settings.CardBackgroundImageFitMode = CardBackgroundImageFitModeHelper.Normalize(settings.CardBackgroundImageFitMode);
        settings.CardBackgroundImagePlacement = CardBackgroundImagePlacementHelper.Normalize(settings.CardBackgroundImagePlacement);
        settings.FullscreenOverlayImageFitMode = CardBackgroundImageFitModeHelper.Normalize(settings.FullscreenOverlayImageFitMode);
    }
}
