using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using NotificationsPro.Helpers;
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
        if (!settings.SettingsSchemaVersion.HasValue || settings.SettingsSchemaVersion.Value < SettingsManager.CurrentSettingsSchemaVersion)
            settings.SettingsSchemaVersion = SettingsManager.CurrentSettingsSchemaVersion;

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
        var hasCardBackgroundMode = JsonPropertyExists(json, nameof(AppSettings.CardBackgroundMode));
        var hasNotificationAnimationStyle = JsonPropertyExists(json, nameof(AppSettings.NotificationAnimationStyle));
        var imported = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

        if (imported != null)
            SanitizeImportedSettings(imported, hasCardBackgroundMode, hasNotificationAnimationStyle);

        return imported;
    }

    /// <summary>Clamp imported settings to valid ranges to prevent corrupt/malicious values.</summary>
    private static void SanitizeImportedSettings(AppSettings s, bool hasCardBackgroundMode, bool hasNotificationAnimationStyle)
    {
        s.MaxVisibleNotifications = Math.Clamp(s.MaxVisibleNotifications, 1, AppSettings.MaxVisibleNotificationsUpperBound);
        s.NotificationDuration = Math.Clamp(s.NotificationDuration, 1, 300);
        s.NotificationAnimationStyle = NotificationAnimationStyleHelper.Normalize(
            hasNotificationAnimationStyle
                ? s.NotificationAnimationStyle
                : NotificationAnimationStyleHelper.FromLegacyFadeOnly(s.FadeOnlyAnimation));
        s.FadeOnlyAnimation = NotificationAnimationStyleHelper.IsLegacyFadeOnly(s.NotificationAnimationStyle);
        s.AnimationEasing = AnimationEasingHelper.Normalize(s.AnimationEasing);
        s.HighlightOverlayOpacity = double.IsNaN(s.HighlightOverlayOpacity) ? 0.25 : Math.Clamp(s.HighlightOverlayOpacity, 0.05, 0.80);
        s.HighlightAnimation = HighlightAnimationHelper.Normalize(s.HighlightAnimation);
        s.HighlightBorderMode = HighlightBorderModeHelper.Normalize(s.HighlightBorderMode);
        s.HighlightBorderThickness = double.IsNaN(s.HighlightBorderThickness) ? 1 : Math.Clamp(s.HighlightBorderThickness, 0.5, 8.0);
        s.OverlayWidth = double.IsNaN(s.OverlayWidth) ? 340 : Math.Clamp(s.OverlayWidth, 220, 7680);
        s.OverlayMaxHeight = double.IsNaN(s.OverlayMaxHeight) ? 800 : Math.Clamp(s.OverlayMaxHeight, 200, 4320);
        s.BurstLimitCount = Math.Clamp(s.BurstLimitCount, 1, 100);
        s.BurstLimitWindowSeconds = Math.Clamp(s.BurstLimitWindowSeconds, 1, 60);
        s.FontSize = double.IsNaN(s.FontSize) ? 14 : Math.Clamp(s.FontSize, 6, 72);
        s.TitleFontSize = double.IsNaN(s.TitleFontSize) ? 16 : Math.Clamp(s.TitleFontSize, 6, 72);
        s.AppNameFontSize = double.IsNaN(s.AppNameFontSize) ? 14 : Math.Clamp(s.AppNameFontSize, 6, 72);
        s.OverlayScrollbarTrackColor = string.IsNullOrWhiteSpace(s.OverlayScrollbarTrackColor) ? "#141414" : s.OverlayScrollbarTrackColor;
        s.OverlayScrollbarThumbColor = string.IsNullOrWhiteSpace(s.OverlayScrollbarThumbColor) ? "#4F4F4F" : s.OverlayScrollbarThumbColor;
        s.OverlayScrollbarThumbHoverColor = string.IsNullOrWhiteSpace(s.OverlayScrollbarThumbHoverColor) ? "#0078D4" : s.OverlayScrollbarThumbHoverColor;
        s.OverlayScrollbarPadding = double.IsNaN(s.OverlayScrollbarPadding) ? 1.5 : Math.Clamp(s.OverlayScrollbarPadding, 0.0, 6.0);
        s.OverlayScrollbarContentGap = double.IsNaN(s.OverlayScrollbarContentGap) ? 10.0 : Math.Clamp(s.OverlayScrollbarContentGap, 0.0, 24.0);
        s.OverlayScrollbarCornerRadius = double.IsNaN(s.OverlayScrollbarCornerRadius) ? 6.0 : Math.Clamp(s.OverlayScrollbarCornerRadius, 0.0, 12.0);
        s.ReadNotificationsAloudTriggerMode = NarrationTriggerModeHelper.Normalize(s.ReadNotificationsAloudTriggerMode);
        s.CardBackgroundMode = CardBackgroundModeHelper.Normalize(
            !hasCardBackgroundMode && !string.IsNullOrWhiteSpace(s.CardBackgroundImagePath)
                ? CardBackgroundModeHelper.Image
                : s.CardBackgroundMode);
        s.CardBackgroundImageOpacity = double.IsNaN(s.CardBackgroundImageOpacity) ? 0.45 : Math.Clamp(s.CardBackgroundImageOpacity, 0.0, 1.0);
        s.CardBackgroundImageHueDegrees = double.IsNaN(s.CardBackgroundImageHueDegrees) ? 0.0 : Math.Clamp(s.CardBackgroundImageHueDegrees, -180, 180);
        s.CardBackgroundImageBrightness = double.IsNaN(s.CardBackgroundImageBrightness) ? 1.0 : Math.Clamp(s.CardBackgroundImageBrightness, 0.2, 2.0);
        s.CardBackgroundImageSaturation = double.IsNaN(s.CardBackgroundImageSaturation) ? 1.0 : Math.Clamp(s.CardBackgroundImageSaturation, 0.0, 2.0);
        s.CardBackgroundImageContrast = double.IsNaN(s.CardBackgroundImageContrast) ? 1.0 : Math.Clamp(s.CardBackgroundImageContrast, 0.2, 2.0);
        s.CardBackgroundImageFitMode = CardBackgroundImageFitModeHelper.Normalize(s.CardBackgroundImageFitMode);
        s.CardBackgroundImagePlacement = CardBackgroundImagePlacementHelper.Normalize(s.CardBackgroundImagePlacement);
        s.CardBackgroundImageVerticalFocus = ImageVerticalFocusHelper.Normalize(s.CardBackgroundImageVerticalFocus);
        s.FullscreenOverlayImageHueDegrees = double.IsNaN(s.FullscreenOverlayImageHueDegrees) ? 0.0 : Math.Clamp(s.FullscreenOverlayImageHueDegrees, -180, 180);
        s.FullscreenOverlayImageBrightness = double.IsNaN(s.FullscreenOverlayImageBrightness) ? 1.0 : Math.Clamp(s.FullscreenOverlayImageBrightness, 0.2, 2.0);
        s.FullscreenOverlayImageSaturation = double.IsNaN(s.FullscreenOverlayImageSaturation) ? 1.0 : Math.Clamp(s.FullscreenOverlayImageSaturation, 0.0, 2.0);
        s.FullscreenOverlayImageContrast = double.IsNaN(s.FullscreenOverlayImageContrast) ? 1.0 : Math.Clamp(s.FullscreenOverlayImageContrast, 0.2, 2.0);
        s.FullscreenOverlayImageFitMode = CardBackgroundImageFitModeHelper.Normalize(s.FullscreenOverlayImageFitMode);
        s.FullscreenOverlayImageVerticalFocus = ImageVerticalFocusHelper.Normalize(s.FullscreenOverlayImageVerticalFocus);
        s.HighlightRules ??= new List<HighlightRuleDefinition>();
        s.MuteRules ??= new List<MuteRuleDefinition>();
        s.NarrationRules ??= new List<NarrationRuleDefinition>();
        s.PerAppBackgroundImages ??= new Dictionary<string, string>();
        foreach (var rule in s.HighlightRules)
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
        s.SettingsThemeMode = SettingsThemeService.ResolveThemeModeForLoadedSettings(s);
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

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return sanitized.Trim();
    }
}
