using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using NotificationsPro.Helpers;
using NotificationsPro.Models;
using NotificationsPro.Services;
using WinForms = System.Windows.Forms;

namespace NotificationsPro.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    public const double OverlayWidthMin = 220;
    public const double OverlayWidthMax = 7680;
    public const double OverlayMaxHeightMin = 200;
    public const double OverlayMaxHeightMax = 4320;

    private readonly SettingsManager _settingsManager;
    private readonly QueueManager _queueManager;
    private readonly DispatcherTimer _saveDebounce;
    private bool _overlayWidthDirty;

    private static readonly string[] PreviewApps =
    {
        "Microsoft Teams",
        "Slack",
        "Outlook Mail",
        "Windows Security",
        "Discord"
    };

    private static readonly string[] PreviewTitles =
    {
        "Sarah sent a message",
        "Design review updated",
        "Q2 planning reminder",
        "Scan completed",
        "GameNight starts soon"
    };

    private static readonly string[] PreviewBodies =
    {
        "Sarah: Hey, are you available for a quick call about the project?",
        "New message in #general — Alex uploaded the final designs for review.",
        "You have 3 unread messages from the marketing team regarding Q2 planning.",
        "Your device scan completed successfully. No threats found.",
        "GameNight server — Jake: Who's joining tonight? Starting at 8pm!"
    };

    private int _previewIndex;

    // Preview card visibility (not persisted — session-only toggle)
    private bool _showPreviewCard;
    public bool ShowPreviewCard { get => _showPreviewCard; set => SetProperty(ref _showPreviewCard, value); }

    // Typography — shared
    private string _fontFamily = "Segoe UI";
    public string FontFamily { get => _fontFamily; set { if (SetProperty(ref _fontFamily, value)) QueueSave(); } }

    private double _lineSpacing = 1.5;
    public double LineSpacing { get => _lineSpacing; set { if (SetProperty(ref _lineSpacing, value)) QueueSave(); } }

    // Typography — body
    private double _fontSize = 14;
    public double FontSize { get => _fontSize; set { if (SetProperty(ref _fontSize, value)) QueueSave(); } }

    private string _fontWeight = "Normal";
    public string FontWeight { get => _fontWeight; set { if (SetProperty(ref _fontWeight, value)) QueueSave(); } }

    // Typography — app name
    private double _appNameFontSize = 14;
    public double AppNameFontSize { get => _appNameFontSize; set { if (SetProperty(ref _appNameFontSize, value)) QueueSave(); } }

    private string _appNameFontWeight = "SemiBold";
    public string AppNameFontWeight { get => _appNameFontWeight; set { if (SetProperty(ref _appNameFontWeight, value)) QueueSave(); } }

    // Typography — title
    private double _titleFontSize = 16;
    public double TitleFontSize { get => _titleFontSize; set { if (SetProperty(ref _titleFontSize, value)) QueueSave(); } }

    private string _titleFontWeight = "SemiBold";
    public string TitleFontWeight { get => _titleFontWeight; set { if (SetProperty(ref _titleFontWeight, value)) QueueSave(); } }

    // Colors
    private string _textColor = "#E6E6E6";
    public string TextColor { get => _textColor; set { if (SetProperty(ref _textColor, value)) QueueSave(); } }

    private string _titleColor = "#FFFFFF";
    public string TitleColor { get => _titleColor; set { if (SetProperty(ref _titleColor, value)) QueueSave(); } }

    private string _appNameColor = "#C8C8C8";
    public string AppNameColor { get => _appNameColor; set { if (SetProperty(ref _appNameColor, value)) QueueSave(); } }

    private string _backgroundColor = "#202020";
    public string BackgroundColor { get => _backgroundColor; set { if (SetProperty(ref _backgroundColor, value)) QueueSave(); } }

    private double _backgroundOpacity = 0.94;
    public double BackgroundOpacity { get => _backgroundOpacity; set { if (SetProperty(ref _backgroundOpacity, value)) QueueSave(); } }

    private string _accentColor = "#0078D4";
    public string AccentColor { get => _accentColor; set { if (SetProperty(ref _accentColor, value)) QueueSave(); } }

    // Card shape
    private double _cornerRadius = 12;
    public double CornerRadius { get => _cornerRadius; set { if (SetProperty(ref _cornerRadius, value)) QueueSave(); } }

    private double _padding = 16;
    public double Padding { get => _padding; set { if (SetProperty(ref _padding, value)) QueueSave(); } }

    private double _cardGap = 8;
    public double CardGap { get => _cardGap; set { if (SetProperty(ref _cardGap, value)) QueueSave(); } }

    private double _outerMargin = 4;
    public double OuterMargin { get => _outerMargin; set { if (SetProperty(ref _outerMargin, value)) QueueSave(); } }

    private bool _showAccent = true;
    public bool ShowAccent { get => _showAccent; set { if (SetProperty(ref _showAccent, value)) QueueSave(); } }

    private double _accentThickness = 3;
    public double AccentThickness { get => _accentThickness; set { if (SetProperty(ref _accentThickness, value)) QueueSave(); } }

    private bool _showBorder;
    public bool ShowBorder { get => _showBorder; set { if (SetProperty(ref _showBorder, value)) QueueSave(); } }

    private string _borderColor = "#3A3A3A";
    public string BorderColor { get => _borderColor; set { if (SetProperty(ref _borderColor, value)) QueueSave(); } }

    private double _borderThickness = 1;
    public double BorderThickness { get => _borderThickness; set { if (SetProperty(ref _borderThickness, value)) QueueSave(); } }

    // Behavior
    private double _notificationDuration = 5;
    public double NotificationDuration { get => _notificationDuration; set { if (SetProperty(ref _notificationDuration, value)) QueueSave(); } }

    private int _maxVisibleNotifications = 15;
    public int MaxVisibleNotifications { get => _maxVisibleNotifications; set { if (SetProperty(ref _maxVisibleNotifications, Math.Max(1, value))) QueueSave(); } }

    private bool _showAppName = true;
    public bool ShowAppName { get => _showAppName; set { if (SetProperty(ref _showAppName, value)) QueueSave(); } }

    private bool _showNotificationTitle = true;
    public bool ShowNotificationTitle { get => _showNotificationTitle; set { if (SetProperty(ref _showNotificationTitle, value)) QueueSave(); } }

    private bool _showNotificationBody = true;
    public bool ShowNotificationBody { get => _showNotificationBody; set { if (SetProperty(ref _showNotificationBody, value)) QueueSave(); } }

    private bool _limitTextLines;
    public bool LimitTextLines { get => _limitTextLines; set { if (SetProperty(ref _limitTextLines, value)) QueueSave(); } }

    private int _maxAppNameLines = 2;
    public int MaxAppNameLines { get => _maxAppNameLines; set { if (SetProperty(ref _maxAppNameLines, Math.Max(1, value))) QueueSave(); } }

    private int _maxTitleLines = 2;
    public int MaxTitleLines { get => _maxTitleLines; set { if (SetProperty(ref _maxTitleLines, Math.Max(1, value))) QueueSave(); } }

    private int _maxBodyLines = 4;
    public int MaxBodyLines { get => _maxBodyLines; set { if (SetProperty(ref _maxBodyLines, Math.Max(1, value))) QueueSave(); } }

    private bool _singleLineMode;
    public bool SingleLineMode
    {
        get => _singleLineMode;
        set
        {
            if (!SetProperty(ref _singleLineMode, value)) return;
            OnPropertyChanged(nameof(IsStackedLayout));
            QueueSave();
        }
    }

    private bool _singleLineWrapText;
    public bool SingleLineWrapText { get => _singleLineWrapText; set { if (SetProperty(ref _singleLineWrapText, value)) QueueSave(); } }

    private int _singleLineMaxLines = 3;
    public int SingleLineMaxLines { get => _singleLineMaxLines; set { if (SetProperty(ref _singleLineMaxLines, Math.Max(1, value))) QueueSave(); } }

    private bool _singleLineAutoFullWidth;
    public bool SingleLineAutoFullWidth { get => _singleLineAutoFullWidth; set { if (SetProperty(ref _singleLineAutoFullWidth, value)) QueueSave(); } }

    private bool _showTimestamp;
    public bool ShowTimestamp { get => _showTimestamp; set { if (SetProperty(ref _showTimestamp, value)) QueueSave(); } }

    private double _timestampFontSize = 11;
    public double TimestampFontSize { get => _timestampFontSize; set { if (SetProperty(ref _timestampFontSize, Math.Clamp(value, 8, 32))) QueueSave(); } }

    private string _timestampDisplayMode = "Relative";
    public string TimestampDisplayMode { get => _timestampDisplayMode; set { if (SetProperty(ref _timestampDisplayMode, NormalizeTimestampDisplayMode(value))) QueueSave(); } }

    private string _timestampFontWeight = "Normal";
    public string TimestampFontWeight { get => _timestampFontWeight; set { if (SetProperty(ref _timestampFontWeight, value)) QueueSave(); } }

    private string _timestampColor = "#C8C8C8";
    public string TimestampColor { get => _timestampColor; set { if (SetProperty(ref _timestampColor, value)) QueueSave(); } }

    private bool _newestOnTop = true;
    public bool NewestOnTop { get => _newestOnTop; set { if (SetProperty(ref _newestOnTop, value)) QueueSave(); } }

    public bool IsStackedLayout => !SingleLineMode;

    private bool _alwaysOnTop = true;
    public bool AlwaysOnTop { get => _alwaysOnTop; set { if (SetProperty(ref _alwaysOnTop, value)) QueueSave(); } }

    private bool _clickThrough = false;
    public bool ClickThrough { get => _clickThrough; set { if (SetProperty(ref _clickThrough, value)) QueueSave(); } }

    private bool _animationsEnabled = true;
    public bool AnimationsEnabled { get => _animationsEnabled; set { if (SetProperty(ref _animationsEnabled, value)) QueueSave(); } }

    private bool _fadeOnlyAnimation;
    public bool FadeOnlyAnimation { get => _fadeOnlyAnimation; set { if (SetProperty(ref _fadeOnlyAnimation, value)) QueueSave(); } }

    private string _slideInDirection = "Left";
    public string SlideInDirection { get => _slideInDirection; set { if (SetProperty(ref _slideInDirection, value)) QueueSave(); } }

    private double _animationDurationMs = 300;
    public double AnimationDurationMs { get => _animationDurationMs; set { if (SetProperty(ref _animationDurationMs, value)) QueueSave(); } }

    // Deduplication
    private bool _deduplicationEnabled = true;
    public bool DeduplicationEnabled { get => _deduplicationEnabled; set { if (SetProperty(ref _deduplicationEnabled, value)) QueueSave(); } }

    private double _deduplicationWindowSeconds = 2;
    public double DeduplicationWindowSeconds { get => _deduplicationWindowSeconds; set { if (SetProperty(ref _deduplicationWindowSeconds, value)) QueueSave(); } }

    // Filtering
    private string _highlightColor = "#FFD700";
    public string HighlightColor { get => _highlightColor; set { if (SetProperty(ref _highlightColor, value)) QueueSave(); } }

    private string _newHighlightKeyword = string.Empty;
    public string NewHighlightKeyword { get => _newHighlightKeyword; set => SetProperty(ref _newHighlightKeyword, value); }

    private string _newMuteKeyword = string.Empty;
    public string NewMuteKeyword { get => _newMuteKeyword; set => SetProperty(ref _newMuteKeyword, value); }

    public ObservableCollection<string> HighlightKeywords { get; } = new();
    public ObservableCollection<string> MuteKeywords { get; } = new();
    public ObservableCollection<MutedAppEntry> MutedAppEntries { get; } = new();

    // Notification icons (M9.5)
    private bool _showNotificationIcons;
    public bool ShowNotificationIcons { get => _showNotificationIcons; set { if (SetProperty(ref _showNotificationIcons, value)) QueueSave(); } }

    private double _iconSize = 24;
    public double IconSize { get => _iconSize; set { if (SetProperty(ref _iconSize, Math.Clamp(value, 16, 48))) QueueSave(); } }

    private string _defaultIconPreset = "None";
    public string DefaultIconPreset { get => _defaultIconPreset; set { if (SetProperty(ref _defaultIconPreset, value)) QueueSave(); } }

    public List<string> AvailableIconPresets { get; } = new(Models.IconPreset.PresetNames);

    // Per-app config entries (populated from SeenAppNames)
    public ObservableCollection<PerAppConfigEntry> PerAppConfigEntries { get; } = new();

    // Notification sounds (M9.5)
    private bool _soundEnabled;
    public bool SoundEnabled { get => _soundEnabled; set { if (SetProperty(ref _soundEnabled, value)) QueueSave(); } }

    private string _defaultSound = "None";
    public string DefaultSound { get => _defaultSound; set { if (SetProperty(ref _defaultSound, value)) QueueSave(); } }

    public ObservableCollection<string> AvailableSystemSounds { get; } = new(Services.SoundService.SystemSoundNames);

    // Per-app dropdown options include "Default" as a fallback option
    public List<string> PerAppSoundOptions { get; } = new(new[] { "Default" }.Concat(Services.SoundService.SystemSoundNames));
    public List<string> PerAppIconOptions { get; } = new(new[] { "Default" }.Concat(Models.IconPreset.PresetNames));

    // Toast suppression (M9.5)
    private bool _suppressToastPopups;
    public bool SuppressToastPopups { get => _suppressToastPopups; set { if (SetProperty(ref _suppressToastPopups, value)) QueueSave(); } }

    // Scheduling
    private bool _quietHoursEnabled;
    public bool QuietHoursEnabled { get => _quietHoursEnabled; set { if (SetProperty(ref _quietHoursEnabled, value)) QueueSave(); } }

    private string _quietHoursStart = "22:00";
    public string QuietHoursStart { get => _quietHoursStart; set { if (SetProperty(ref _quietHoursStart, value)) QueueSave(); } }

    private string _quietHoursEnd = "08:00";
    public string QuietHoursEnd { get => _quietHoursEnd; set { if (SetProperty(ref _quietHoursEnd, value)) QueueSave(); } }

    // Burst limiting
    private bool _burstLimitEnabled;
    public bool BurstLimitEnabled { get => _burstLimitEnabled; set { if (SetProperty(ref _burstLimitEnabled, value)) QueueSave(); } }

    private int _burstLimitCount = 10;
    public int BurstLimitCount { get => _burstLimitCount; set { if (SetProperty(ref _burstLimitCount, Math.Max(1, value))) QueueSave(); } }

    private double _burstLimitWindowSeconds = 5;
    public double BurstLimitWindowSeconds { get => _burstLimitWindowSeconds; set { if (SetProperty(ref _burstLimitWindowSeconds, value)) QueueSave(); } }

    // Accessibility — Timing
    private bool _persistentNotifications;
    public bool PersistentNotifications { get => _persistentNotifications; set { if (SetProperty(ref _persistentNotifications, value)) QueueSave(); } }

    // Accessibility — Master toggle
    private bool _accessibilityModeEnabled;
    public bool AccessibilityModeEnabled
    {
        get => _accessibilityModeEnabled;
        set
        {
            if (SetProperty(ref _accessibilityModeEnabled, value))
            {
                if (value) ApplyAccessibilityDefaults();
                QueueSave();
            }
        }
    }

    private bool _autoDurationEnabled;
    public bool AutoDurationEnabled { get => _autoDurationEnabled; set { if (SetProperty(ref _autoDurationEnabled, value)) QueueSave(); } }

    private double _autoDurationSecondsPerLine = 2.0;
    public double AutoDurationSecondsPerLine { get => _autoDurationSecondsPerLine; set { if (SetProperty(ref _autoDurationSecondsPerLine, value)) QueueSave(); } }

    private double _autoDurationBaseSeconds = 5.0;
    public double AutoDurationBaseSeconds { get => _autoDurationBaseSeconds; set { if (SetProperty(ref _autoDurationBaseSeconds, value)) QueueSave(); } }

    // Accessibility — System integration
    private bool _respectReduceMotion = true;
    public bool RespectReduceMotion { get => _respectReduceMotion; set { if (SetProperty(ref _respectReduceMotion, value)) QueueSave(); } }

    private bool _respectHighContrast = true;
    public bool RespectHighContrast { get => _respectHighContrast; set { if (SetProperty(ref _respectHighContrast, value)) QueueSave(); } }

    private bool _respectTextScaling;
    public bool RespectTextScaling { get => _respectTextScaling; set { if (SetProperty(ref _respectTextScaling, value)) QueueSave(); } }

    // Accessibility — Global hotkeys
    private bool _globalHotkeysEnabled;
    public bool GlobalHotkeysEnabled { get => _globalHotkeysEnabled; set { if (SetProperty(ref _globalHotkeysEnabled, value)) QueueSave(); } }

    private string _hotkeyToggleOverlay = "Ctrl+Alt+N";
    public string HotkeyToggleOverlay { get => _hotkeyToggleOverlay; set { if (SetProperty(ref _hotkeyToggleOverlay, value)) QueueSave(); } }

    private string _hotkeyDismissAll = "Ctrl+Alt+D";
    public string HotkeyDismissAll { get => _hotkeyDismissAll; set { if (SetProperty(ref _hotkeyDismissAll, value)) QueueSave(); } }

    private string _hotkeyToggleDnd = "Ctrl+Alt+P";
    public string HotkeyToggleDnd { get => _hotkeyToggleDnd; set { if (SetProperty(ref _hotkeyToggleDnd, value)) QueueSave(); } }

    // Accessibility — Density
    private string _densityPreset = "Comfortable";
    public string DensityPreset { get => _densityPreset; set => SetProperty(ref _densityPreset, value); }

    // Streaming & Presentation (M10)
    private bool _chromaKeyEnabled;
    public bool ChromaKeyEnabled { get => _chromaKeyEnabled; set { if (SetProperty(ref _chromaKeyEnabled, value)) QueueSave(); } }

    private string _chromaKeyColor = "#00FF00";
    public string ChromaKeyColor { get => _chromaKeyColor; set { if (SetProperty(ref _chromaKeyColor, value)) QueueSave(); } }

    private bool _obsFixedWindowMode;
    public bool ObsFixedWindowMode { get => _obsFixedWindowMode; set { if (SetProperty(ref _obsFixedWindowMode, value)) QueueSave(); } }

    private double _obsFixedWidth = 400;
    public double ObsFixedWidth { get => _obsFixedWidth; set { if (SetProperty(ref _obsFixedWidth, Math.Clamp(value, 200, 7680))) QueueSave(); } }

    private double _obsFixedHeight = 600;
    public double ObsFixedHeight { get => _obsFixedHeight; set { if (SetProperty(ref _obsFixedHeight, Math.Clamp(value, 200, 4320))) QueueSave(); } }

    private bool _presentationModeEnabled;
    public bool PresentationModeEnabled { get => _presentationModeEnabled; set { if (SetProperty(ref _presentationModeEnabled, value)) QueueSave(); } }

    private string _newPresentationApp = string.Empty;
    public string NewPresentationApp { get => _newPresentationApp; set => SetProperty(ref _newPresentationApp, value); }

    public ObservableCollection<string> PresentationApps { get; } = new();

    private bool _perAppTintEnabled;
    public bool PerAppTintEnabled { get => _perAppTintEnabled; set { if (SetProperty(ref _perAppTintEnabled, value)) QueueSave(); } }

    private double _perAppTintOpacity = 0.15;
    public double PerAppTintOpacity { get => _perAppTintOpacity; set { if (SetProperty(ref _perAppTintOpacity, value)) QueueSave(); } }

    public List<string> AvailableChromaColors { get; } = new()
    {
        "#00FF00", "#0000FF", "#FF00FF"
    };

    // Fullscreen overlay mode (M9.5)
    private bool _fullscreenOverlayMode;
    public bool FullscreenOverlayMode { get => _fullscreenOverlayMode; set { if (SetProperty(ref _fullscreenOverlayMode, value)) QueueSave(); } }

    private double _fullscreenOverlayOpacity = 0.5;
    public double FullscreenOverlayOpacity { get => _fullscreenOverlayOpacity; set { if (SetProperty(ref _fullscreenOverlayOpacity, Math.Clamp(value, 0.1, 1.0))) QueueSave(); } }

    private string _fullscreenOverlayColor = "#000000";
    public string FullscreenOverlayColor { get => _fullscreenOverlayColor; set { if (SetProperty(ref _fullscreenOverlayColor, value)) QueueSave(); } }

    // Settings window theming (M9.5)
    private bool _suppressSettingsThemeAutoCustom;

    private string _settingsThemeMode = "Windows Dark";
    public string SettingsThemeMode
    {
        get => _settingsThemeMode;
        set
        {
            var normalized = Services.SettingsThemeService.NormalizeThemeMode(value);
            if (!SetProperty(ref _settingsThemeMode, normalized)) return;

            if (string.Equals(normalized, "System", StringComparison.OrdinalIgnoreCase)
                && Services.SettingsThemeService.TryGetPresetColors(normalized, out var systemColors))
            {
                ApplySettingsThemeColors(systemColors, queueSave: false);
            }
            else if (!string.Equals(normalized, "Custom", StringComparison.OrdinalIgnoreCase))
            {
                var namedTheme = FindThemeByName(normalized);
                if (namedTheme != null)
                {
                    ApplySettingsThemeColors(GetSettingsThemeColors(namedTheme), queueSave: false);
                }
                else if (Services.SettingsThemeService.TryGetPresetColors(normalized, out var presetColors))
                {
                    ApplySettingsThemeColors(presetColors, queueSave: false);
                }
            }

            QueueSave();
            ApplySettingsTheme();
        }
    }

    private string _settingsWindowBg = "#111111";
    public string SettingsWindowBg { get => _settingsWindowBg; set => SetSettingsWindowColor(ref _settingsWindowBg, value); }

    private string _settingsWindowSurface = "#1C1C1C";
    public string SettingsWindowSurface { get => _settingsWindowSurface; set => SetSettingsWindowColor(ref _settingsWindowSurface, value); }

    private string _settingsWindowSurfaceLight = "#262626";
    public string SettingsWindowSurfaceLight { get => _settingsWindowSurfaceLight; set => SetSettingsWindowColor(ref _settingsWindowSurfaceLight, value); }

    private string _settingsWindowSurfaceHover = "#303030";
    public string SettingsWindowSurfaceHover { get => _settingsWindowSurfaceHover; set => SetSettingsWindowColor(ref _settingsWindowSurfaceHover, value); }

    private string _settingsWindowText = "#F3F3F3";
    public string SettingsWindowText { get => _settingsWindowText; set => SetSettingsWindowColor(ref _settingsWindowText, value); }

    private string _settingsWindowTextSecondary = "#C7C7C7";
    public string SettingsWindowTextSecondary { get => _settingsWindowTextSecondary; set => SetSettingsWindowColor(ref _settingsWindowTextSecondary, value); }

    private string _settingsWindowTextMuted = "#8A8A8A";
    public string SettingsWindowTextMuted { get => _settingsWindowTextMuted; set => SetSettingsWindowColor(ref _settingsWindowTextMuted, value); }

    private string _settingsWindowAccent = "#0078D4";
    public string SettingsWindowAccent { get => _settingsWindowAccent; set => SetSettingsWindowColor(ref _settingsWindowAccent, value); }

    private string _settingsWindowBorder = "#353535";
    public string SettingsWindowBorder { get => _settingsWindowBorder; set => SetSettingsWindowColor(ref _settingsWindowBorder, value); }

    private bool _linkOverlayThemeAndUiTheme;
    public bool LinkOverlayThemeAndUiTheme { get => _linkOverlayThemeAndUiTheme; set { if (SetProperty(ref _linkOverlayThemeAndUiTheme, value)) QueueSave(); } }

    public ObservableCollection<string> AvailableSettingsThemeModes { get; } = new();

    private void SetSettingsWindowColor(ref string backingField, string value)
    {
        if (!SetProperty(ref backingField, value))
            return;

        if (!_suppressSettingsThemeAutoCustom
            && !string.Equals(_settingsThemeMode, "Custom", StringComparison.OrdinalIgnoreCase))
        {
            _settingsThemeMode = "Custom";
            OnPropertyChanged(nameof(SettingsThemeMode));
        }

        QueueSave();
        ApplySettingsTheme();
    }

    private void ApplySettingsThemeColors(IReadOnlyList<string> colors, bool queueSave)
    {
        if (colors.Count < 9)
            return;

        _suppressSettingsThemeAutoCustom = true;
        try
        {
            _settingsWindowBg = colors[0];
            _settingsWindowSurface = colors[1];
            _settingsWindowSurfaceLight = colors[2];
            _settingsWindowSurfaceHover = colors[3];
            _settingsWindowText = colors[4];
            _settingsWindowTextSecondary = colors[5];
            _settingsWindowTextMuted = colors[6];
            _settingsWindowAccent = colors[7];
            _settingsWindowBorder = colors[8];
        }
        finally
        {
            _suppressSettingsThemeAutoCustom = false;
        }

        OnPropertyChanged(nameof(SettingsWindowBg));
        OnPropertyChanged(nameof(SettingsWindowSurface));
        OnPropertyChanged(nameof(SettingsWindowSurfaceLight));
        OnPropertyChanged(nameof(SettingsWindowSurfaceHover));
        OnPropertyChanged(nameof(SettingsWindowText));
        OnPropertyChanged(nameof(SettingsWindowTextSecondary));
        OnPropertyChanged(nameof(SettingsWindowTextMuted));
        OnPropertyChanged(nameof(SettingsWindowAccent));
        OnPropertyChanged(nameof(SettingsWindowBorder));

        if (queueSave)
            QueueSave();
    }

    private ThemePreset? FindThemeByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        foreach (var theme in BuiltInThemes)
        {
            if (string.Equals(theme.Name, name, StringComparison.OrdinalIgnoreCase))
                return theme;
        }

        foreach (var theme in CustomThemes)
        {
            if (string.Equals(theme.Name, name, StringComparison.OrdinalIgnoreCase))
                return theme;
        }

        return null;
    }

    private static string[] GetSettingsThemeColors(ThemePreset theme)
    {
        return
        [
            theme.SettingsWindowBg,
            theme.SettingsWindowSurface,
            theme.SettingsWindowSurfaceLight,
            theme.SettingsWindowSurfaceHover,
            theme.SettingsWindowText,
            theme.SettingsWindowTextSecondary,
            theme.SettingsWindowTextMuted,
            theme.SettingsWindowAccent,
            theme.SettingsWindowBorder
        ];
    }

    private static void CopySettingsUiTheme(AppSettings source, AppSettings target)
    {
        target.SettingsThemeMode = source.SettingsThemeMode;
        target.SettingsWindowBg = source.SettingsWindowBg;
        target.SettingsWindowSurface = source.SettingsWindowSurface;
        target.SettingsWindowSurfaceLight = source.SettingsWindowSurfaceLight;
        target.SettingsWindowSurfaceHover = source.SettingsWindowSurfaceHover;
        target.SettingsWindowText = source.SettingsWindowText;
        target.SettingsWindowTextSecondary = source.SettingsWindowTextSecondary;
        target.SettingsWindowTextMuted = source.SettingsWindowTextMuted;
        target.SettingsWindowAccent = source.SettingsWindowAccent;
        target.SettingsWindowBorder = source.SettingsWindowBorder;
    }

    private void RefreshSettingsThemeModeOptions()
    {
        var options = new List<string> { "System" };

        foreach (var theme in BuiltInThemes)
        {
            if (!options.Contains(theme.Name, StringComparer.OrdinalIgnoreCase))
                options.Add(theme.Name);
        }

        foreach (var theme in CustomThemes)
        {
            if (!options.Contains(theme.Name, StringComparer.OrdinalIgnoreCase))
                options.Add(theme.Name);
        }

        var currentMode = Services.SettingsThemeService.NormalizeThemeMode(_settingsThemeMode);
        if (!string.IsNullOrWhiteSpace(currentMode)
            && !options.Contains(currentMode, StringComparer.OrdinalIgnoreCase)
            && !string.Equals(currentMode, "Custom", StringComparison.OrdinalIgnoreCase))
        {
            options.Add(currentMode);
        }

        options.Add("Custom");

        AvailableSettingsThemeModes.Clear();
        foreach (var option in options)
            AvailableSettingsThemeModes.Add(option);
    }

    private void ApplySettingsTheme()
    {
        if (System.Windows.Application.Current == null) return;
        Services.SettingsThemeService.ApplySettingsTheme(BuildCurrentSettings());
    }

    private AppSettings BuildCurrentSettings()
    {
        return new AppSettings
        {
            SettingsThemeMode = Services.SettingsThemeService.NormalizeThemeMode(SettingsThemeMode),
            SettingsWindowBg = SettingsWindowBg,
            SettingsWindowSurface = SettingsWindowSurface,
            SettingsWindowSurfaceLight = SettingsWindowSurfaceLight,
            SettingsWindowSurfaceHover = SettingsWindowSurfaceHover,
            SettingsWindowText = SettingsWindowText,
            SettingsWindowTextSecondary = SettingsWindowTextSecondary,
            SettingsWindowTextMuted = SettingsWindowTextMuted,
            SettingsWindowAccent = SettingsWindowAccent,
            SettingsWindowBorder = SettingsWindowBorder,
            LinkOverlayThemeAndUiTheme = LinkOverlayThemeAndUiTheme,
        };
    }

    private static string NormalizeTimestampDisplayMode(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
            return "Relative";

        if (string.Equals(mode, "Relative", StringComparison.OrdinalIgnoreCase))
            return "Relative";
        if (string.Equals(mode, "Time", StringComparison.OrdinalIgnoreCase))
            return "Time";
        if (string.Equals(mode, "DateTime", StringComparison.OrdinalIgnoreCase))
            return "DateTime";

        return "Relative";
    }

    // Settings window display mode (M9.5)
    private string _settingsDisplayMode = "Popup";
    public string SettingsDisplayMode { get => _settingsDisplayMode; set { if (SetProperty(ref _settingsDisplayMode, value)) QueueSave(); } }

    private bool _popupAutoClose;
    public bool PopupAutoClose { get => _popupAutoClose; set { if (SetProperty(ref _popupAutoClose, value)) QueueSave(); } }

    public List<string> AvailableSettingsDisplayModes { get; } = new() { "Window", "Popup" };

    // System Integration (M9) — Start with Windows
    private bool _startWithWindows;
    public bool StartWithWindows
    {
        get => _startWithWindows;
        set
        {
            if (!SetProperty(ref _startWithWindows, value)) return;
            if (value)
                StartupHelper.EnableStartup();
            else
                StartupHelper.DisableStartup();
            QueueSave();
        }
    }

    // System Integration (M9) — Multi-monitor
    public ObservableCollection<MonitorInfo> MonitorItems { get; } = new();

    private int _selectedMonitorIndex;
    public int SelectedMonitorIndex
    {
        get => _selectedMonitorIndex;
        set
        {
            if (SetProperty(ref _selectedMonitorIndex, value))
                QueueSave();
        }
    }

    // UX Polish (M8) — saved feedback
    private bool _showSavedIndicator;
    public bool ShowSavedIndicator { get => _showSavedIndicator; set => SetProperty(ref _showSavedIndicator, value); }
    private DispatcherTimer? _savedFeedbackTimer;

    // UX Polish (M8) — first-run tip
    private bool _showFirstRunTip;
    public bool ShowFirstRunTip { get => _showFirstRunTip; set => SetProperty(ref _showFirstRunTip, value); }

    public ICommand DismissFirstRunTipCommand { get; }

    // Position
    private double _overlayWidth = 380;
    public double OverlayWidth
    {
        get => _overlayWidth;
        set
        {
            var clamped = Math.Clamp(value, OverlayWidthMin, OverlayWidthMax);
            if (!SetProperty(ref _overlayWidth, clamped)) return;
            _overlayWidthDirty = true;
            QueueSave();
        }
    }

    private double _overlayMaxHeight = 600;
    public double OverlayMaxHeight
    {
        get => _overlayMaxHeight;
        set
        {
            var clamped = Math.Clamp(value, OverlayMaxHeightMin, OverlayMaxHeightMax);
            if (SetProperty(ref _overlayMaxHeight, clamped))
                QueueSave();
        }
    }

    private bool _allowManualResize = true;
    public bool AllowManualResize { get => _allowManualResize; set { if (SetProperty(ref _allowManualResize, value)) QueueSave(); } }

    private bool _snapToEdges = true;
    public bool SnapToEdges { get => _snapToEdges; set { if (SetProperty(ref _snapToEdges, value)) QueueSave(); } }

    private double _snapDistance = 20;
    public double SnapDistance { get => _snapDistance; set { if (SetProperty(ref _snapDistance, value)) QueueSave(); } }

    // Overlay scrollbar (M9.5)
    private bool _overlayScrollbarVisible = true;
    public bool OverlayScrollbarVisible { get => _overlayScrollbarVisible; set { if (SetProperty(ref _overlayScrollbarVisible, value)) QueueSave(); } }

    private double _overlayScrollbarWidth = 8;
    public double OverlayScrollbarWidth { get => _overlayScrollbarWidth; set { if (SetProperty(ref _overlayScrollbarWidth, Math.Clamp(value, 4, 20))) QueueSave(); } }

    private double _overlayScrollbarOpacity = 1.0;
    public double OverlayScrollbarOpacity { get => _overlayScrollbarOpacity; set { if (SetProperty(ref _overlayScrollbarOpacity, Math.Clamp(value, 0.1, 1.0))) QueueSave(); } }

    // Collections
    public List<string> AvailableFonts { get; }
    public List<string> AvailableFontWeights { get; } = new()
    {
        "Thin", "Light", "Normal", "Medium", "SemiBold", "Bold"
    };

    public List<string> AvailableSlideDirections { get; } = new()
    {
        "Left", "Right", "Top", "Bottom"
    };

    public List<string> AvailableTimestampDisplayModes { get; } = new()
    {
        "Relative", "Time", "DateTime"
    };

    // Commands
    public ICommand PreviewNotificationCommand { get; }
    public ICommand ResetToDefaultsCommand { get; }
    public ICommand MoveOverlayPresetCommand { get; }
    public ICommand SetOverlayWidthPresetCommand { get; }
    public ICommand SetOverlayHeightPresetCommand { get; }
    public ICommand AddHighlightKeywordCommand { get; }
    public ICommand RemoveHighlightKeywordCommand { get; }
    public ICommand AddMuteKeywordCommand { get; }
    public ICommand RemoveMuteKeywordCommand { get; }
    public ICommand ToggleMuteAppCommand { get; }
    public ICommand ApplyThemeCommand { get; }
    public ICommand SaveCustomThemeCommand { get; }
    public ICommand DeleteCustomThemeCommand { get; }
    public ICommand ExportSettingsCommand { get; }
    public ICommand ImportSettingsCommand { get; }
    public ICommand ApplyDensityPresetCommand { get; }
    public ICommand MoveToMonitorCommand { get; }
    public ICommand RefreshMonitorsCommand { get; }
    public ICommand AddPresentationAppCommand { get; }
    public ICommand RemovePresentationAppCommand { get; }
    public ICommand TestSoundCommand { get; }
    public ICommand BrowseCustomSoundCommand { get; }
    public ICommand BrowseCustomIconCommand { get; }
    public ImageSource TrayIconImage { get; }

    // Themes
    private readonly ThemeManager _themeManager = new();

    private string _newThemeName = string.Empty;
    public string NewThemeName { get => _newThemeName; set => SetProperty(ref _newThemeName, value); }

    public ObservableCollection<ThemePreset> BuiltInThemes { get; } = new();
    public ObservableCollection<ThemePreset> CustomThemes { get; } = new();

    private ThemePreset? _selectedBuiltInTheme;
    public ThemePreset? SelectedBuiltInTheme
    {
        get => _selectedBuiltInTheme;
        set => SetProperty(ref _selectedBuiltInTheme, value);
    }

    public ICommand ApplySelectedBuiltInThemeCommand { get; private set; } = null!;

    public SettingsViewModel(SettingsManager settingsManager, QueueManager queueManager)
    {
        _settingsManager = settingsManager;
        _queueManager = queueManager;

        _saveDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _saveDebounce.Tick += (_, _) =>
        {
            _saveDebounce.Stop();
            SaveSettings();
        };

        AvailableFonts = Fonts.SystemFontFamilies
            .Select(f => f.Source)
            .OrderBy(f => f)
            .ToList();

        PreviewNotificationCommand = new RelayCommand(SendPreviewNotification);
        ResetToDefaultsCommand = new RelayCommand(ResetToDefaults);
        MoveOverlayPresetCommand = new RelayCommand(MoveOverlayPreset);
        SetOverlayWidthPresetCommand = new RelayCommand(SetOverlayWidthPreset);
        SetOverlayHeightPresetCommand = new RelayCommand(SetOverlayHeightPreset);
        AddHighlightKeywordCommand = new RelayCommand(_ => AddHighlightKeyword());
        RemoveHighlightKeywordCommand = new RelayCommand(RemoveHighlightKeyword);
        AddMuteKeywordCommand = new RelayCommand(_ => AddMuteKeyword());
        RemoveMuteKeywordCommand = new RelayCommand(RemoveMuteKeyword);
        ToggleMuteAppCommand = new RelayCommand(ToggleMuteApp);
        ApplyThemeCommand = new RelayCommand(ApplyTheme);
        ApplySelectedBuiltInThemeCommand = new RelayCommand(_ =>
        {
            if (SelectedBuiltInTheme != null)
                ApplyTheme(SelectedBuiltInTheme);
        });
        SaveCustomThemeCommand = new RelayCommand(_ => SaveCustomTheme());
        DeleteCustomThemeCommand = new RelayCommand(DeleteCustomTheme);
        ExportSettingsCommand = new RelayCommand(_ => ExportSettings());
        ImportSettingsCommand = new RelayCommand(_ => ImportSettings());
        ApplyDensityPresetCommand = new RelayCommand(ApplyDensityPreset);
        MoveToMonitorCommand = new RelayCommand(_ => MoveToSelectedMonitor());
        RefreshMonitorsCommand = new RelayCommand(_ => RefreshMonitors());
        AddPresentationAppCommand = new RelayCommand(_ => AddPresentationApp());
        RemovePresentationAppCommand = new RelayCommand(RemovePresentationApp);
        TestSoundCommand = new RelayCommand(_ => TestSound());
        BrowseCustomSoundCommand = new RelayCommand(_ => BrowseCustomSound());
        BrowseCustomIconCommand = new RelayCommand(_ => BrowseCustomIcon());
        DismissFirstRunTipCommand = new RelayCommand(_ => DismissFirstRunTip());
        TrayIconImage = IconHelper.CreateTrayIconImageSource(32);

        foreach (var t in ThemePreset.BuiltInThemes)
            BuiltInThemes.Add(t);
        _selectedBuiltInTheme = BuiltInThemes.FirstOrDefault();
        RefreshCustomThemes();

        LoadFromSettings();
        RefreshMonitors();
        RefreshPerAppConfig();

        // Show first-run tip if welcome hasn't been shown yet
        if (!_settingsManager.Settings.HasShownWelcome)
            ShowFirstRunTip = true;
    }

    private void LoadFromSettings()
    {
        var s = _settingsManager.Settings;
        _fontFamily = s.FontFamily;
        _fontSize = s.FontSize;
        _fontWeight = s.FontWeight;
        _appNameFontSize = s.AppNameFontSize;
        _appNameFontWeight = s.AppNameFontWeight;
        _titleFontSize = s.TitleFontSize;
        _titleFontWeight = s.TitleFontWeight;
        _lineSpacing = s.LineSpacing;
        _textColor = s.TextColor;
        _titleColor = s.TitleColor;
        _appNameColor = s.AppNameColor;
        _backgroundColor = s.BackgroundColor;
        _backgroundOpacity = s.BackgroundOpacity;
        _accentColor = s.AccentColor;
        _cornerRadius = s.CornerRadius;
        _padding = s.Padding;
        _cardGap = s.CardGap;
        _outerMargin = s.OuterMargin;
        _showAccent = s.ShowAccent;
        _accentThickness = s.AccentThickness;
        _showBorder = s.ShowBorder;
        _borderColor = s.BorderColor;
        _borderThickness = s.BorderThickness;
        _notificationDuration = s.NotificationDuration;
        _maxVisibleNotifications = Math.Max(1, s.MaxVisibleNotifications);
        _showAppName = s.ShowAppName;
        _showNotificationTitle = s.ShowNotificationTitle;
        _showNotificationBody = s.ShowNotificationBody;
        _limitTextLines = s.LimitTextLines;
        _maxAppNameLines = s.MaxAppNameLines;
        _maxTitleLines = s.MaxTitleLines;
        _maxBodyLines = s.MaxBodyLines;
        _singleLineMode = s.SingleLineMode;
        _singleLineWrapText = s.SingleLineWrapText;
        _singleLineMaxLines = Math.Max(1, s.SingleLineMaxLines);
        _singleLineAutoFullWidth = s.SingleLineAutoFullWidth;
        _showTimestamp = s.ShowTimestamp;
        _timestampFontSize = Math.Clamp(s.TimestampFontSize, 8, 32);
        _timestampDisplayMode = NormalizeTimestampDisplayMode(s.TimestampDisplayMode);
        _timestampFontWeight = string.IsNullOrWhiteSpace(s.TimestampFontWeight) ? "Normal" : s.TimestampFontWeight;
        _timestampColor = string.IsNullOrWhiteSpace(s.TimestampColor) ? "#C8C8C8" : s.TimestampColor;
        _newestOnTop = s.NewestOnTop;
        _alwaysOnTop = s.AlwaysOnTop;
        _clickThrough = s.ClickThrough;
        _animationsEnabled = s.AnimationsEnabled;
        _fadeOnlyAnimation = s.FadeOnlyAnimation;
        _slideInDirection = s.SlideInDirection;
        _animationDurationMs = s.AnimationDurationMs;
        _deduplicationEnabled = s.DeduplicationEnabled;
        _deduplicationWindowSeconds = s.DeduplicationWindowSeconds;
        _highlightColor = s.HighlightColor;
        _showNotificationIcons = s.ShowNotificationIcons;
        _iconSize = s.IconSize;
        _defaultIconPreset = s.DefaultIconPreset;
        _soundEnabled = s.SoundEnabled;
        _defaultSound = s.DefaultSound;
        _suppressToastPopups = s.SuppressToastPopups;
        _quietHoursEnabled = s.QuietHoursEnabled;
        _quietHoursStart = s.QuietHoursStart;
        _quietHoursEnd = s.QuietHoursEnd;
        _burstLimitEnabled = s.BurstLimitEnabled;
        _burstLimitCount = s.BurstLimitCount;
        _burstLimitWindowSeconds = s.BurstLimitWindowSeconds;
        _accessibilityModeEnabled = s.AccessibilityModeEnabled;
        _persistentNotifications = s.PersistentNotifications;
        _autoDurationEnabled = s.AutoDurationEnabled;
        _autoDurationSecondsPerLine = s.AutoDurationSecondsPerLine;
        _autoDurationBaseSeconds = s.AutoDurationBaseSeconds;
        _respectReduceMotion = s.RespectReduceMotion;
        _respectHighContrast = s.RespectHighContrast;
        _respectTextScaling = s.RespectTextScaling;
        _globalHotkeysEnabled = s.GlobalHotkeysEnabled;
        _hotkeyToggleOverlay = s.HotkeyToggleOverlay;
        _hotkeyDismissAll = s.HotkeyDismissAll;
        _hotkeyToggleDnd = s.HotkeyToggleDnd;
        _densityPreset = s.DensityPreset;
        _chromaKeyEnabled = s.ChromaKeyEnabled;
        _chromaKeyColor = s.ChromaKeyColor;
        _obsFixedWindowMode = s.ObsFixedWindowMode;
        _obsFixedWidth = s.ObsFixedWidth;
        _obsFixedHeight = s.ObsFixedHeight;
        _presentationModeEnabled = s.PresentationModeEnabled;
        _perAppTintEnabled = s.PerAppTintEnabled;
        _perAppTintOpacity = s.PerAppTintOpacity;
        _fullscreenOverlayMode = s.FullscreenOverlayMode;
        _fullscreenOverlayOpacity = s.FullscreenOverlayOpacity;
        _fullscreenOverlayColor = s.FullscreenOverlayColor;
        _settingsDisplayMode = s.SettingsDisplayMode;
        _popupAutoClose = s.PopupAutoClose;
        _settingsThemeMode = Services.SettingsThemeService.NormalizeThemeMode(s.SettingsThemeMode);
        _settingsWindowBg = s.SettingsWindowBg;
        _settingsWindowSurface = s.SettingsWindowSurface;
        _settingsWindowSurfaceLight = s.SettingsWindowSurfaceLight;
        _settingsWindowSurfaceHover = s.SettingsWindowSurfaceHover;
        _settingsWindowText = s.SettingsWindowText;
        _settingsWindowTextSecondary = s.SettingsWindowTextSecondary;
        _settingsWindowTextMuted = s.SettingsWindowTextMuted;
        _settingsWindowAccent = s.SettingsWindowAccent;
        _settingsWindowBorder = s.SettingsWindowBorder;
        _linkOverlayThemeAndUiTheme = s.LinkOverlayThemeAndUiTheme;
        _startWithWindows = s.StartWithWindows;
        _selectedMonitorIndex = s.SelectedMonitorIndex;

        RefreshSettingsThemeModeOptions();

        if (!string.Equals(_settingsThemeMode, "Custom", StringComparison.OrdinalIgnoreCase)
            && Services.SettingsThemeService.TryGetPresetColors(_settingsThemeMode, out var presetColors))
        {
            ApplySettingsThemeColors(presetColors, queueSave: false);
        }

        HighlightKeywords.Clear();
        foreach (var kw in s.HighlightKeywords) HighlightKeywords.Add(kw);
        MuteKeywords.Clear();
        foreach (var kw in s.MuteKeywords) MuteKeywords.Add(kw);
        PresentationApps.Clear();
        foreach (var app in s.PresentationApps) PresentationApps.Add(app);
        RefreshMutedAppEntries();

        _overlayWidth = Math.Clamp(s.OverlayWidth, OverlayWidthMin, OverlayWidthMax);
        _overlayMaxHeight = Math.Clamp(s.OverlayMaxHeight, OverlayMaxHeightMin, OverlayMaxHeightMax);
        _allowManualResize = s.AllowManualResize;
        _snapToEdges = s.SnapToEdges;
        _snapDistance = s.SnapDistance;
        _overlayScrollbarVisible = s.OverlayScrollbarVisible;
        _overlayScrollbarWidth = s.OverlayScrollbarWidth;
        _overlayScrollbarOpacity = s.OverlayScrollbarOpacity;
        _overlayWidthDirty = false;
        OnPropertyChanged(nameof(IsStackedLayout));
    }

    private void QueueSave()
    {
        _saveDebounce.Stop();
        _saveDebounce.Start();
    }

    private void SaveSettings()
    {
        var previousSettings = _settingsManager.Settings;
        var showAppName = ShowAppName;
        var showTitle = ShowNotificationTitle;
        var showBody = ShowNotificationBody;
        var isLeavingSingleLineMode = previousSettings.SingleLineMode && !SingleLineMode;
        var resolvedLimitTextLines = LimitTextLines;

        // Keep stacked cards dense when returning from single-line mode to avoid
        // carrying over wrapped banner readability settings into stacked layout.
        if (isLeavingSingleLineMode && !resolvedLimitTextLines)
            resolvedLimitTextLines = true;

        // Keep cards meaningful if all display fields are toggled off.
        if (!showAppName && !showTitle && !showBody)
            showBody = true;

        // Preserve a live manually-resized width unless the width control was explicitly adjusted.
        var previousAutoFullWidth = previousSettings.SingleLineMode && previousSettings.SingleLineAutoFullWidth;
        var nextAutoFullWidth = SingleLineMode && SingleLineAutoFullWidth;
        var savedManualWidth = Math.Clamp(
            previousSettings.LastManualOverlayWidth > 0
                ? previousSettings.LastManualOverlayWidth
                : previousSettings.OverlayWidth,
            OverlayWidthMin,
            OverlayWidthMax);

        var resolvedOverlayWidth = _overlayWidthDirty
            ? OverlayWidth
            : previousSettings.OverlayWidth;

        if (previousAutoFullWidth && !nextAutoFullWidth)
            resolvedOverlayWidth = savedManualWidth;

        resolvedOverlayWidth = Math.Clamp(resolvedOverlayWidth, OverlayWidthMin, OverlayWidthMax);
        var nextLastManualWidth = nextAutoFullWidth
            ? savedManualWidth
            : resolvedOverlayWidth;

        if (Math.Abs(resolvedOverlayWidth - _overlayWidth) > 0.5)
        {
            _overlayWidth = resolvedOverlayWidth;
            OnPropertyChanged(nameof(OverlayWidth));
        }

        if (resolvedLimitTextLines != _limitTextLines)
        {
            _limitTextLines = resolvedLimitTextLines;
            OnPropertyChanged(nameof(LimitTextLines));
        }

        var s = new AppSettings
        {
            FontFamily = FontFamily,
            FontSize = FontSize,
            FontWeight = FontWeight,
            AppNameFontSize = AppNameFontSize,
            AppNameFontWeight = AppNameFontWeight,
            TitleFontSize = TitleFontSize,
            TitleFontWeight = TitleFontWeight,
            LineSpacing = LineSpacing,
            TextColor = TextColor,
            TitleColor = TitleColor,
            AppNameColor = AppNameColor,
            BackgroundColor = BackgroundColor,
            BackgroundOpacity = BackgroundOpacity,
            CornerRadius = CornerRadius,
            Padding = Padding,
            CardGap = CardGap,
            OuterMargin = OuterMargin,
            ShowAccent = ShowAccent,
            AccentThickness = AccentThickness,
            ShowBorder = ShowBorder,
            BorderColor = BorderColor,
            BorderThickness = BorderThickness,
            AccentColor = AccentColor,
            NotificationDuration = NotificationDuration,
            MaxVisibleNotifications = Math.Clamp(MaxVisibleNotifications, 1, 40),
            ShowAppName = showAppName,
            ShowNotificationTitle = showTitle,
            ShowNotificationBody = showBody,
            LimitTextLines = resolvedLimitTextLines,
            MaxAppNameLines = Math.Max(1, MaxAppNameLines),
            MaxTitleLines = Math.Max(1, MaxTitleLines),
            MaxBodyLines = Math.Max(1, MaxBodyLines),
            SingleLineMode = SingleLineMode,
            SingleLineWrapText = SingleLineWrapText,
            SingleLineMaxLines = Math.Max(1, SingleLineMaxLines),
            SingleLineAutoFullWidth = SingleLineAutoFullWidth,
            ShowTimestamp = ShowTimestamp,
            TimestampFontSize = Math.Clamp(TimestampFontSize, 8, 32),
            TimestampDisplayMode = NormalizeTimestampDisplayMode(TimestampDisplayMode),
            TimestampFontWeight = TimestampFontWeight,
            TimestampColor = TimestampColor,
            NewestOnTop = NewestOnTop,
            AlwaysOnTop = AlwaysOnTop,
            ClickThrough = ClickThrough,
            AnimationsEnabled = AnimationsEnabled,
            FadeOnlyAnimation = FadeOnlyAnimation,
            SlideInDirection = SlideInDirection,
            AnimationDurationMs = Math.Max(0, AnimationDurationMs),
            DeduplicationEnabled = DeduplicationEnabled,
            DeduplicationWindowSeconds = DeduplicationWindowSeconds,
            HighlightColor = HighlightColor,
            HighlightKeywords = HighlightKeywords.ToList(),
            MuteKeywords = MuteKeywords.ToList(),
            MutedApps = _settingsManager.Settings.MutedApps,
            ShowNotificationIcons = ShowNotificationIcons,
            IconSize = IconSize,
            DefaultIconPreset = DefaultIconPreset,
            PerAppIcons = new Dictionary<string, string>(_settingsManager.Settings.PerAppIcons),
            SoundEnabled = SoundEnabled,
            DefaultSound = DefaultSound,
            PerAppSounds = new Dictionary<string, string>(_settingsManager.Settings.PerAppSounds),
            SuppressToastPopups = SuppressToastPopups,
            QuietHoursEnabled = QuietHoursEnabled,
            QuietHoursStart = QuietHoursStart,
            QuietHoursEnd = QuietHoursEnd,
            BurstLimitEnabled = BurstLimitEnabled,
            BurstLimitCount = Math.Max(1, BurstLimitCount),
            BurstLimitWindowSeconds = BurstLimitWindowSeconds,
            AccessibilityModeEnabled = AccessibilityModeEnabled,
            PersistentNotifications = PersistentNotifications,
            AutoDurationEnabled = AutoDurationEnabled,
            AutoDurationSecondsPerLine = AutoDurationSecondsPerLine,
            AutoDurationBaseSeconds = AutoDurationBaseSeconds,
            RespectReduceMotion = RespectReduceMotion,
            RespectHighContrast = RespectHighContrast,
            RespectTextScaling = RespectTextScaling,
            GlobalHotkeysEnabled = GlobalHotkeysEnabled,
            HotkeyToggleOverlay = HotkeyToggleOverlay,
            HotkeyDismissAll = HotkeyDismissAll,
            HotkeyToggleDnd = HotkeyToggleDnd,
            DensityPreset = DensityPreset,
            OverlayWidth = resolvedOverlayWidth,
            LastManualOverlayWidth = Math.Clamp(nextLastManualWidth, OverlayWidthMin, OverlayWidthMax),
            OverlayMaxHeight = Math.Clamp(OverlayMaxHeight, OverlayMaxHeightMin, OverlayMaxHeightMax),
            AllowManualResize = AllowManualResize,
            SnapToEdges = SnapToEdges,
            SnapDistance = SnapDistance,
            OverlayScrollbarVisible = OverlayScrollbarVisible,
            OverlayScrollbarWidth = OverlayScrollbarWidth,
            OverlayScrollbarOpacity = OverlayScrollbarOpacity,
            // Preserve position from current settings
            OverlayLeft = previousSettings.OverlayLeft,
            OverlayTop = previousSettings.OverlayTop,
            MonitorIndex = previousSettings.MonitorIndex,
            OverlayVisible = previousSettings.OverlayVisible,
            NotificationsPaused = previousSettings.NotificationsPaused,
            ChromaKeyEnabled = ChromaKeyEnabled,
            ChromaKeyColor = ChromaKeyColor,
            ObsFixedWindowMode = ObsFixedWindowMode,
            ObsFixedWidth = ObsFixedWidth,
            ObsFixedHeight = ObsFixedHeight,
            PresentationModeEnabled = PresentationModeEnabled,
            PresentationApps = PresentationApps.ToList(),
            PerAppTintEnabled = PerAppTintEnabled,
            PerAppTintOpacity = PerAppTintOpacity,
            FullscreenOverlayMode = FullscreenOverlayMode,
            FullscreenOverlayOpacity = FullscreenOverlayOpacity,
            FullscreenOverlayColor = FullscreenOverlayColor,
            SettingsDisplayMode = SettingsDisplayMode,
            PopupAutoClose = PopupAutoClose,
            SettingsThemeMode = Services.SettingsThemeService.NormalizeThemeMode(SettingsThemeMode),
            SettingsWindowBg = SettingsWindowBg,
            SettingsWindowSurface = SettingsWindowSurface,
            SettingsWindowSurfaceLight = SettingsWindowSurfaceLight,
            SettingsWindowSurfaceHover = SettingsWindowSurfaceHover,
            SettingsWindowText = SettingsWindowText,
            SettingsWindowTextSecondary = SettingsWindowTextSecondary,
            SettingsWindowTextMuted = SettingsWindowTextMuted,
            SettingsWindowAccent = SettingsWindowAccent,
            SettingsWindowBorder = SettingsWindowBorder,
            LinkOverlayThemeAndUiTheme = LinkOverlayThemeAndUiTheme,
            StartWithWindows = StartWithWindows,
            SelectedMonitorIndex = SelectedMonitorIndex,
            HasShownWelcome = previousSettings.HasShownWelcome,
            SettingsWindowLeft = previousSettings.SettingsWindowLeft,
            SettingsWindowTop = previousSettings.SettingsWindowTop,
        };

        _settingsManager.Apply(s);
        _overlayWidthDirty = false;
        ShowSavedFeedback();
    }

    private void ShowSavedFeedback()
    {
        ShowSavedIndicator = true;
        _savedFeedbackTimer?.Stop();
        _savedFeedbackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
        _savedFeedbackTimer.Tick += (_, _) =>
        {
            _savedFeedbackTimer.Stop();
            ShowSavedIndicator = false;
        };
        _savedFeedbackTimer.Start();
    }

    private void SendPreviewNotification()
    {
        // Ensure behavior changes (like max visible count) apply before preview enqueue.
        _saveDebounce.Stop();
        SaveSettings();

        var cycleIndex = _previewIndex % PreviewApps.Length;
        var sequence = _previewIndex + 1;
        var appName = PreviewApps[cycleIndex];
        var title = $"{PreviewTitles[cycleIndex]} #{sequence}";
        var body = $"{PreviewBodies[cycleIndex]} [Preview {sequence}]";
        _previewIndex++;

        _queueManager.AddNotification(appName, title, body);
    }

    private void MoveOverlayPreset(object? parameter)
    {
        if (parameter is not string preset || string.IsNullOrWhiteSpace(preset))
            return;

        _saveDebounce.Stop();
        SaveSettings();

        var updated = _settingsManager.Settings.Clone();
        // Use the selected monitor from M9 monitor picker
        updated.MonitorIndex = SelectedMonitorIndex;
        updated.SelectedMonitorIndex = SelectedMonitorIndex;
        var workArea = GetWorkAreaForMonitor(SelectedMonitorIndex);
        const double margin = 16;

        var targetWidth = Math.Clamp(updated.OverlayWidth, OverlayWidthMin, OverlayWidthMax);
        if (updated.SingleLineMode && updated.SingleLineAutoFullWidth)
            targetWidth = Math.Clamp(workArea.Width - (margin * 2), OverlayWidthMin, OverlayWidthMax);

        var fallbackHeight = Math.Min(360, workArea.Height - (margin * 2));
        var targetTop = workArea.Top + margin;
        var targetLeft = workArea.Left + margin;

        switch (preset.Trim().ToLowerInvariant())
        {
            case "top-left":
                targetLeft = workArea.Left + margin;
                targetTop = workArea.Top + margin;
                break;
            case "top-center":
                targetLeft = workArea.Left + ((workArea.Width - targetWidth) / 2);
                targetTop = workArea.Top + margin;
                break;
            case "top-right":
                targetLeft = workArea.Right - targetWidth - margin;
                targetTop = workArea.Top + margin;
                break;
            case "middle-left":
                targetLeft = workArea.Left + margin;
                targetTop = workArea.Top + ((workArea.Height - fallbackHeight) / 2);
                break;
            case "middle-right":
                targetLeft = workArea.Right - targetWidth - margin;
                targetTop = workArea.Top + ((workArea.Height - fallbackHeight) / 2);
                break;
            default:
                return;
        }

        var minLeft = workArea.Left;
        var maxLeft = workArea.Right - targetWidth;
        if (maxLeft < minLeft)
            maxLeft = minLeft;

        var minTop = workArea.Top;
        var maxTop = workArea.Bottom - fallbackHeight;
        if (maxTop < minTop)
            maxTop = minTop;

        targetLeft = Math.Max(minLeft, Math.Min(targetLeft, maxLeft));
        targetTop = Math.Max(minTop, Math.Min(targetTop, maxTop));

        updated.OverlayWidth = targetWidth;
        if (!(updated.SingleLineMode && updated.SingleLineAutoFullWidth))
            updated.LastManualOverlayWidth = targetWidth;
        updated.OverlayLeft = targetLeft;
        updated.OverlayTop = targetTop;
        _settingsManager.Apply(updated);

        if (Math.Abs(_overlayWidth - targetWidth) > 0.5)
        {
            _overlayWidth = targetWidth;
            OnPropertyChanged(nameof(OverlayWidth));
        }

        _overlayWidthDirty = false;
    }

    private static Rect GetWorkAreaForMonitor(int monitorIndex)
    {
        var screens = WinForms.Screen.AllScreens;
        if (screens.Length == 0)
            return SystemParameters.WorkArea;

        if (monitorIndex < 0 || monitorIndex >= screens.Length)
            return ToRect(WinForms.Screen.PrimaryScreen?.WorkingArea ?? screens[0].WorkingArea);

        return ToRect(screens[monitorIndex].WorkingArea);
    }

    private static Rect ToRect(System.Drawing.Rectangle rect)
    {
        return new Rect(rect.Left, rect.Top, rect.Width, rect.Height);
    }

    private void SetOverlayHeightPreset(object? parameter)
    {
        if (parameter is not string preset || string.IsNullOrWhiteSpace(preset))
            return;

        var targetHeight = preset.Trim().ToLowerInvariant() switch
        {
            "1080p" => 1080d,
            "2k" => 1440d,
            "4k" => 2160d,
            "8k" => 4320d,
            _ => 0d
        };

        if (targetHeight <= 0)
            return;

        OverlayMaxHeight = targetHeight;
    }

    private void SetOverlayWidthPreset(object? parameter)
    {
        if (parameter is not string preset || string.IsNullOrWhiteSpace(preset))
            return;

        var targetWidth = preset.Trim().ToLowerInvariant() switch
        {
            "1080p" => 1920d,
            "2k" => 2560d,
            "4k" => 3840d,
            "8k" => 7680d,
            _ => 0d
        };

        if (targetWidth <= 0)
            return;

        OverlayWidth = targetWidth;
    }

    private void ResetToDefaults()
    {
        var result = System.Windows.MessageBox.Show(
            "This will reset all settings to their default values. Are you sure?",
            "Reset to Defaults",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        _settingsManager.ResetToDefaults();
        LoadFromSettings();

        // Notify all properties changed
        var props = GetType().GetProperties();
        foreach (var prop in props)
            OnPropertyChanged(prop.Name);
    }

    private void AddHighlightKeyword()
    {
        var kw = NewHighlightKeyword?.Trim();
        if (string.IsNullOrWhiteSpace(kw)) return;
        if (!HighlightKeywords.Contains(kw, StringComparer.OrdinalIgnoreCase))
        {
            HighlightKeywords.Add(kw);
            QueueSave();
        }
        NewHighlightKeyword = string.Empty;
    }

    private void RemoveHighlightKeyword(object? parameter)
    {
        if (parameter is string kw)
        {
            HighlightKeywords.Remove(kw);
            QueueSave();
        }
    }

    private void AddMuteKeyword()
    {
        var kw = NewMuteKeyword?.Trim();
        if (string.IsNullOrWhiteSpace(kw)) return;
        if (!MuteKeywords.Contains(kw, StringComparer.OrdinalIgnoreCase))
        {
            MuteKeywords.Add(kw);
            QueueSave();
        }
        NewMuteKeyword = string.Empty;
    }

    private void RemoveMuteKeyword(object? parameter)
    {
        if (parameter is string kw)
        {
            MuteKeywords.Remove(kw);
            QueueSave();
        }
    }

    private void ToggleMuteApp(object? parameter)
    {
        if (parameter is not string appName) return;
        if (_queueManager.IsAppMuted(appName))
            _queueManager.UnmuteApp(appName);
        else
            _queueManager.MuteApp(appName);
        RefreshMutedAppEntries();
    }

    public void RefreshMutedAppEntries()
    {
        MutedAppEntries.Clear();
        foreach (var app in _queueManager.SeenAppNames.OrderBy(a => a, StringComparer.OrdinalIgnoreCase))
            MutedAppEntries.Add(new MutedAppEntry(app, _queueManager.IsAppMuted(app)));
    }

    public void RefreshPerAppConfig()
    {
        PerAppConfigEntries.Clear();
        var s = _settingsManager.Settings;
        foreach (var app in _queueManager.SeenAppNames.OrderBy(a => a, StringComparer.OrdinalIgnoreCase))
        {
            s.PerAppSounds.TryGetValue(app, out var sound);
            s.PerAppIcons.TryGetValue(app, out var icon);
            PerAppConfigEntries.Add(new PerAppConfigEntry(app, sound ?? "Default", icon ?? "Default", OnPerAppConfigChanged));
        }
    }

    private void OnPerAppConfigChanged(PerAppConfigEntry entry)
    {
        var updated = _settingsManager.Settings.Clone();

        // Update per-app sounds
        if (entry.Sound == "Default" || string.IsNullOrWhiteSpace(entry.Sound))
            updated.PerAppSounds.Remove(entry.AppName);
        else
            updated.PerAppSounds[entry.AppName] = entry.Sound;

        // Update per-app icons
        if (entry.Icon == "Default" || string.IsNullOrWhiteSpace(entry.Icon))
            updated.PerAppIcons.Remove(entry.AppName);
        else
            updated.PerAppIcons[entry.AppName] = entry.Icon;

        _settingsManager.Apply(updated);
    }


    private void ApplyTheme(object? parameter)
    {
        if (parameter is not ThemePreset theme) return;

        _saveDebounce.Stop();
        SaveSettings();

        var current = _settingsManager.Settings;
        var updated = current.Clone();
        theme.ApplyOverlayTo(updated);
        if (updated.LinkOverlayThemeAndUiTheme)
        {
            theme.ApplySettingsWindowTo(updated);
            updated.SettingsThemeMode = theme.Name;
        }
        else
        {
            // Guardrail: overlay theme changes must not mutate settings-window theme when unlinked.
            CopySettingsUiTheme(current, updated);
        }
        _settingsManager.Apply(updated);
        LoadFromSettings();

        var props = GetType().GetProperties();
        foreach (var prop in props)
            OnPropertyChanged(prop.Name);
    }

    private void SaveCustomTheme()
    {
        var name = NewThemeName?.Trim();
        if (string.IsNullOrWhiteSpace(name)) return;

        _saveDebounce.Stop();
        SaveSettings();

        var theme = ThemePreset.FromSettings(_settingsManager.Settings, name);
        _themeManager.SaveCustomTheme(theme);
        NewThemeName = string.Empty;
        RefreshCustomThemes();
    }

    private void DeleteCustomTheme(object? parameter)
    {
        if (parameter is not string themeName) return;
        _themeManager.DeleteCustomTheme(themeName);
        RefreshCustomThemes();
    }

    private void RefreshCustomThemes()
    {
        CustomThemes.Clear();
        foreach (var t in _themeManager.LoadCustomThemes())
            CustomThemes.Add(t);
        RefreshSettingsThemeModeOptions();
    }

    private void ExportSettings()
    {
        _saveDebounce.Stop();
        SaveSettings();

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "JSON files (*.json)|*.json",
            DefaultExt = ".json",
            FileName = "NotificationsPro-settings.json"
        };

        if (dialog.ShowDialog() == true)
            ThemeManager.ExportSettings(_settingsManager.Settings, dialog.FileName);
    }

    private void ImportSettings()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "JSON files (*.json)|*.json",
            DefaultExt = ".json"
        };

        if (dialog.ShowDialog() != true) return;

        var imported = ThemeManager.ImportSettings(dialog.FileName);
        if (imported == null) return;

        // Preserve device-specific fields so importing doesn't reposition the
        // overlay or reset session state on the current machine.
        var current = _settingsManager.Settings;
        imported.OverlayLeft = current.OverlayLeft;
        imported.OverlayTop = current.OverlayTop;
        imported.MonitorIndex = current.MonitorIndex;
        imported.SelectedMonitorIndex = current.SelectedMonitorIndex;
        imported.SettingsWindowLeft = current.SettingsWindowLeft;
        imported.SettingsWindowTop = current.SettingsWindowTop;
        imported.HasShownWelcome = current.HasShownWelcome;
        imported.OverlayVisible = current.OverlayVisible;
        imported.NotificationsPaused = current.NotificationsPaused;

        _settingsManager.Apply(imported);
        LoadFromSettings();

        var props = GetType().GetProperties();
        foreach (var prop in props)
            OnPropertyChanged(prop.Name);
    }

    private void ApplyDensityPreset(object? parameter)
    {
        if (parameter is not string preset || string.IsNullOrWhiteSpace(preset))
            return;

        switch (preset.Trim())
        {
            case "Compact":
                FontSize = 12;
                AppNameFontSize = 11;
                TitleFontSize = 13;
                Padding = 8;
                CardGap = 4;
                OuterMargin = 2;
                LineSpacing = 1.2;
                LimitTextLines = true;
                MaxAppNameLines = 1;
                MaxTitleLines = 1;
                MaxBodyLines = 2;
                break;

            case "Comfortable":
                FontSize = 14;
                AppNameFontSize = 14;
                TitleFontSize = 16;
                Padding = 16;
                CardGap = 8;
                OuterMargin = 4;
                LineSpacing = 1.5;
                LimitTextLines = false;
                MaxAppNameLines = 2;
                MaxTitleLines = 2;
                MaxBodyLines = 4;
                break;

            case "Spacious":
                FontSize = 16;
                AppNameFontSize = 16;
                TitleFontSize = 18;
                Padding = 24;
                CardGap = 12;
                OuterMargin = 8;
                LineSpacing = 1.8;
                LimitTextLines = false;
                MaxAppNameLines = 3;
                MaxTitleLines = 3;
                MaxBodyLines = 6;
                break;

            default:
                return;
        }

        DensityPreset = preset.Trim();
    }

    private void ApplyAccessibilityDefaults()
    {
        PersistentNotifications = true;
        RespectReduceMotion = true;
        RespectHighContrast = true;
        RespectTextScaling = true;
        ApplyDensityPreset("Spacious");
    }

    private void DismissFirstRunTip()
    {
        ShowFirstRunTip = false;
        _settingsManager.Settings.HasShownWelcome = true;
        _settingsManager.Save();
    }

    private void AddPresentationApp()
    {
        var app = NewPresentationApp?.Trim();
        if (string.IsNullOrWhiteSpace(app)) return;
        if (!PresentationApps.Contains(app, StringComparer.OrdinalIgnoreCase))
        {
            PresentationApps.Add(app);
            QueueSave();
        }
        NewPresentationApp = string.Empty;
    }

    private void RemovePresentationApp(object? parameter)
    {
        if (parameter is string app)
        {
            PresentationApps.Remove(app);
            QueueSave();
        }
    }

    private void TestSound()
    {
        _saveDebounce.Stop();
        SaveSettings();
        Services.SoundService.PlaySound("__test__", _settingsManager.Settings);
    }

    private void BrowseCustomSound()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Sound files (*.wav)|*.wav",
            Title = "Choose a custom notification sound"
        };

        if (dialog.ShowDialog() != true) return;

        // Copy to custom sounds directory
        Services.SoundService.EnsureSoundsDirExists();
        var destDir = Services.SoundService.GetCustomSoundsDir();
        var fileName = System.IO.Path.GetFileName(dialog.FileName);
        var destPath = System.IO.Path.Combine(destDir, fileName);

        try
        {
            System.IO.File.Copy(dialog.FileName, destPath, overwrite: true);
        }
        catch { return; }

        // Add to available sounds if not already present
        var displayName = System.IO.Path.GetFileNameWithoutExtension(fileName);
        if (!AvailableSystemSounds.Contains(destPath))
            AvailableSystemSounds.Add(destPath);

        DefaultSound = destPath;
    }

    private void BrowseCustomIcon()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Image files (*.png;*.jpg;*.jpeg;*.ico;*.bmp)|*.png;*.jpg;*.jpeg;*.ico;*.bmp",
            Title = "Choose a custom notification icon"
        };

        if (dialog.ShowDialog() != true) return;

        Services.IconService.EnsureIconsDirExists();
        var destDir = Services.IconService.GetCustomIconsDir();
        var fileName = System.IO.Path.GetFileName(dialog.FileName);
        var destPath = System.IO.Path.Combine(destDir, fileName);

        try
        {
            System.IO.File.Copy(dialog.FileName, destPath, overwrite: true);
        }
        catch { return; }

        if (!AvailableIconPresets.Contains(destPath))
            AvailableIconPresets.Add(destPath);

        DefaultIconPreset = destPath;
    }

    public void RefreshMonitors()
    {
        MonitorItems.Clear();
        var screens = WinForms.Screen.AllScreens;
        for (var i = 0; i < screens.Length; i++)
        {
            var s = screens[i];
            var label = $"Monitor {i + 1}: {s.Bounds.Width}x{s.Bounds.Height}";
            if (s.Primary)
                label += " (Primary)";
            MonitorItems.Add(new MonitorInfo(i, label, s.Primary));
        }

        // Clamp selected index to valid range
        if (_selectedMonitorIndex >= screens.Length)
        {
            _selectedMonitorIndex = 0;
            OnPropertyChanged(nameof(SelectedMonitorIndex));
        }
    }

    private void MoveToSelectedMonitor()
    {
        _saveDebounce.Stop();
        SaveSettings();

        var updated = _settingsManager.Settings.Clone();
        updated.MonitorIndex = SelectedMonitorIndex;
        updated.SelectedMonitorIndex = SelectedMonitorIndex;

        var workArea = GetWorkAreaForMonitor(SelectedMonitorIndex);
        const double margin = 16;

        var targetWidth = Math.Clamp(updated.OverlayWidth, OverlayWidthMin, OverlayWidthMax);
        if (updated.SingleLineMode && updated.SingleLineAutoFullWidth)
            targetWidth = Math.Clamp(workArea.Width - (margin * 2), OverlayWidthMin, OverlayWidthMax);

        // Position at top-right of selected monitor
        updated.OverlayLeft = workArea.Right - targetWidth - margin;
        updated.OverlayTop = workArea.Top + margin;
        updated.OverlayWidth = targetWidth;
        if (!(updated.SingleLineMode && updated.SingleLineAutoFullWidth))
            updated.LastManualOverlayWidth = targetWidth;

        _settingsManager.Apply(updated);

        if (Math.Abs(_overlayWidth - targetWidth) > 0.5)
        {
            _overlayWidth = targetWidth;
            OnPropertyChanged(nameof(OverlayWidth));
        }

        _overlayWidthDirty = false;
    }

    public ThemeManager GetThemeManager() => _themeManager;
}

public class MonitorInfo
{
    public int Index { get; }
    public string DisplayName { get; }
    public bool IsPrimary { get; }

    public MonitorInfo(int index, string displayName, bool isPrimary)
    {
        Index = index;
        DisplayName = displayName;
        IsPrimary = isPrimary;
    }

    public override string ToString() => DisplayName;
}

public class MutedAppEntry
{
    public string AppName { get; }
    public bool IsMuted { get; }

    public MutedAppEntry(string appName, bool isMuted)
    {
        AppName = appName;
        IsMuted = isMuted;
    }
}

public class PerAppConfigEntry : BaseViewModel
{
    public string AppName { get; }
    private readonly Action<PerAppConfigEntry>? _onChanged;

    private string _sound;
    public string Sound
    {
        get => _sound;
        set
        {
            if (!SetProperty(ref _sound, value)) return;
            _onChanged?.Invoke(this);
        }
    }

    private string _icon;
    public string Icon
    {
        get => _icon;
        set
        {
            if (!SetProperty(ref _icon, value)) return;
            _onChanged?.Invoke(this);
        }
    }

    public PerAppConfigEntry(string appName, string sound, string icon, Action<PerAppConfigEntry>? onChanged = null)
    {
        AppName = appName;
        _sound = sound;
        _icon = icon;
        _onChanged = onChanged;
    }
}
