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
        }

        settings.OverlayBackgroundImageOpacity = Math.Clamp(settings.OverlayBackgroundImageOpacity, 0.0, 1.0);
        settings.OverlayBackgroundImageBrightness = Math.Clamp(settings.OverlayBackgroundImageBrightness, 0.2, 2.0);

        MigrateOverlayLanes(settings);

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

    private static void MigrateOverlayLanes(AppSettings settings)
    {
        var needsSecondaryLane = settings.SecondaryOverlayEnabled
            || settings.AppProfiles.Any(profile => string.Equals(
                OverlayLaneHelper.Normalize(profile.OverlayLane),
                OverlayLaneHelper.Secondary,
                StringComparison.OrdinalIgnoreCase));

        if (needsSecondaryLane && !settings.OverlayLanes.Any(lane =>
                string.Equals(OverlayLaneHelper.Normalize(lane.Id), OverlayLaneHelper.Secondary, StringComparison.OrdinalIgnoreCase)))
        {
            settings.OverlayLanes.Add(new OverlayLaneDefinition
            {
                Id = OverlayLaneHelper.Secondary,
                Name = OverlayLaneHelper.SecondaryDisplayName,
                IsEnabled = settings.SecondaryOverlayEnabled,
                MonitorIndex = Math.Max(0, settings.SecondaryOverlayMonitorIndex),
                PositionPreset = SecondaryOverlayPositionHelper.Normalize(settings.SecondaryOverlayPositionPreset),
                Left = settings.SecondaryOverlayLeft,
                Top = settings.SecondaryOverlayTop,
                Width = settings.SecondaryOverlayWidth <= 0 ? 340 : settings.SecondaryOverlayWidth,
                MaxHeight = settings.SecondaryOverlayMaxHeight <= 0 ? 480 : settings.SecondaryOverlayMaxHeight
            });
        }

        foreach (var lane in settings.OverlayLanes)
        {
            lane.Id = OverlayLaneHelper.Normalize(lane.Id);
            lane.Name = string.IsNullOrWhiteSpace(lane.Name)
                ? OverlayLaneHelper.GetDisplayName(settings.OverlayLanes, lane.Id)
                : lane.Name.Trim();
            lane.MonitorIndex = Math.Max(0, lane.MonitorIndex);
            lane.PositionPreset = SecondaryOverlayPositionHelper.Normalize(lane.PositionPreset);
            lane.Width = lane.Width <= 0 ? 340 : lane.Width;
            lane.MaxHeight = lane.MaxHeight <= 0 ? 480 : lane.MaxHeight;
            HydrateLaneProfileFromMain(settings, lane);
            lane.BackgroundImageOpacity = Math.Clamp(lane.BackgroundImageOpacity, 0.0, 1.0);
            lane.BackgroundImageBrightness = Math.Clamp(lane.BackgroundImageBrightness, 0.2, 2.0);
        }

        settings.OverlayLanes = settings.OverlayLanes
            .Where(lane => !string.Equals(lane.Id, OverlayLaneHelper.Main, StringComparison.OrdinalIgnoreCase))
            .GroupBy(lane => lane.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .OrderBy(lane => lane.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var profile in settings.AppProfiles)
            profile.OverlayLane = OverlayLaneHelper.NormalizeOrMain(profile.OverlayLane, settings.OverlayLanes);
    }

    private static void HydrateLaneProfileFromMain(AppSettings settings, OverlayLaneDefinition lane)
    {
        var needsLegacyHydration =
            string.IsNullOrWhiteSpace(lane.FontFamily)
            && lane.FontSize <= 0
            && lane.AppNameFontSize <= 0
            && lane.TitleFontSize <= 0
            && string.IsNullOrWhiteSpace(lane.TimestampDisplayMode)
            && string.IsNullOrWhiteSpace(lane.DensityPreset);

        if (needsLegacyHydration)
        {
            lane.FontFamily = settings.FontFamily;
            lane.FontSize = settings.FontSize;
            lane.FontWeight = settings.FontWeight;
            lane.AppNameFontSize = settings.AppNameFontSize;
            lane.AppNameFontWeight = settings.AppNameFontWeight;
            lane.TitleFontSize = settings.TitleFontSize;
            lane.TitleFontWeight = settings.TitleFontWeight;
            lane.LineSpacing = settings.LineSpacing;
            lane.TextAlignment = settings.TextAlignment;
            lane.BackgroundOpacity = settings.BackgroundOpacity;
            lane.CornerRadius = settings.CornerRadius;
            lane.Padding = settings.Padding;
            lane.CardGap = settings.CardGap;
            lane.OuterMargin = settings.OuterMargin;
            lane.ShowAccent = settings.ShowAccent;
            lane.AccentThickness = settings.AccentThickness;
            lane.ShowBorder = settings.ShowBorder;
            lane.BorderColor = settings.BorderColor;
            lane.BorderThickness = settings.BorderThickness;
            lane.ShowAppName = settings.ShowAppName;
            lane.ShowNotificationTitle = settings.ShowNotificationTitle;
            lane.ShowNotificationBody = settings.ShowNotificationBody;
            lane.LimitTextLines = settings.LimitTextLines;
            lane.MaxAppNameLines = settings.MaxAppNameLines;
            lane.MaxTitleLines = settings.MaxTitleLines;
            lane.MaxBodyLines = settings.MaxBodyLines;
            lane.SingleLineMode = settings.SingleLineMode;
            lane.SingleLineWrapText = settings.SingleLineWrapText;
            lane.SingleLineMaxLines = settings.SingleLineMaxLines;
            lane.SingleLineAutoFullWidth = settings.SingleLineAutoFullWidth;
            lane.ShowTimestamp = settings.ShowTimestamp;
            lane.TimestampFontSize = settings.TimestampFontSize;
            lane.TimestampDisplayMode = settings.TimestampDisplayMode;
            lane.TimestampFontWeight = settings.TimestampFontWeight;
            lane.TimestampColor = settings.TimestampColor;
            lane.DensityPreset = settings.DensityPreset;
        }

        lane.FontFamily = string.IsNullOrWhiteSpace(lane.FontFamily) ? settings.FontFamily : lane.FontFamily.Trim();
        lane.FontSize = lane.FontSize <= 0 ? settings.FontSize : lane.FontSize;
        lane.FontWeight = string.IsNullOrWhiteSpace(lane.FontWeight) ? settings.FontWeight : lane.FontWeight.Trim();
        lane.AppNameFontSize = lane.AppNameFontSize <= 0 ? settings.AppNameFontSize : lane.AppNameFontSize;
        lane.AppNameFontWeight = string.IsNullOrWhiteSpace(lane.AppNameFontWeight) ? settings.AppNameFontWeight : lane.AppNameFontWeight.Trim();
        lane.TitleFontSize = lane.TitleFontSize <= 0 ? settings.TitleFontSize : lane.TitleFontSize;
        lane.TitleFontWeight = string.IsNullOrWhiteSpace(lane.TitleFontWeight) ? settings.TitleFontWeight : lane.TitleFontWeight.Trim();
        lane.LineSpacing = lane.LineSpacing <= 0 ? settings.LineSpacing : lane.LineSpacing;
        lane.TextAlignment = string.IsNullOrWhiteSpace(lane.TextAlignment) ? settings.TextAlignment : lane.TextAlignment.Trim();
        lane.BackgroundOpacity = lane.BackgroundOpacity <= 0 ? settings.BackgroundOpacity : Math.Clamp(lane.BackgroundOpacity, 0.0, 1.0);
        lane.CornerRadius = lane.CornerRadius < 0 ? settings.CornerRadius : lane.CornerRadius;
        lane.Padding = lane.Padding <= 0 ? settings.Padding : lane.Padding;
        lane.CardGap = lane.CardGap < 0 ? settings.CardGap : lane.CardGap;
        lane.OuterMargin = lane.OuterMargin < 0 ? settings.OuterMargin : lane.OuterMargin;
        lane.AccentThickness = lane.AccentThickness <= 0 ? settings.AccentThickness : lane.AccentThickness;
        lane.BorderColor = string.IsNullOrWhiteSpace(lane.BorderColor) ? settings.BorderColor : lane.BorderColor.Trim();
        lane.BorderThickness = lane.BorderThickness <= 0 ? settings.BorderThickness : lane.BorderThickness;
        lane.MaxAppNameLines = lane.MaxAppNameLines <= 0 ? settings.MaxAppNameLines : lane.MaxAppNameLines;
        lane.MaxTitleLines = lane.MaxTitleLines <= 0 ? settings.MaxTitleLines : lane.MaxTitleLines;
        lane.MaxBodyLines = lane.MaxBodyLines <= 0 ? settings.MaxBodyLines : lane.MaxBodyLines;
        lane.SingleLineMaxLines = lane.SingleLineMaxLines <= 0 ? settings.SingleLineMaxLines : lane.SingleLineMaxLines;
        lane.TimestampFontSize = lane.TimestampFontSize <= 0 ? settings.TimestampFontSize : lane.TimestampFontSize;
        lane.TimestampDisplayMode = string.IsNullOrWhiteSpace(lane.TimestampDisplayMode) ? settings.TimestampDisplayMode : lane.TimestampDisplayMode.Trim();
        lane.TimestampFontWeight = string.IsNullOrWhiteSpace(lane.TimestampFontWeight) ? settings.TimestampFontWeight : lane.TimestampFontWeight.Trim();
        lane.TimestampColor = string.IsNullOrWhiteSpace(lane.TimestampColor) ? settings.TimestampColor : lane.TimestampColor.Trim();
        lane.DensityPreset = string.IsNullOrWhiteSpace(lane.DensityPreset) ? settings.DensityPreset : lane.DensityPreset.Trim();

        if (!lane.ShowAppName && !lane.ShowNotificationTitle && !lane.ShowNotificationBody)
            lane.ShowNotificationBody = true;
    }
}
