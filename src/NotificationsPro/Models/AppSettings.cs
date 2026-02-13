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

    // Appearance — Colors
    public string TextColor { get; set; } = "#E4E4EF";
    public string TitleColor { get; set; } = "#FFFFFF";
    public string AppNameColor { get; set; } = "#B8B8CC";
    public string BackgroundColor { get; set; } = "#1E1E2E";
    public double BackgroundOpacity { get; set; } = 0.92;
    public string AccentColor { get; set; } = "#7C5CFC";

    // Appearance — Card Shape
    public double CornerRadius { get; set; } = 12;
    public double Padding { get; set; } = 16;
    public double CardGap { get; set; } = 8;
    public double OuterMargin { get; set; } = 4;
    public bool ShowAccent { get; set; } = true;
    public double AccentThickness { get; set; } = 3;
    public bool ShowBorder { get; set; } = false;
    public string BorderColor { get; set; } = "#363650";
    public double BorderThickness { get; set; } = 1;

    // Behavior
    public double NotificationDuration { get; set; } = 5;
    public int MaxVisibleNotifications { get; set; } = 3;
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
    public bool ShowTimestamp { get; set; } = false;
    public bool NewestOnTop { get; set; } = true;
    public bool AlwaysOnTop { get; set; } = true;
    public bool ClickThrough { get; set; } = false;
    public bool AnimationsEnabled { get; set; } = true;
    public bool FadeOnlyAnimation { get; set; } = false;
    public string SlideInDirection { get; set; } = "Left";
    public double AnimationDurationMs { get; set; } = 300;
    public bool NotificationsPaused { get; set; } = false;
    public bool DeduplicationEnabled { get; set; } = true;
    public double DeduplicationWindowSeconds { get; set; } = 2;

    // Filtering
    public List<string> MutedApps { get; set; } = new();
    public List<string> HighlightKeywords { get; set; } = new();
    public List<string> MuteKeywords { get; set; } = new();
    public string HighlightColor { get; set; } = "#FFD700";

    // Scheduling
    public bool QuietHoursEnabled { get; set; } = false;
    public string QuietHoursStart { get; set; } = "22:00";
    public string QuietHoursEnd { get; set; } = "08:00";

    // Burst limiting
    public bool BurstLimitEnabled { get; set; } = false;
    public int BurstLimitCount { get; set; } = 10;
    public double BurstLimitWindowSeconds { get; set; } = 5;

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

    // Accessibility — Information density
    public string DensityPreset { get; set; } = "Comfortable";

    // Position & Size (null = not yet positioned, use default)
    public double? OverlayLeft { get; set; }
    public double? OverlayTop { get; set; }
    public double OverlayWidth { get; set; } = 380;
    public double LastManualOverlayWidth { get; set; } = 380;
    public double OverlayMaxHeight { get; set; } = 600;
    public bool AllowManualResize { get; set; } = true;
    public int MonitorIndex { get; set; } = 0;
    public bool SnapToEdges { get; set; } = true;
    public double SnapDistance { get; set; } = 20;

    // Overlay visibility
    public bool OverlayVisible { get; set; } = true;

    // UX Polish (M8)
    public bool HasShownWelcome { get; set; } = false;
    public double? SettingsWindowLeft { get; set; }
    public double? SettingsWindowTop { get; set; }

    public AppSettings Clone()
    {
        var clone = (AppSettings)MemberwiseClone();
        clone.MutedApps = new List<string>(MutedApps);
        clone.HighlightKeywords = new List<string>(HighlightKeywords);
        clone.MuteKeywords = new List<string>(MuteKeywords);
        return clone;
    }
}
