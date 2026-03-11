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
                    MigrateLegacySettings(loaded);
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

    private static void MigrateLegacySettings(AppSettings settings)
    {
        if (settings.HighlightRules.Count == 0 && settings.HighlightKeywords.Count > 0)
        {
            foreach (var keyword in settings.HighlightKeywords)
            {
                settings.HighlightRules.Add(new HighlightRuleDefinition
                {
                    Keyword = keyword,
                    Color = settings.PerKeywordColors.TryGetValue(keyword, out var color)
                        ? color
                        : settings.HighlightColor,
                    IsRegex = settings.HighlightKeywordRegexFlags.TryGetValue(keyword, out var isRegex) && isRegex,
                    Scope = NotificationMatchScopeHelper.TitleAndBody
                });
            }
        }

        if (settings.MuteRules.Count == 0 && settings.MuteKeywords.Count > 0)
        {
            foreach (var keyword in settings.MuteKeywords)
            {
                settings.MuteRules.Add(new MuteRuleDefinition
                {
                    Keyword = keyword,
                    IsRegex = settings.MuteKeywordRegexFlags.TryGetValue(keyword, out var isRegex) && isRegex,
                    Scope = NotificationMatchScopeHelper.TitleAndBody
                });
            }
        }

        if (settings.AppProfiles.Count == 0)
        {
            var appNames = settings.PerAppIcons.Keys
                .Concat(settings.PerAppSounds.Keys)
                .Concat(settings.SpokenMutedApps)
                .Where(app => !string.IsNullOrWhiteSpace(app))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(app => app, StringComparer.OrdinalIgnoreCase);

            foreach (var appName in appNames)
            {
                settings.PerAppSounds.TryGetValue(appName, out var sound);
                settings.PerAppIcons.TryGetValue(appName, out var icon);
                settings.AppProfiles.Add(new AppProfile
                {
                    AppName = appName,
                    IsReadAloudEnabled = !settings.SpokenMutedApps.Contains(appName, StringComparer.OrdinalIgnoreCase),
                    Sound = sound ?? "Default",
                    Icon = icon ?? "Default",
                    OverlayLane = OverlayLaneHelper.Main
                });
            }
        }

        foreach (var profile in settings.AppProfiles)
        {
            profile.OverlayLane = OverlayLaneHelper.Normalize(profile.OverlayLane);
            profile.Sound = string.IsNullOrWhiteSpace(profile.Sound) ? "Default" : profile.Sound;
            profile.Icon = string.IsNullOrWhiteSpace(profile.Icon) ? "Default" : profile.Icon;
            profile.BackgroundImageOpacity = Math.Clamp(profile.BackgroundImageOpacity, 0.0, 1.0);
            profile.BackgroundImageBrightness = Math.Clamp(profile.BackgroundImageBrightness, 0.2, 2.0);
        }

        foreach (var rule in settings.HighlightRules)
        {
            rule.Scope = NotificationMatchScopeHelper.Normalize(rule.Scope);
            rule.AppFilter ??= string.Empty;
        }

        foreach (var rule in settings.MuteRules)
        {
            rule.Scope = NotificationMatchScopeHelper.Normalize(rule.Scope);
            rule.AppFilter ??= string.Empty;
        }

        foreach (var rule in settings.NarrationRules)
        {
            rule.Scope = NotificationMatchScopeHelper.Normalize(rule.Scope);
            rule.AppFilter ??= string.Empty;
            rule.Action = NarrationRuleActionHelper.Normalize(rule.Action);
            rule.ReadMode = NarrationRuleReadModeHelper.Normalize(rule.ReadMode);
        }

        settings.SecondaryOverlayPositionPreset = SecondaryOverlayPositionHelper.Normalize(settings.SecondaryOverlayPositionPreset);
        settings.SecondaryOverlayWidth = settings.SecondaryOverlayWidth <= 0 ? 340 : settings.SecondaryOverlayWidth;
        settings.SecondaryOverlayMaxHeight = settings.SecondaryOverlayMaxHeight <= 0 ? 480 : settings.SecondaryOverlayMaxHeight;
    }
}
