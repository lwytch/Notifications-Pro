using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using NotificationsPro.Helpers;
using NotificationsPro.Models;
using WinForms = System.Windows.Forms;

namespace NotificationsPro.Services;

public class SettingsManager
{
    public const int CurrentSettingsSchemaVersion = 4;

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
    public bool HadExistingSettingsFile { get; private set; }

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
        HadExistingSettingsFile = File.Exists(_settingsPath);

        try
        {
            if (HadExistingSettingsFile)
            {
                var json = File.ReadAllText(_settingsPath);
                var hasCardBackgroundMode = JsonPropertyExists(json, nameof(AppSettings.CardBackgroundMode));
                var loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (loaded != null)
                {
                    NormalizeLoadedSettings(loaded, hasCardBackgroundMode);
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

        var primaryScreenHeight = WinForms.Screen.PrimaryScreen?.WorkingArea.Height;
        if (StartupSettingsMigrationHelper.Apply(Settings, primaryScreenHeight, HadExistingSettingsFile))
            Save();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(_settingsDir);
            if (!Settings.SettingsSchemaVersion.HasValue || Settings.SettingsSchemaVersion.Value < CurrentSettingsSchemaVersion)
                Settings.SettingsSchemaVersion = CurrentSettingsSchemaVersion;
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
        NormalizeLoadedSettings(updated, hasCardBackgroundMode: true);
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

    private static void NormalizeLoadedSettings(AppSettings settings, bool hasCardBackgroundMode)
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
        settings.ReadNotificationsAloudTriggerMode = NarrationTriggerModeHelper.Normalize(settings.ReadNotificationsAloudTriggerMode);
        settings.AnimationEasing = AnimationEasingHelper.Normalize(settings.AnimationEasing);
        settings.HighlightOverlayOpacity = double.IsNaN(settings.HighlightOverlayOpacity)
            ? 0.25
            : Math.Clamp(settings.HighlightOverlayOpacity, 0.05, 0.80);
        settings.HighlightAnimation = HighlightAnimationHelper.Normalize(settings.HighlightAnimation);
        settings.HighlightBorderMode = HighlightBorderModeHelper.Normalize(settings.HighlightBorderMode);
        settings.HighlightBorderThickness = double.IsNaN(settings.HighlightBorderThickness)
            ? 1
            : Math.Clamp(settings.HighlightBorderThickness, 0.5, 8.0);
        settings.MaxVisibleNotifications = Math.Clamp(settings.MaxVisibleNotifications, 1, AppSettings.MaxVisibleNotificationsUpperBound);
        settings.OverlayScrollbarTrackColor = string.IsNullOrWhiteSpace(settings.OverlayScrollbarTrackColor)
            ? "#141414"
            : settings.OverlayScrollbarTrackColor;
        settings.OverlayScrollbarThumbColor = string.IsNullOrWhiteSpace(settings.OverlayScrollbarThumbColor)
            ? "#4F4F4F"
            : settings.OverlayScrollbarThumbColor;
        settings.OverlayScrollbarThumbHoverColor = string.IsNullOrWhiteSpace(settings.OverlayScrollbarThumbHoverColor)
            ? "#0078D4"
            : settings.OverlayScrollbarThumbHoverColor;
        settings.OverlayScrollbarPadding = double.IsNaN(settings.OverlayScrollbarPadding)
            ? 1.5
            : Math.Clamp(settings.OverlayScrollbarPadding, 0.0, 6.0);
        settings.OverlayScrollbarContentGap = double.IsNaN(settings.OverlayScrollbarContentGap)
            ? 10.0
            : Math.Clamp(settings.OverlayScrollbarContentGap, 0.0, 24.0);
        settings.OverlayScrollbarCornerRadius = double.IsNaN(settings.OverlayScrollbarCornerRadius)
            ? 6.0
            : Math.Clamp(settings.OverlayScrollbarCornerRadius, 0.0, 12.0);
        settings.CardBackgroundMode = CardBackgroundModeHelper.Normalize(
            !hasCardBackgroundMode && !string.IsNullOrWhiteSpace(settings.CardBackgroundImagePath)
                ? CardBackgroundModeHelper.Image
                : settings.CardBackgroundMode);
        settings.CardBackgroundImageOpacity = double.IsNaN(settings.CardBackgroundImageOpacity)
            ? 0.45
            : Math.Clamp(settings.CardBackgroundImageOpacity, 0.0, 1.0);
        settings.CardBackgroundImageHueDegrees = double.IsNaN(settings.CardBackgroundImageHueDegrees)
            ? 0.0
            : Math.Clamp(settings.CardBackgroundImageHueDegrees, -180, 180);
        settings.CardBackgroundImageBrightness = double.IsNaN(settings.CardBackgroundImageBrightness)
            ? 1.0
            : Math.Clamp(settings.CardBackgroundImageBrightness, 0.2, 2.0);
        settings.CardBackgroundImageSaturation = double.IsNaN(settings.CardBackgroundImageSaturation)
            ? 1.0
            : Math.Clamp(settings.CardBackgroundImageSaturation, 0.0, 2.0);
        settings.CardBackgroundImageContrast = double.IsNaN(settings.CardBackgroundImageContrast)
            ? 1.0
            : Math.Clamp(settings.CardBackgroundImageContrast, 0.2, 2.0);
        settings.CardBackgroundImageFitMode = CardBackgroundImageFitModeHelper.Normalize(settings.CardBackgroundImageFitMode);
        settings.CardBackgroundImagePlacement = CardBackgroundImagePlacementHelper.Normalize(settings.CardBackgroundImagePlacement);
        settings.CardBackgroundImageVerticalFocus = ImageVerticalFocusHelper.Normalize(settings.CardBackgroundImageVerticalFocus);
        settings.FullscreenOverlayImageHueDegrees = double.IsNaN(settings.FullscreenOverlayImageHueDegrees)
            ? 0.0
            : Math.Clamp(settings.FullscreenOverlayImageHueDegrees, -180, 180);
        settings.FullscreenOverlayImageBrightness = double.IsNaN(settings.FullscreenOverlayImageBrightness)
            ? 1.0
            : Math.Clamp(settings.FullscreenOverlayImageBrightness, 0.2, 2.0);
        settings.FullscreenOverlayImageSaturation = double.IsNaN(settings.FullscreenOverlayImageSaturation)
            ? 1.0
            : Math.Clamp(settings.FullscreenOverlayImageSaturation, 0.0, 2.0);
        settings.FullscreenOverlayImageContrast = double.IsNaN(settings.FullscreenOverlayImageContrast)
            ? 1.0
            : Math.Clamp(settings.FullscreenOverlayImageContrast, 0.2, 2.0);
        settings.FullscreenOverlayImageFitMode = CardBackgroundImageFitModeHelper.Normalize(settings.FullscreenOverlayImageFitMode);
        settings.FullscreenOverlayImageVerticalFocus = ImageVerticalFocusHelper.Normalize(settings.FullscreenOverlayImageVerticalFocus);
        foreach (var rule in settings.HighlightRules)
        {
            rule.Scope = NotificationMatchScopeHelper.Normalize(rule.Scope);
            rule.AppFilter = rule.AppFilter?.Trim() ?? string.Empty;
            rule.Animation = string.IsNullOrWhiteSpace(rule.Animation)
                ? string.Empty
                : HighlightAnimationHelper.Normalize(rule.Animation);
            rule.BorderMode = string.IsNullOrWhiteSpace(rule.BorderMode)
                ? string.Empty
                : HighlightBorderModeHelper.Normalize(rule.BorderMode);
            rule.OverlayOpacity = rule.OverlayOpacity.HasValue
                ? Math.Clamp(rule.OverlayOpacity.Value, 0.05, 0.80)
                : null;
            rule.BorderThickness = rule.BorderThickness.HasValue
                ? Math.Clamp(rule.BorderThickness.Value, 0.5, 8.0)
                : null;
        }
        settings.SettingsThemeMode = SettingsThemeService.ResolveThemeModeForLoadedSettings(settings);
    }

    private static bool JsonPropertyExists(string json, string propertyName)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }
}
