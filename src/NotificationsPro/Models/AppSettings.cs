namespace NotificationsPro.Models;

public class AppSettings
{
    // Appearance — Typography
    public string FontFamily { get; set; } = "Segoe UI";
    public double FontSize { get; set; } = 14;
    public string FontWeight { get; set; } = "Normal";
    public double AppNameFontSize { get; set; } = 14;
    public string AppNameFontWeight { get; set; } = "SemiBold";
    public double TitleFontSize { get; set; } = 16;
    public string TitleFontWeight { get; set; } = "SemiBold";
    public double LineSpacing { get; set; } = 1.5;
    public string TextAlignment { get; set; } = "Left";

    // Appearance — Colors
    public string TextColor { get; set; } = "#E6E6E6";
    public string TitleColor { get; set; } = "#FFFFFF";
    public string AppNameColor { get; set; } = "#C8C8C8";
    public string BackgroundColor { get; set; } = "#202020";
    public double BackgroundOpacity { get; set; } = 0.94;
    public string AccentColor { get; set; } = "#0078D4";

    // Appearance — Card Shape
    public double CornerRadius { get; set; } = 20;
    public double Padding { get; set; } = 16;
    public double CardGap { get; set; } = 8;
    public double OuterMargin { get; set; } = 4;
    public bool ShowAccent { get; set; } = true;
    public double AccentThickness { get; set; } = 3;
    public bool ShowBorder { get; set; } = false;
    public string BorderColor { get; set; } = "#3A3A3A";
    public double BorderThickness { get; set; } = 1;

    // Behavior
    public double NotificationDuration { get; set; } = 5;
    public int MaxVisibleNotifications { get; set; } = 20;
    public bool ShowAppName { get; set; } = true;
    public bool ShowNotificationTitle { get; set; } = true;
    public bool ShowNotificationBody { get; set; } = true;
    public bool LimitTextLines { get; set; } = false;
    public int MaxAppNameLines { get; set; } = 2;
    public int MaxTitleLines { get; set; } = 2;
    public int MaxBodyLines { get; set; } = 4;
    public bool SingleLineMode { get; set; } = false;
    public bool SingleLineWrapText { get; set; } = false;
    public int SingleLineMaxLines { get; set; } = 3;
    public bool SingleLineAutoFullWidth { get; set; } = false;
    public bool ReplaceMode { get; set; } = false; // Replace existing notification rather than stacking
    public bool ShowTimestamp { get; set; } = true;
    public double TimestampFontSize { get; set; } = 11;
    public string TimestampDisplayMode { get; set; } = "Relative"; // "Relative", "Time", "DateTime"
    public string TimestampFontWeight { get; set; } = "Normal";
    public string TimestampColor { get; set; } = "#C8C8C8";
    public bool NewestOnTop { get; set; } = true;
    public bool AlwaysOnTop { get; set; } = true;
    public bool ClickThrough { get; set; } = false;
    public bool AnimationsEnabled { get; set; } = true;
    public bool FadeOnlyAnimation { get; set; } = false;
    public string SlideInDirection { get; set; } = "Left";
    public double AnimationDurationMs { get; set; } = 1200;
    public bool NotificationsPaused { get; set; } = false;
    public bool DeduplicationEnabled { get; set; } = true;
    public double DeduplicationWindowSeconds { get; set; } = 2;

    // Filtering
    public List<string> MutedApps { get; set; } = new();
    public List<string> HighlightKeywords { get; set; } = new();
    public Dictionary<string, string> PerKeywordColors { get; set; } = new();
    public List<string> MuteKeywords { get; set; } = new();
    public Dictionary<string, bool> HighlightKeywordRegexFlags { get; set; } = new();
    public Dictionary<string, bool> MuteKeywordRegexFlags { get; set; } = new();
    public string HighlightColor { get; set; } = "#FFD700";

    // Notification icons (M9.5)
    public bool ShowNotificationIcons { get; set; } = false;
    public double IconSize { get; set; } = 24;
    public string DefaultIconPreset { get; set; } = "None";
    public Dictionary<string, string> PerAppIcons { get; set; } = new();

    // Notification sounds (M9.5)
    public bool SoundEnabled { get; set; } = false;
    public string DefaultSound { get; set; } = "None";
    public Dictionary<string, string> PerAppSounds { get; set; } = new();

    // Toast suppression (M9.5)
    public bool SuppressToastPopups { get; set; } = false;

    // Scheduling
    public bool QuietHoursEnabled { get; set; } = false;
    public string QuietHoursStart { get; set; } = "22:00";
    public string QuietHoursEnd { get; set; } = "08:00";

    // Burst limiting
    public bool BurstLimitEnabled { get; set; } = false;
    public int BurstLimitCount { get; set; } = 10;
    public double BurstLimitWindowSeconds { get; set; } = 5;

    // Accessibility — Master toggle
    public bool AccessibilityModeEnabled { get; set; } = false;

    // Accessibility — Timing
    public bool PersistentNotifications { get; set; } = false;
    public bool AutoDurationEnabled { get; set; } = false;
    public double AutoDurationSecondsPerLine { get; set; } = 2.0;
    public double AutoDurationBaseSeconds { get; set; } = 5.0;

    // Accessibility — System integration
    public bool RespectReduceMotion { get; set; } = true;
    public bool RespectHighContrast { get; set; } = true;
    public bool RespectTextScaling { get; set; } = false;

    // Accessibility — Global hotkeys
    public bool GlobalHotkeysEnabled { get; set; } = false;
    public string HotkeyToggleOverlay { get; set; } = "Ctrl+Alt+N";
    public string HotkeyDismissAll { get; set; } = "Ctrl+Alt+D";
    public string HotkeyToggleDnd { get; set; } = "Ctrl+Alt+P";

    // Accessibility — Spoken notifications
    public bool ReadNotificationsAloudEnabled { get; set; } = false;
    public string ReadNotificationsAloudMode { get; set; } = "Body Only";
    public string ReadNotificationsAloudVoiceId { get; set; } = string.Empty;
    public double ReadNotificationsAloudRate { get; set; } = 1.0;
    public double ReadNotificationsAloudVolume { get; set; } = 1.0;

    // Accessibility — Voice Access card labels
    public string VoiceAccessReadMode { get; set; } = "Off";

    // Accessibility — Information density
    public string DensityPreset { get; set; } = "Comfortable";

    // Position & Size (null = not yet positioned, use default)
    public double? OverlayLeft { get; set; }
    public double? OverlayTop { get; set; }
    public double OverlayWidth { get; set; } = 340;
    public double LastManualOverlayWidth { get; set; } = 340;
    public double OverlayMaxHeight { get; set; } = 480;
    public bool AllowManualResize { get; set; } = true;
    public int MonitorIndex { get; set; } = 0;
    public bool SnapToEdges { get; set; } = true;
    public double SnapDistance { get; set; } = 20;

    // Overlay scrollbar (M9.5)
    public bool OverlayScrollbarVisible { get; set; } = true;
    public double OverlayScrollbarWidth { get; set; } = 8;
    public double OverlayScrollbarOpacity { get; set; } = 1.0;

    // Overlay visibility
    public bool OverlayVisible { get; set; } = true;

    // Fullscreen overlay mode (M9.5)
    public bool FullscreenOverlayMode { get; set; } = false;
    public double FullscreenOverlayOpacity { get; set; } = 0.5;
    public string FullscreenOverlayColor { get; set; } = "#000000";

    // Streaming & Presentation (M10)
    public bool ChromaKeyEnabled { get; set; } = false;
    public string ChromaKeyColor { get; set; } = "#00FF00";
    public bool ObsFixedWindowMode { get; set; } = false;
    public double ObsFixedWidth { get; set; } = 400;
    public double ObsFixedHeight { get; set; } = 600;
    public bool PresentationModeEnabled { get; set; } = false;
    public List<string> PresentationApps { get; set; } = new() { "PowerPoint", "Zoom", "Google Meet", "Microsoft Teams" };
    public bool PerAppTintEnabled { get; set; } = false;
    public double PerAppTintOpacity { get; set; } = 0.15;

    // System Integration (M9)
    public bool StartWithWindows { get; set; } = false;
    public int SelectedMonitorIndex { get; set; } = 0;

    // Settings window theming (M9.5)
    // Supports "System", "Custom", or any overlay theme name (built-in/custom).
    public string SettingsThemeMode { get; set; } = "Windows Dark";
    public string SettingsWindowBg { get; set; } = "#111111";
    public double SettingsWindowOpacity { get; set; } = 0.95;
    public double SettingsSurfaceOpacity { get; set; } = 1.0;
    public double SettingsElementOpacity { get; set; } = 1.0;
    public string SettingsWindowSurface { get; set; } = "#1C1C1C";
    public string SettingsWindowSurfaceLight { get; set; } = "#262626";
    public string SettingsWindowSurfaceHover { get; set; } = "#303030";
    public string SettingsWindowText { get; set; } = "#F3F3F3";
    public string SettingsWindowTextSecondary { get; set; } = "#C7C7C7";
    public string SettingsWindowTextMuted { get; set; } = "#8A8A8A";
    public string SettingsWindowAccent { get; set; } = "#0078D4";
    public string SettingsWindowBorder { get; set; } = "#353535";
    public bool LinkOverlayThemeAndUiTheme { get; set; } = false;

    // Settings window display mode (M9.5)
    public string SettingsDisplayMode { get; set; } = "Popup"; // "Window" or "Popup"
    public bool PopupAutoClose { get; set; } = false;
    public double SettingsWindowCornerRadius { get; set; } = 20;
    public bool CompactSettingsWindow { get; set; } = true;

    // Time-based theme switching (M13 nice-to-have)
    public bool ThemeScheduleEnabled { get; set; } = false;
    public string DayThemeName { get; set; } = "Windows Light";
    public string NightThemeName { get; set; } = "Windows Dark";
    public string DayStartTime { get; set; } = "07:00";
    public string NightStartTime { get; set; } = "19:00";

    // Notification grouping (M13 nice-to-have)
    public bool GroupByApp { get; set; } = false;

    // Session archive (M13) — opt-in, RAM-only, never persisted to disk
    public bool SessionArchiveEnabled { get; set; } = false;
    public int SessionArchiveMaxItems { get; set; } = 200;

    // UX Polish (M8)
    public bool HasShownWelcome { get; set; } = false;
    public double? SettingsWindowLeft { get; set; }
    public double? SettingsWindowTop { get; set; }

    public AppSettings Clone()
    {
        var clone = (AppSettings)MemberwiseClone();
        clone.MutedApps = new List<string>(MutedApps);
        clone.HighlightKeywords = new List<string>(HighlightKeywords);
        clone.PerKeywordColors = new Dictionary<string, string>(PerKeywordColors);
        clone.HighlightKeywordRegexFlags = new Dictionary<string, bool>(HighlightKeywordRegexFlags);
        clone.MuteKeywordRegexFlags = new Dictionary<string, bool>(MuteKeywordRegexFlags);
        clone.MuteKeywords = new List<string>(MuteKeywords);
        clone.PresentationApps = new List<string>(PresentationApps);
        clone.PerAppSounds = new Dictionary<string, string>(PerAppSounds);
        clone.PerAppIcons = new Dictionary<string, string>(PerAppIcons);
        return clone;
    }
}
