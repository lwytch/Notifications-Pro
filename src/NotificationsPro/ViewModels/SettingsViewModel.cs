using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using NotificationsPro.Helpers;
using NotificationsPro.Models;
using NotificationsPro.Services;
using WinForms = System.Windows.Forms;

namespace NotificationsPro.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    public const double OverlayWidthMin = 220;
    public const double OverlayWidthMax = 7680;
    public const double OverlayMaxHeightMin = 200;
    public const double OverlayMaxHeightMax = 4320;

    private readonly SettingsManager _settingsManager = null!;
    private readonly QueueManager _queueManager = null!;
    private readonly DispatcherTimer _saveDebounce;
    private bool _overlayWidthDirty;

    // Undo/Redo
    private readonly Stack<AppSettings> _undoStack = new();
    private readonly Stack<AppSettings> _redoStack = new();
    private const int MaxUndoHistory = 50;
    private bool _isUndoRedoOperation;


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
    public IEnumerable<string> AvailableTextAlignments => new[] { "Left", "Center", "Right" };

    private string _textAlignment = "Left";
    public string TextAlignment { get => _textAlignment; set { if (SetProperty(ref _textAlignment, value)) QueueSave(); } }

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
    private bool _replaceMode;
    public bool ReplaceMode { get => _replaceMode; set { if (SetProperty(ref _replaceMode, value)) QueueSave(); } }

    private double _notificationDuration = 5;
    public double NotificationDuration { get => _notificationDuration; set { if (SetProperty(ref _notificationDuration, value)) QueueSave(); } }

    private int _maxVisibleNotifications = 40;
    public int MaxVisibleNotifications { get => _maxVisibleNotifications; set { if (SetProperty(ref _maxVisibleNotifications, Math.Clamp(value, 1, AppSettings.MaxVisibleNotificationsUpperBound))) QueueSave(); } }

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

    private string _notificationAnimationStyle = NotificationAnimationStyleHelper.SlideFade;
    public string NotificationAnimationStyle
    {
        get => _notificationAnimationStyle;
        set
        {
            var normalized = NotificationAnimationStyleHelper.Normalize(value);
            if (!SetProperty(ref _notificationAnimationStyle, normalized))
                return;

            var legacyFadeOnly = NotificationAnimationStyleHelper.IsLegacyFadeOnly(normalized);
            if (_fadeOnlyAnimation != legacyFadeOnly)
            {
                _fadeOnlyAnimation = legacyFadeOnly;
                OnPropertyChanged(nameof(FadeOnlyAnimation));
            }

            OnPropertyChanged(nameof(SelectedNotificationAnimationStyleUsesDirection));
            QueueSave();
        }
    }

    public bool SelectedNotificationAnimationStyleUsesDirection =>
        NotificationAnimationStyleHelper.UsesDirection(NotificationAnimationStyle);

    private string _slideInDirection = "Left";
    public string SlideInDirection { get => _slideInDirection; set { if (SetProperty(ref _slideInDirection, value)) QueueSave(); } }

    private double _animationDurationMs = 1200;
    public double AnimationDurationMs { get => _animationDurationMs; set { if (SetProperty(ref _animationDurationMs, value)) QueueSave(); } }

    private string _animationEasing = AnimationEasingHelper.EaseOut;
    public string AnimationEasing { get => _animationEasing; set { if (SetProperty(ref _animationEasing, AnimationEasingHelper.Normalize(value))) QueueSave(); } }

    // Deduplication
    private bool _deduplicationEnabled = true;
    public bool DeduplicationEnabled { get => _deduplicationEnabled; set { if (SetProperty(ref _deduplicationEnabled, value)) QueueSave(); } }

    private double _deduplicationWindowSeconds = 2;
    public double DeduplicationWindowSeconds { get => _deduplicationWindowSeconds; set { if (SetProperty(ref _deduplicationWindowSeconds, value)) QueueSave(); } }

    // Filtering
    private string _highlightColor = "#FFD700";
    public string HighlightColor { get => _highlightColor; set { if (SetProperty(ref _highlightColor, value)) QueueSave(); } }

    private double _highlightOverlayOpacity = 0.25;
    public double HighlightOverlayOpacity { get => _highlightOverlayOpacity; set { if (SetProperty(ref _highlightOverlayOpacity, Math.Clamp(value, 0.05, 0.80))) QueueSave(); } }

    private string _highlightAnimation = HighlightAnimationHelper.None;
    public string HighlightAnimation { get => _highlightAnimation; set { if (SetProperty(ref _highlightAnimation, HighlightAnimationHelper.Normalize(value))) QueueSave(); } }

    private string _highlightBorderMode = HighlightBorderModeHelper.FullBorder;
    public string HighlightBorderMode { get => _highlightBorderMode; set { if (SetProperty(ref _highlightBorderMode, HighlightBorderModeHelper.Normalize(value))) QueueSave(); } }

    private double _highlightBorderThickness = 1;
    public double HighlightBorderThickness { get => _highlightBorderThickness; set { if (SetProperty(ref _highlightBorderThickness, Math.Clamp(value, 0.5, 8.0))) QueueSave(); } }

    private string _newHighlightKeyword = string.Empty;
    public string NewHighlightKeyword { get => _newHighlightKeyword; set => SetProperty(ref _newHighlightKeyword, value); }

    private string _newMuteKeyword = string.Empty;
    public string NewMuteKeyword { get => _newMuteKeyword; set => SetProperty(ref _newMuteKeyword, value); }

    public ObservableCollection<KeywordHighlightEntry> HighlightKeywordEntries { get; } = new();
    public ObservableCollection<MuteKeywordEntry> MuteKeywordEntries { get; } = new();
    public ObservableCollection<MutedAppEntry> MutedAppEntries { get; } = new();
    public ObservableCollection<SpokenAppEntry> SpokenAppEntries { get; } = new();

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
    public string DefaultSound
    {
        get => _defaultSound;
        set
        {
            if (!SetProperty(ref _defaultSound, value)) return;
            // Keep SelectedWindowsSound in sync
            var match = AvailableWindowsSounds.FirstOrDefault(s => string.Equals(s.WavPath, value, StringComparison.OrdinalIgnoreCase));
            if (match != _selectedWindowsSound)
                SetProperty(ref _selectedWindowsSound, match, nameof(SelectedWindowsSound));
            QueueSave();
        }
    }

    private Services.SoundService.WindowsSound? _selectedWindowsSound;
    public Services.SoundService.WindowsSound? SelectedWindowsSound
    {
        get => _selectedWindowsSound;
        set
        {
            if (!SetProperty(ref _selectedWindowsSound, value)) return;
            // Keep DefaultSound in sync
            var path = value?.WavPath ?? "None";
            if (_defaultSound != path)
                SetProperty(ref _defaultSound, path, nameof(DefaultSound));
            QueueSave();
        }
    }

    public ObservableCollection<Services.SoundService.WindowsSound> AvailableWindowsSounds { get; } = new();

    // Per-app dropdown options include "Default" as a fallback option
    public ObservableCollection<Services.SoundService.WindowsSound> PerAppSoundOptions { get; } = new();
    public List<string> PerAppIconOptions { get; } = new(new[] { "Default" }.Concat(Models.IconPreset.PresetNames));

    // Toast suppression (M9.5)
    private bool _suppressToastPopups;
    public bool SuppressToastPopups { get => _suppressToastPopups; set { if (SetProperty(ref _suppressToastPopups, value)) QueueSave(); } }

    // Session archive (M13) — opt-in, RAM-only
    private bool _sessionArchiveEnabled;
    public bool SessionArchiveEnabled { get => _sessionArchiveEnabled; set { if (SetProperty(ref _sessionArchiveEnabled, value)) QueueSave(); } }

    private int _sessionArchiveMaxItems = 200;
    public int SessionArchiveMaxItems { get => _sessionArchiveMaxItems; set { if (SetProperty(ref _sessionArchiveMaxItems, Math.Clamp(value, 10, 1000))) QueueSave(); } }

    // Theme schedule
    private bool _themeScheduleEnabled;
    public bool ThemeScheduleEnabled { get => _themeScheduleEnabled; set { if (SetProperty(ref _themeScheduleEnabled, value)) QueueSave(); } }

    private string _dayThemeName = "Windows Light";
    public string DayThemeName { get => _dayThemeName; set { if (SetProperty(ref _dayThemeName, value)) QueueSave(); } }

    private string _nightThemeName = "Windows Dark";
    public string NightThemeName { get => _nightThemeName; set { if (SetProperty(ref _nightThemeName, value)) QueueSave(); } }

    private string _dayStartTime = "07:00";
    public string DayStartTime { get => _dayStartTime; set { if (SetProperty(ref _dayStartTime, value)) QueueSave(); } }

    private string _nightStartTime = "19:00";
    public string NightStartTime { get => _nightStartTime; set { if (SetProperty(ref _nightStartTime, value)) QueueSave(); } }

    // Notification grouping
    private bool _groupByApp;
    public bool GroupByApp { get => _groupByApp; set { if (SetProperty(ref _groupByApp, value)) QueueSave(); } }

    private string _appGroupingStyle = "Framed Group";
    public string AppGroupingStyle
    {
        get => _appGroupingStyle;
        set
        {
            if (SetProperty(ref _appGroupingStyle, NormalizeAppGroupingStyle(value)))
                QueueSave();
        }
    }

    private bool _showAppGroupCounts = true;
    public bool ShowAppGroupCounts { get => _showAppGroupCounts; set { if (SetProperty(ref _showAppGroupCounts, value)) QueueSave(); } }

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

    private string _globalHotkeyRegistrationError = string.Empty;
    public string GlobalHotkeyRegistrationError
    {
        get => _globalHotkeyRegistrationError;
        private set
        {
            if (SetProperty(ref _globalHotkeyRegistrationError, value))
                OnPropertyChanged(nameof(HasGlobalHotkeyRegistrationError));
        }
    }

    public bool HasGlobalHotkeyRegistrationError => !string.IsNullOrWhiteSpace(GlobalHotkeyRegistrationError);

    // Accessibility — Spoken notifications
    private bool _readNotificationsAloudEnabled;
    public bool ReadNotificationsAloudEnabled { get => _readNotificationsAloudEnabled; set { if (SetProperty(ref _readNotificationsAloudEnabled, value)) QueueSave(); } }

    private string _readNotificationsAloudTriggerMode = NarrationTriggerModeHelper.AllAllowedNotifications;
    public string ReadNotificationsAloudTriggerMode
    {
        get => _readNotificationsAloudTriggerMode;
        set
        {
            if (SetProperty(ref _readNotificationsAloudTriggerMode, NormalizeReadNotificationsAloudTriggerMode(value)))
            {
                OnPropertyChanged(nameof(NarrationRulesOnlyEnabled));
                QueueSave();
            }
        }
    }

    public bool NarrationRulesOnlyEnabled
    {
        get => string.Equals(
            _readNotificationsAloudTriggerMode,
            NarrationTriggerModeHelper.OnlyMatchingNarrationRules,
            StringComparison.OrdinalIgnoreCase);
        set => ReadNotificationsAloudTriggerMode = value
            ? NarrationTriggerModeHelper.OnlyMatchingNarrationRules
            : NarrationTriggerModeHelper.AllAllowedNotifications;
    }

    private string _readNotificationsAloudMode = SpokenNotificationTextFormatter.ModeBodyOnly;
    public string ReadNotificationsAloudMode
    {
        get => _readNotificationsAloudMode;
        set
        {
            if (SetProperty(ref _readNotificationsAloudMode, NormalizeReadNotificationsAloudMode(value)))
                QueueSave();
        }
    }

    private string _readNotificationsAloudVoiceId = string.Empty;
    public string ReadNotificationsAloudVoiceId
    {
        get => _readNotificationsAloudVoiceId;
        set
        {
            if (SetProperty(ref _readNotificationsAloudVoiceId, NormalizeReadNotificationsAloudVoiceId(value)))
                QueueSave();
        }
    }

    private double _readNotificationsAloudRate = 1.0;
    public double ReadNotificationsAloudRate
    {
        get => _readNotificationsAloudRate;
        set
        {
            if (SetProperty(ref _readNotificationsAloudRate, Math.Clamp(value, 0.5, 6.0)))
                QueueSave();
        }
    }

    private double _readNotificationsAloudVolume = 1.0;
    public double ReadNotificationsAloudVolume
    {
        get => _readNotificationsAloudVolume;
        set
        {
            if (SetProperty(ref _readNotificationsAloudVolume, Math.Clamp(value, 0.0, 1.0)))
                QueueSave();
        }
    }

    private bool _isNarrationPreviewInProgress;
    public bool IsNarrationPreviewInProgress
    {
        get => _isNarrationPreviewInProgress;
        private set
        {
            if (!SetProperty(ref _isNarrationPreviewInProgress, value))
                return;

            CommandManager.InvalidateRequerySuggested();
        }
    }

    // Accessibility — Microsoft Voice Access
    private string _voiceAccessReadMode = VoiceAccessTextFormatter.ModeOff;
    public string VoiceAccessReadMode
    {
        get => _voiceAccessReadMode;
        set
        {
            if (SetProperty(ref _voiceAccessReadMode, NormalizeVoiceAccessReadMode(value)))
                QueueSave();
        }
    }

    // Appearance — Density
    private string _densityPreset = "Comfortable";
    public string DensityPreset { get => _densityPreset; set { if (SetProperty(ref _densityPreset, value)) QueueSave(); } }

    private string _notificationAccessStatusSummary = "Checking Windows notification access...";
    public string NotificationAccessStatusSummary
    {
        get => _notificationAccessStatusSummary;
        private set => SetProperty(ref _notificationAccessStatusSummary, value);
    }

    private string _notificationAccessStatusDetail =
        "Notifications Pro uses direct WinRT notification access when Windows allows it, and falls back to accessibility capture when needed.";
    public string NotificationAccessStatusDetail
    {
        get => _notificationAccessStatusDetail;
        private set => SetProperty(ref _notificationAccessStatusDetail, value);
    }

    private Func<Task>? _retryNotificationAccessAsync;

    private string _notificationCaptureMode = NotificationCaptureModeHelper.ModeAuto;
    public string NotificationCaptureMode
    {
        get => _notificationCaptureMode;
        set
        {
            if (SetProperty(ref _notificationCaptureMode, NormalizeNotificationCaptureMode(value)))
                QueueSave();
        }
    }

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
                    ApplySettingsThemePreset(namedTheme, queueSave: false);
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

    private double _settingsWindowOpacity = 0.95;
    public double SettingsWindowOpacity { get => _settingsWindowOpacity; set => SetSettingsThemeOpacity(ref _settingsWindowOpacity, value); }

    private double _settingsSurfaceOpacity = 1.0;
    public double SettingsSurfaceOpacity { get => _settingsSurfaceOpacity; set => SetSettingsThemeOpacity(ref _settingsSurfaceOpacity, value); }

    private double _settingsElementOpacity = 1.0;
    public double SettingsElementOpacity { get => _settingsElementOpacity; set => SetSettingsThemeOpacity(ref _settingsElementOpacity, value); }

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

    private double _settingsWindowCornerRadius = 12;
    public double SettingsWindowCornerRadius { get => _settingsWindowCornerRadius; set => SetSettingsThemeCornerRadius(value); }

    private bool _compactSettingsWindow = true;
    public bool CompactSettingsWindow { get => _compactSettingsWindow; set { if (SetProperty(ref _compactSettingsWindow, value)) QueueSave(); } }

    private bool _linkOverlayThemeAndUiTheme;
    public bool LinkOverlayThemeAndUiTheme { get => _linkOverlayThemeAndUiTheme; set { if (SetProperty(ref _linkOverlayThemeAndUiTheme, value)) QueueSave(); } }

    public ObservableCollection<string> AvailableSettingsThemeModes { get; } = new();

    private void SetSettingsWindowColor(ref string backingField, string value)
    {
        if (!SetProperty(ref backingField, value))
            return;

        MarkSettingsThemeCustomIfNeeded();

        QueueSave();
        ApplySettingsTheme();
    }

    private void SetSettingsThemeOpacity(ref double backingField, double value)
    {
        if (!SetProperty(ref backingField, value))
            return;

        MarkSettingsThemeCustomIfNeeded();
        QueueSave();
        ApplySettingsTheme();
    }

    private void SetSettingsThemeCornerRadius(double value)
    {
        if (!SetProperty(ref _settingsWindowCornerRadius, value))
            return;

        MarkSettingsThemeCustomIfNeeded();
        QueueSave();
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

    private void ApplySettingsThemePreset(ThemePreset theme, bool queueSave)
    {
        _suppressSettingsThemeAutoCustom = true;
        try
        {
            _settingsWindowBg = theme.SettingsWindowBg;
            _settingsWindowSurface = theme.SettingsWindowSurface;
            _settingsWindowSurfaceLight = theme.SettingsWindowSurfaceLight;
            _settingsWindowSurfaceHover = theme.SettingsWindowSurfaceHover;
            _settingsWindowText = theme.SettingsWindowText;
            _settingsWindowTextSecondary = theme.SettingsWindowTextSecondary;
            _settingsWindowTextMuted = theme.SettingsWindowTextMuted;
            _settingsWindowAccent = theme.SettingsWindowAccent;
            _settingsWindowBorder = theme.SettingsWindowBorder;
            _settingsWindowOpacity = theme.SettingsWindowOpacity;
            _settingsSurfaceOpacity = theme.SettingsSurfaceOpacity;
            _settingsElementOpacity = theme.SettingsElementOpacity;
            _settingsWindowCornerRadius = theme.SettingsWindowCornerRadius;
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
        OnPropertyChanged(nameof(SettingsWindowOpacity));
        OnPropertyChanged(nameof(SettingsSurfaceOpacity));
        OnPropertyChanged(nameof(SettingsElementOpacity));
        OnPropertyChanged(nameof(SettingsWindowCornerRadius));

        if (queueSave)
            QueueSave();
    }

    private void MarkSettingsThemeCustomIfNeeded()
    {
        if (_suppressSettingsThemeAutoCustom
            || string.Equals(_settingsThemeMode, "Custom", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _settingsThemeMode = "Custom";
        OnPropertyChanged(nameof(SettingsThemeMode));
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
        target.SettingsWindowOpacity = source.SettingsWindowOpacity;
        target.SettingsSurfaceOpacity = source.SettingsSurfaceOpacity;
        target.SettingsElementOpacity = source.SettingsElementOpacity;
        target.SettingsWindowCornerRadius = source.SettingsWindowCornerRadius;
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
        Services.SettingsThemeService.ApplySettingsTheme(BuildSettingsThemePreviewSnapshot());
    }

    private AppSettings BuildSettingsThemePreviewSnapshot()
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
            SettingsWindowBorder = SettingsWindowBorder,
            SettingsWindowOpacity = SettingsWindowOpacity,
            SettingsSurfaceOpacity = SettingsSurfaceOpacity,
            SettingsElementOpacity = SettingsElementOpacity,
            SettingsWindowCornerRadius = SettingsWindowCornerRadius,
            CompactSettingsWindow = CompactSettingsWindow,
            LinkOverlayThemeAndUiTheme = LinkOverlayThemeAndUiTheme,
        };
    }

    private AppSettings BuildCurrentSettingsSnapshot(AppSettings previousSettings)
    {
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

        return new AppSettings
        {
            FontFamily = FontFamily,
            FontSize = FontSize,
            FontWeight = FontWeight,
            AppNameFontSize = AppNameFontSize,
            AppNameFontWeight = AppNameFontWeight,
            TitleFontSize = TitleFontSize,
            TitleFontWeight = TitleFontWeight,
            LineSpacing = LineSpacing,
            TextAlignment = TextAlignment,
            TextColor = TextColor,
            TitleColor = TitleColor,
            AppNameColor = AppNameColor,
            BackgroundColor = BackgroundColor,
            BackgroundOpacity = BackgroundOpacity,
            CardBackgroundMode = CardBackgroundMode,
            CardBackgroundImagePath = CardBackgroundImagePath,
            CardBackgroundImageOpacity = CardBackgroundImageOpacity,
            CardBackgroundImageHueDegrees = CardBackgroundImageHueDegrees,
            CardBackgroundImageBrightness = CardBackgroundImageBrightness,
            CardBackgroundImageSaturation = CardBackgroundImageSaturation,
            CardBackgroundImageContrast = CardBackgroundImageContrast,
            CardBackgroundImageBlackAndWhite = CardBackgroundImageBlackAndWhite,
            CardBackgroundImageFitMode = CardBackgroundImageFitMode,
            CardBackgroundImagePlacement = CardBackgroundImagePlacement,
            CardBackgroundImageVerticalFocus = CardBackgroundImageVerticalFocus,
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
            MaxVisibleNotifications = Math.Clamp(MaxVisibleNotifications, 1, AppSettings.MaxVisibleNotificationsUpperBound),
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
            ReplaceMode = ReplaceMode,
            ShowTimestamp = ShowTimestamp,
            TimestampFontSize = Math.Clamp(TimestampFontSize, 8, 32),
            TimestampDisplayMode = NormalizeTimestampDisplayMode(TimestampDisplayMode),
            TimestampFontWeight = TimestampFontWeight,
            TimestampColor = TimestampColor,
            NewestOnTop = NewestOnTop,
            AlwaysOnTop = AlwaysOnTop,
            ClickThrough = ClickThrough,
            AnimationsEnabled = AnimationsEnabled,
            FadeOnlyAnimation = NotificationAnimationStyleHelper.IsLegacyFadeOnly(NotificationAnimationStyle),
            NotificationAnimationStyle = NotificationAnimationStyleHelper.Normalize(NotificationAnimationStyle),
            SlideInDirection = SlideInDirection,
            AnimationDurationMs = Math.Max(0, AnimationDurationMs),
            AnimationEasing = AnimationEasingHelper.Normalize(AnimationEasing),
            DeduplicationEnabled = DeduplicationEnabled,
            DeduplicationWindowSeconds = DeduplicationWindowSeconds,
            HighlightColor = HighlightColor,
            HighlightOverlayOpacity = Math.Clamp(HighlightOverlayOpacity, 0.05, 0.80),
            HighlightAnimation = HighlightAnimationHelper.Normalize(HighlightAnimation),
            HighlightBorderMode = HighlightBorderModeHelper.Normalize(HighlightBorderMode),
            HighlightBorderThickness = Math.Clamp(HighlightBorderThickness, 0.5, 8.0),
            HighlightRules = BuildHighlightRules(),
            HighlightKeywords = HighlightKeywordEntries.Select(e => e.Keyword).ToList(),
            PerKeywordColors = HighlightKeywordEntries
                .Where(e => !string.Equals(e.Color, HighlightColor, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(e => e.Keyword, e => e.Color),
            HighlightKeywordRegexFlags = HighlightKeywordEntries
                .Where(e => e.IsRegex)
                .ToDictionary(e => e.Keyword, _ => true),
            MuteRules = BuildMuteRules(),
            MuteKeywords = MuteKeywordEntries.Select(e => e.Keyword).ToList(),
            MuteKeywordRegexFlags = MuteKeywordEntries
                .Where(e => e.IsRegex)
                .ToDictionary(e => e.Keyword, _ => true),
            NarrationRules = BuildNarrationRules(),
            MutedApps = _settingsManager.Settings.MutedApps,
            SpokenMutedApps = new List<string>(_settingsManager.Settings.SpokenMutedApps),
            ShowNotificationIcons = ShowNotificationIcons,
            IconSize = IconSize,
            DefaultIconPreset = DefaultIconPreset,
            PerAppIcons = new Dictionary<string, string>(_settingsManager.Settings.PerAppIcons),
            SoundEnabled = SoundEnabled,
            DefaultSound = DefaultSound,
            PerAppSounds = new Dictionary<string, string>(_settingsManager.Settings.PerAppSounds),
            PerAppBackgroundImages = new Dictionary<string, string>(_settingsManager.Settings.PerAppBackgroundImages),
            SuppressToastPopups = SuppressToastPopups,
            SessionArchiveEnabled = SessionArchiveEnabled,
            SessionArchiveMaxItems = SessionArchiveMaxItems,
            ThemeScheduleEnabled = ThemeScheduleEnabled,
            DayThemeName = DayThemeName,
            NightThemeName = NightThemeName,
            DayStartTime = DayStartTime,
            NightStartTime = NightStartTime,
            GroupByApp = GroupByApp,
            AppGroupingStyle = NormalizeAppGroupingStyle(AppGroupingStyle),
            ShowAppGroupCounts = ShowAppGroupCounts,
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
            ReadNotificationsAloudEnabled = ReadNotificationsAloudEnabled,
            ReadNotificationsAloudTriggerMode = NormalizeReadNotificationsAloudTriggerMode(ReadNotificationsAloudTriggerMode),
            ReadNotificationsAloudMode = NormalizeReadNotificationsAloudMode(ReadNotificationsAloudMode),
            ReadNotificationsAloudVoiceId = NormalizeReadNotificationsAloudVoiceId(ReadNotificationsAloudVoiceId),
            ReadNotificationsAloudRate = ReadNotificationsAloudRate,
            ReadNotificationsAloudVolume = ReadNotificationsAloudVolume,
            VoiceAccessReadMode = NormalizeVoiceAccessReadMode(VoiceAccessReadMode),
            NotificationCaptureMode = NormalizeNotificationCaptureMode(NotificationCaptureMode),
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
            OverlayScrollbarTrackColor = OverlayScrollbarTrackColor,
            OverlayScrollbarThumbColor = OverlayScrollbarThumbColor,
            OverlayScrollbarThumbHoverColor = OverlayScrollbarThumbHoverColor,
            OverlayScrollbarPadding = OverlayScrollbarPadding,
            OverlayScrollbarContentGap = OverlayScrollbarContentGap,
            OverlayScrollbarCornerRadius = OverlayScrollbarCornerRadius,
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
            FullscreenOverlayImagePath = FullscreenOverlayImagePath,
            FullscreenOverlayImageFitMode = FullscreenOverlayImageFitMode,
            FullscreenOverlayImageHueDegrees = FullscreenOverlayImageHueDegrees,
            FullscreenOverlayImageBrightness = FullscreenOverlayImageBrightness,
            FullscreenOverlayImageSaturation = FullscreenOverlayImageSaturation,
            FullscreenOverlayImageContrast = FullscreenOverlayImageContrast,
            FullscreenOverlayImageBlackAndWhite = FullscreenOverlayImageBlackAndWhite,
            FullscreenOverlayImageVerticalFocus = FullscreenOverlayImageVerticalFocus,
            SettingsDisplayMode = SettingsDisplayMode,
            PopupAutoClose = PopupAutoClose,
            SettingsThemeMode = Services.SettingsThemeService.NormalizeThemeMode(SettingsThemeMode),
            SettingsWindowBg = SettingsWindowBg,
            SettingsWindowOpacity = SettingsWindowOpacity,
            SettingsSurfaceOpacity = SettingsSurfaceOpacity,
            SettingsElementOpacity = SettingsElementOpacity,
            SettingsWindowSurface = SettingsWindowSurface,
            SettingsWindowSurfaceLight = SettingsWindowSurfaceLight,
            SettingsWindowSurfaceHover = SettingsWindowSurfaceHover,
            SettingsWindowText = SettingsWindowText,
            SettingsWindowTextSecondary = SettingsWindowTextSecondary,
            SettingsWindowTextMuted = SettingsWindowTextMuted,
            SettingsWindowAccent = SettingsWindowAccent,
            SettingsWindowBorder = SettingsWindowBorder,
            SettingsWindowCornerRadius = SettingsWindowCornerRadius,
            CompactSettingsWindow = CompactSettingsWindow,
            LinkOverlayThemeAndUiTheme = LinkOverlayThemeAndUiTheme,
            StartWithWindows = StartWithWindows,
            SelectedMonitorIndex = SelectedMonitorIndex,
            HasShownWelcome = previousSettings.HasShownWelcome,
            ShowQuickTips = ShowQuickTips,
            SettingsWindowLeft = previousSettings.SettingsWindowLeft,
            SettingsWindowTop = previousSettings.SettingsWindowTop,
        };
    }

    private static string NormalizeTimestampDisplayMode(string? mode)
    {
        return TimestampTextFormatter.NormalizeMode(mode);
    }

    private static string NormalizeVoiceAccessReadMode(string? mode)
    {
        return VoiceAccessTextFormatter.NormalizeMode(mode);
    }

    private static string NormalizeReadNotificationsAloudMode(string? mode)
    {
        return SpokenNotificationTextFormatter.NormalizeMode(mode);
    }

    private static string NormalizeReadNotificationsAloudTriggerMode(string? mode)
    {
        return NarrationTriggerModeHelper.Normalize(mode);
    }

    private static string NormalizeReadNotificationsAloudVoiceId(string? voiceId)
    {
        return string.IsNullOrWhiteSpace(voiceId) ? string.Empty : voiceId.Trim();
    }

    private static string NormalizeAppGroupingStyle(string? style)
    {
        return style switch
        {
            "Framed Group" => "Framed Group",
            "Header Chip" => "Header Chip",
            "Minimal Label" => "Minimal Label",
            _ => "Framed Group"
        };
    }

    private static string NormalizeNotificationCaptureMode(string? mode)
    {
        return NotificationCaptureModeHelper.NormalizeMode(mode);
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
                _ = StartupHelper.EnableStartupAsync();
            else
                _ = StartupHelper.DisableStartupAsync();
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

    private string _overlayScrollbarTrackColor = "#141414";
    public string OverlayScrollbarTrackColor { get => _overlayScrollbarTrackColor; set { if (SetProperty(ref _overlayScrollbarTrackColor, value)) QueueSave(); } }

    private string _overlayScrollbarThumbColor = "#4F4F4F";
    public string OverlayScrollbarThumbColor { get => _overlayScrollbarThumbColor; set { if (SetProperty(ref _overlayScrollbarThumbColor, value)) QueueSave(); } }

    private string _overlayScrollbarThumbHoverColor = "#0078D4";
    public string OverlayScrollbarThumbHoverColor { get => _overlayScrollbarThumbHoverColor; set { if (SetProperty(ref _overlayScrollbarThumbHoverColor, value)) QueueSave(); } }

    private double _overlayScrollbarPadding = 1.5;
    public double OverlayScrollbarPadding { get => _overlayScrollbarPadding; set { if (SetProperty(ref _overlayScrollbarPadding, Math.Clamp(value, 0.0, 6.0))) QueueSave(); } }

    private double _overlayScrollbarContentGap = 10;
    public double OverlayScrollbarContentGap { get => _overlayScrollbarContentGap; set { if (SetProperty(ref _overlayScrollbarContentGap, Math.Clamp(value, 0.0, 24.0))) QueueSave(); } }

    private double _overlayScrollbarCornerRadius = 6;
    public double OverlayScrollbarCornerRadius { get => _overlayScrollbarCornerRadius; set { if (SetProperty(ref _overlayScrollbarCornerRadius, Math.Clamp(value, 0.0, 12.0))) QueueSave(); } }

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

    public List<string> AvailableNotificationAnimationStyles { get; } = NotificationAnimationStyleHelper.KnownModes.ToList();
    public List<string> AvailableAnimationEasings { get; } = AnimationEasingHelper.KnownModes.ToList();

    public List<string> AvailableTimestampDisplayModes { get; } = new()
    {
        "Relative", "Time", "DateTime"
    };

    public List<string> AvailableHighlightAnimations { get; } = HighlightAnimationHelper.KnownModes.ToList();
    public List<string> AvailableHighlightBorderModes { get; } = HighlightBorderModeHelper.KnownModes.ToList();
    public List<string> AvailableHighlightAnimationOverrides { get; } = new(new[] { KeywordHighlightEntry.UseGlobalSetting }.Concat(HighlightAnimationHelper.KnownModes));
    public List<string> AvailableHighlightBorderModeOverrides { get; } = new(new[] { KeywordHighlightEntry.UseGlobalSetting }.Concat(HighlightBorderModeHelper.KnownModes));

    public List<string> AvailableAppGroupingStyles { get; } = new()
    {
        "Framed Group",
        "Header Chip",
        "Minimal Label"
    };

    public List<string> AvailableVoiceAccessReadModes { get; } = new()
    {
        VoiceAccessTextFormatter.ModeOff,
        VoiceAccessTextFormatter.ModeBodyOnly,
        VoiceAccessTextFormatter.ModeTitleBodyTimestamp
    };

    public List<string> AvailableReadNotificationsAloudModes { get; } = new()
    {
        SpokenNotificationTextFormatter.ModeBodyOnly,
        SpokenNotificationTextFormatter.ModeTitleOnly,
        SpokenNotificationTextFormatter.ModeTitleBody,
        SpokenNotificationTextFormatter.ModeBodyTimestamp,
        SpokenNotificationTextFormatter.ModeTitleTimestamp,
        SpokenNotificationTextFormatter.ModeTitleBodyTimestamp
    };

    public List<string> AvailableReadNotificationsAloudTriggerModes { get; } = NarrationTriggerModeHelper.KnownModes.ToList();

    public ObservableCollection<NarrationVoiceOption> AvailableNarrationVoices { get; } = new();

    public List<string> AvailableNotificationCaptureModes { get; } = new()
    {
        NotificationCaptureModeHelper.ModeAuto,
        NotificationCaptureModeHelper.ModeWinRt,
        NotificationCaptureModeHelper.ModeAccessibility
    };

    // Commands
    public ICommand PreviewNotificationCommand { get; }
    public ICommand ResetToDefaultsCommand { get; }
    public ICommand MoveOverlayPresetCommand { get; }
    public ICommand SetOverlayWidthPresetCommand { get; }
    public ICommand SetOverlayHeightPresetCommand { get; }
    public ICommand AddHighlightKeywordCommand { get; }
    public ICommand RemoveHighlightKeywordCommand { get; }
    public ICommand PreviewHighlightNotificationCommand { get; }
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
    public ICommand TestNarrationCommand { get; }
    public ICommand RefreshNarrationVoicesCommand { get; }
    public ICommand BrowseCustomSoundCommand { get; }
    public ICommand BrowseCustomIconCommand { get; }
    public ICommand BrowsePerAppBackgroundImageCommand { get; }
    public ICommand ClearPerAppBackgroundImageCommand { get; }
    public ICommand ApplyStreamingPresetCommand { get; }
    public ICommand ViewSessionArchiveCommand { get; }
    public ICommand OpenNotificationAccessSettingsCommand { get; }
    public ICommand RetryNotificationAccessCommand { get; }
    public ICommand SetFontPresetCommand { get; }
    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    public ICommand SaveProfileCommand { get; }
    public ICommand LoadProfileCommand { get; }
    public ICommand DeleteProfileCommand { get; }

    private bool _canUndo;
    public bool CanUndo { get => _canUndo; private set => SetProperty(ref _canUndo, value); }
    private bool _canRedo;
    public bool CanRedo { get => _canRedo; private set => SetProperty(ref _canRedo, value); }

    // Profiles
    private readonly ProfileManager _profileManager = new();
    public ObservableCollection<string> SavedProfiles { get; } = new();

    private string _newProfileName = "";
    public string NewProfileName { get => _newProfileName; set => SetProperty(ref _newProfileName, value); }

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

        InitializeSettingsAuditPolishCommands();

        if (_queueManager != null)
        {
            // Initial population from memory
            RefreshPerAppConfig();
            RefreshMutedAppEntries();
            RefreshSpokenAppEntries();

            _queueManager.NotificationAdded += _ =>
            {
                // Dispatch to UI thread since NotificationAdded usually fires on the listener thread
                System.Windows.Application.Current?.Dispatcher.InvokeAsync(() =>
                {
                    RefreshPerAppConfig();
                    RefreshMutedAppEntries();
                    RefreshSpokenAppEntries();
                });
            };
        }

        _saveDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _saveDebounce.Tick += (_, _) =>
        {
            _saveDebounce.Stop();
            SaveSettings();
        };

        var fonts = Fonts.SystemFontFamilies
            .Select(f => f.Source)
            .OrderBy(f => f)
            .ToList();
        // Add bundled OpenDyslexic font (accessibility preset)
        const string openDyslexicUri = "pack://application:,,,/Fonts/#OpenDyslexic";
        if (!fonts.Any(f => f.Contains("OpenDyslexic", StringComparison.OrdinalIgnoreCase)))
            fonts.Insert(0, openDyslexicUri);
        AvailableFonts = fonts;

        PreviewNotificationCommand = new RelayCommand(SendPreviewNotification);
        ResetToDefaultsCommand = new RelayCommand(ResetToDefaults);
        MoveOverlayPresetCommand = new RelayCommand(MoveOverlayPreset);
        SetOverlayWidthPresetCommand = new RelayCommand(SetOverlayWidthPreset);
        SetOverlayHeightPresetCommand = new RelayCommand(SetOverlayHeightPreset);
        AddHighlightKeywordCommand = new RelayCommand(_ => AddHighlightKeyword());
        RemoveHighlightKeywordCommand = new RelayCommand(RemoveHighlightKeyword);
        PreviewHighlightNotificationCommand = new RelayCommand(SendHighlightPreviewNotification);
        AddMuteKeywordCommand = new RelayCommand(_ => AddMuteKeyword());
        RemoveMuteKeywordCommand = new RelayCommand(RemoveMuteKeyword);
        InitializeSinglePanelEnhancementCommands();
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
        TestNarrationCommand = new RelayCommand(_ => TestNarration(), _ => !IsNarrationPreviewInProgress);
        RefreshNarrationVoicesCommand = new RelayCommand(_ => RefreshNarrationVoices());
        BrowseCustomSoundCommand = new RelayCommand(_ => BrowseCustomSound());
        BrowseCustomIconCommand = new RelayCommand(_ => BrowseCustomIcon());
        BrowsePerAppBackgroundImageCommand = new RelayCommand(BrowsePerAppBackgroundImage);
        ClearPerAppBackgroundImageCommand = new RelayCommand(ClearPerAppBackgroundImage);
        ApplyStreamingPresetCommand = new RelayCommand(_ => ApplyStreamingPreset());
        ViewSessionArchiveCommand = new RelayCommand(_ => ViewSessionArchive());
        OpenNotificationAccessSettingsCommand = new RelayCommand(OpenNotificationAccessSettings);
        RetryNotificationAccessCommand = new RelayCommand(RetryNotificationAccess);
        SetFontPresetCommand = new RelayCommand(o => FontFamily = o as string ?? "Segoe UI");
        UndoCommand = new RelayCommand(_ => Undo(), _ => _undoStack.Count > 0);
        RedoCommand = new RelayCommand(_ => Redo(), _ => _redoStack.Count > 0);
        SaveProfileCommand = new RelayCommand(_ => SaveProfile());
        LoadProfileCommand = new RelayCommand(o => LoadProfile(o as string));
        DeleteProfileCommand = new RelayCommand(o => DeleteProfile(o as string));
        DismissFirstRunTipCommand = new RelayCommand(_ => DismissFirstRunTip());
        TrayIconImage = IconHelper.CreateTrayIconImageSource(32);

        foreach (var t in ThemePreset.BuiltInThemes)
            BuiltInThemes.Add(t);
        _selectedBuiltInTheme = BuiltInThemes.FirstOrDefault();
        RefreshCustomThemes();

        LoadFromSettings();
        InitializeSettingsAuditPolishViews();
        RefreshWindowsSounds(); // After LoadFromSettings so custom WAV paths are already known
        RefreshMonitors();
        RefreshPerAppConfig();
        RefreshProfiles();

        // Show first-run tip if welcome hasn't been shown yet
        if (!_settingsManager.Settings.HasShownWelcome && ShowQuickTips)
            ShowFirstRunTip = true;
    }

    public void ConfigureRetryNotificationAccess(Func<Task>? retryNotificationAccessAsync)
    {
        _retryNotificationAccessAsync = retryNotificationAccessAsync;
    }

    public void UpdateNotificationAccessStatus(bool isAccessGranted, string? listenerMode, string? statusMessage)
    {
        var normalizedMode = string.IsNullOrWhiteSpace(listenerMode)
            ? "Unknown"
            : listenerMode.Trim();
        var detail = string.IsNullOrWhiteSpace(statusMessage)
            ? "No additional status reported yet."
            : statusMessage.Trim();

        UpdateCaptureDiagnosticSnapshot(normalizedMode, detail);

        if (string.Equals(normalizedMode, "Accessibility", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(NotificationCaptureMode, NotificationCaptureModeHelper.ModeAccessibility, StringComparison.OrdinalIgnoreCase))
            {
                NotificationAccessStatusSummary = "Capture mode: Forced accessibility";
                NotificationAccessStatusDetail =
                    $"Notifications Pro is set to skip direct WinRT capture and read visible notifications through Windows accessibility APIs instead. Use Auto or Prefer WinRT if you want to retry direct notification access later. Current status: {detail}";
                return;
            }

            NotificationAccessStatusSummary = "Capture mode: Accessibility fallback";
            NotificationAccessStatusDetail =
                $"Windows direct notification access is unavailable right now, so Notifications Pro is reading visible notifications through Windows accessibility APIs. If live notifications stop appearing, switch Settings > System > Notification Access > Capture Mode to Force Accessibility or open Windows Settings > Privacy > Notifications, then use Retry Access Check. Current status: {detail}";
            return;
        }

        if (isAccessGranted)
        {
            NotificationAccessStatusSummary = string.Equals(NotificationCaptureMode, NotificationCaptureModeHelper.ModeWinRt, StringComparison.OrdinalIgnoreCase)
                ? "Capture mode: Prefer WinRT (active)"
                : "Capture mode: WinRT notification access granted";
            NotificationAccessStatusDetail =
                $"Notifications Pro can read notifications directly through Windows notification APIs. Current status: {detail}";
            return;
        }

        NotificationAccessStatusSummary = string.Equals(NotificationCaptureMode, NotificationCaptureModeHelper.ModeWinRt, StringComparison.OrdinalIgnoreCase)
            ? "Capture mode: Prefer WinRT"
            : "Capture mode: Checking Windows notification access";
        NotificationAccessStatusDetail =
            $"Notifications Pro is still checking Windows notification access. If direct access is not available, it can fall back to accessibility capture automatically. Current status: {detail}";
    }

    public void UpdateHotkeyRegistrationError(string? registrationError)
    {
        GlobalHotkeyRegistrationError = registrationError?.Trim() ?? string.Empty;
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
        _textAlignment = string.IsNullOrWhiteSpace(s.TextAlignment) ? "Left" : s.TextAlignment;
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
        _maxVisibleNotifications = Math.Clamp(s.MaxVisibleNotifications, 1, AppSettings.MaxVisibleNotificationsUpperBound);
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
        _replaceMode = s.ReplaceMode;
        _showTimestamp = s.ShowTimestamp;
        _timestampFontSize = Math.Clamp(s.TimestampFontSize, 8, 32);
        _timestampDisplayMode = NormalizeTimestampDisplayMode(s.TimestampDisplayMode);
        _timestampFontWeight = string.IsNullOrWhiteSpace(s.TimestampFontWeight) ? "Normal" : s.TimestampFontWeight;
        _timestampColor = string.IsNullOrWhiteSpace(s.TimestampColor) ? "#C8C8C8" : s.TimestampColor;
        _newestOnTop = s.NewestOnTop;
        _alwaysOnTop = s.AlwaysOnTop;
        _clickThrough = s.ClickThrough;
        _animationsEnabled = s.AnimationsEnabled;
        _notificationAnimationStyle = NotificationAnimationStyleHelper.Normalize(s.NotificationAnimationStyle);
        _fadeOnlyAnimation = NotificationAnimationStyleHelper.IsLegacyFadeOnly(_notificationAnimationStyle);
        _slideInDirection = s.SlideInDirection;
        _animationDurationMs = s.AnimationDurationMs;
        _animationEasing = AnimationEasingHelper.Normalize(s.AnimationEasing);
        _deduplicationEnabled = s.DeduplicationEnabled;
        _deduplicationWindowSeconds = s.DeduplicationWindowSeconds;
        _highlightColor = s.HighlightColor;
        _highlightOverlayOpacity = Math.Clamp(s.HighlightOverlayOpacity, 0.05, 0.80);
        _highlightAnimation = HighlightAnimationHelper.Normalize(s.HighlightAnimation);
        _highlightBorderMode = HighlightBorderModeHelper.Normalize(s.HighlightBorderMode);
        _highlightBorderThickness = double.IsNaN(s.HighlightBorderThickness) ? 1 : Math.Clamp(s.HighlightBorderThickness, 0.5, 8.0);
        _showNotificationIcons = s.ShowNotificationIcons;
        _iconSize = s.IconSize;
        _defaultIconPreset = s.DefaultIconPreset;
        _soundEnabled = s.SoundEnabled;
        // Migrate old system sound names (pre-registry) to "None"
        _defaultSound = s.DefaultSound is "Asterisk" or "Beep" or "Exclamation" or "Hand" or "Question"
            ? "None" : s.DefaultSound;
        _suppressToastPopups = s.SuppressToastPopups;
        _sessionArchiveEnabled = s.SessionArchiveEnabled;
        _sessionArchiveMaxItems = s.SessionArchiveMaxItems;
        _themeScheduleEnabled = s.ThemeScheduleEnabled;
        _dayThemeName = s.DayThemeName;
        _nightThemeName = s.NightThemeName;
        _dayStartTime = s.DayStartTime;
        _nightStartTime = s.NightStartTime;
        _groupByApp = s.GroupByApp;
        _appGroupingStyle = NormalizeAppGroupingStyle(s.AppGroupingStyle);
        _showAppGroupCounts = s.ShowAppGroupCounts;
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
        _readNotificationsAloudEnabled = s.ReadNotificationsAloudEnabled;
        _readNotificationsAloudTriggerMode = NormalizeReadNotificationsAloudTriggerMode(s.ReadNotificationsAloudTriggerMode);
        _readNotificationsAloudMode = NormalizeReadNotificationsAloudMode(s.ReadNotificationsAloudMode);
        _readNotificationsAloudVoiceId = NormalizeReadNotificationsAloudVoiceId(s.ReadNotificationsAloudVoiceId);
        _readNotificationsAloudRate = Math.Clamp(s.ReadNotificationsAloudRate, 0.5, 6.0);
        _readNotificationsAloudVolume = Math.Clamp(s.ReadNotificationsAloudVolume, 0.0, 1.0);
        _voiceAccessReadMode = NormalizeVoiceAccessReadMode(s.VoiceAccessReadMode);
        _notificationCaptureMode = NormalizeNotificationCaptureMode(s.NotificationCaptureMode);
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
        _settingsThemeMode = Services.SettingsThemeService.ResolveThemeModeForLoadedSettings(s);
        _settingsWindowBg = s.SettingsWindowBg;
        _settingsWindowOpacity = s.SettingsWindowOpacity;
        _settingsSurfaceOpacity = s.SettingsSurfaceOpacity;
        _settingsElementOpacity = s.SettingsElementOpacity;
        _settingsWindowSurface = s.SettingsWindowSurface;
        _settingsWindowSurfaceLight = s.SettingsWindowSurfaceLight;
        _settingsWindowSurfaceHover = s.SettingsWindowSurfaceHover;
        _settingsWindowText = s.SettingsWindowText;
        _settingsWindowTextSecondary = s.SettingsWindowTextSecondary;
        _settingsWindowTextMuted = s.SettingsWindowTextMuted;
        _settingsWindowAccent = s.SettingsWindowAccent;
        _settingsWindowBorder = s.SettingsWindowBorder;
        _settingsWindowCornerRadius = s.SettingsWindowCornerRadius;
        _compactSettingsWindow = s.CompactSettingsWindow;
        _linkOverlayThemeAndUiTheme = s.LinkOverlayThemeAndUiTheme;
        _startWithWindows = s.StartWithWindows;
        _selectedMonitorIndex = s.SelectedMonitorIndex;

        RefreshSettingsThemeModeOptions();

        if (!string.Equals(_settingsThemeMode, "Custom", StringComparison.OrdinalIgnoreCase)
            && Services.SettingsThemeService.TryGetPresetColors(_settingsThemeMode, out var presetColors))
        {
            ApplySettingsThemeColors(presetColors, queueSave: false);
        }

        LoadSinglePanelEnhancements(s);
        PresentationApps.Clear();
        foreach (var app in s.PresentationApps) PresentationApps.Add(app);
        RefreshMutedAppEntries();
        RefreshSpokenAppEntries();
        RefreshNarrationVoices();

        _overlayWidth = Math.Clamp(s.OverlayWidth, OverlayWidthMin, OverlayWidthMax);
        _overlayMaxHeight = Math.Clamp(s.OverlayMaxHeight, OverlayMaxHeightMin, OverlayMaxHeightMax);
        _allowManualResize = s.AllowManualResize;
        _snapToEdges = s.SnapToEdges;
        _snapDistance = s.SnapDistance;
        _overlayScrollbarVisible = s.OverlayScrollbarVisible;
        _overlayScrollbarWidth = s.OverlayScrollbarWidth;
        _overlayScrollbarOpacity = s.OverlayScrollbarOpacity;
        _overlayScrollbarTrackColor = s.OverlayScrollbarTrackColor;
        _overlayScrollbarThumbColor = s.OverlayScrollbarThumbColor;
        _overlayScrollbarThumbHoverColor = s.OverlayScrollbarThumbHoverColor;
        _overlayScrollbarPadding = s.OverlayScrollbarPadding;
        _overlayScrollbarContentGap = s.OverlayScrollbarContentGap;
        _overlayScrollbarCornerRadius = s.OverlayScrollbarCornerRadius;
        _overlayWidthDirty = false;
        OnPropertyChanged(nameof(IsStackedLayout));
    }

    private void QueueSave()
    {
        _saveDebounce.Stop();
        _saveDebounce.Start();
    }

    private void Undo()
    {
        if (_undoStack.Count == 0) return;
        _redoStack.Push(_settingsManager.Settings.Clone());
        var previous = _undoStack.Pop();
        _isUndoRedoOperation = true;
        _settingsManager.Apply(previous);
        LoadFromSettings();
        NotifyAllPropertiesChanged();
        _isUndoRedoOperation = false;
        UpdateUndoRedoState();
    }

    private void Redo()
    {
        if (_redoStack.Count == 0) return;
        _undoStack.Push(_settingsManager.Settings.Clone());
        var next = _redoStack.Pop();
        _isUndoRedoOperation = true;
        _settingsManager.Apply(next);
        LoadFromSettings();
        NotifyAllPropertiesChanged();
        _isUndoRedoOperation = false;
        UpdateUndoRedoState();
    }

    private void UpdateUndoRedoState()
    {
        CanUndo = _undoStack.Count > 0;
        CanRedo = _redoStack.Count > 0;
    }

    private void NotifyAllPropertiesChanged()
    {
        var props = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in props)
        {
            if (prop.CanRead)
            {
                OnPropertyChanged(prop.Name);
            }
        }
    }

    public void ReloadFromCurrentSettings()
    {
        LoadFromSettings();
        ApplySettingsTheme();
        NotifyAllPropertiesChanged();
    }

    private void RefreshProfiles()
    {
        SavedProfiles.Clear();
        foreach (var name in _profileManager.GetProfileNames())
            SavedProfiles.Add(name);
    }

    private void SaveProfile()
    {
        if (string.IsNullOrWhiteSpace(NewProfileName)) return;
        _saveDebounce.Stop();
        SaveSettings();
        _profileManager.SaveProfile(NewProfileName, _settingsManager.Settings.Clone());
        NewProfileName = "";
        RefreshProfiles();
    }

    private void LoadProfile(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        var profile = _profileManager.LoadProfile(name);
        if (profile == null) return;
        profile.SettingsThemeMode = Services.SettingsThemeService.ResolveThemeModeForLoadedSettings(profile);
        _settingsManager.Apply(profile);
        ReloadFromCurrentSettings();
    }

    private void DeleteProfile(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        _profileManager.DeleteProfile(name);
        RefreshProfiles();
    }

    private void SaveSettings()
    {
        if (!_isUndoRedoOperation)
        {
            _undoStack.Push(_settingsManager.Settings.Clone());
            if (_undoStack.Count > MaxUndoHistory)
            {
                var items = _undoStack.ToArray();
                _undoStack.Clear();
                for (int i = 0; i < MaxUndoHistory; i++)
                    _undoStack.Push(items[i]);
            }
            _redoStack.Clear();
            UpdateUndoRedoState();
        }
        var previousSettings = _settingsManager.Settings;
        var s = BuildCurrentSettingsSnapshot(previousSettings);
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

        _queueManager.AddPreviewNotification(appName, title, body);
    }

    private void SendHighlightPreviewNotification(object? parameter)
    {
        _saveDebounce.Stop();
        SaveSettings();

        var selectedEntry = parameter as KeywordHighlightEntry;
        var pendingKeyword = NewHighlightKeyword?.Trim();
        var configuredEntry = selectedEntry ?? HighlightKeywordEntries.FirstOrDefault();
        var keyword = !string.IsNullOrWhiteSpace(selectedEntry?.Keyword)
            ? selectedEntry.Keyword
            : !string.IsNullOrWhiteSpace(pendingKeyword)
            ? pendingKeyword
            : configuredEntry?.Keyword;
        if (string.IsNullOrWhiteSpace(keyword))
            keyword = "urgent";

        var previewScope = configuredEntry?.Scope ?? NotificationMatchScopeHelper.TitleAndBody;
        var previewColor = !string.IsNullOrWhiteSpace(configuredEntry?.Color)
            ? configuredEntry!.Color
            : HighlightColor;
        var previewAnimation = configuredEntry == null
            || string.Equals(configuredEntry.Animation, KeywordHighlightEntry.UseGlobalSetting, StringComparison.OrdinalIgnoreCase)
                ? HighlightAnimation
                : configuredEntry.Animation;
        var previewBorderMode = configuredEntry == null
            || string.Equals(configuredEntry.BorderMode, KeywordHighlightEntry.UseGlobalSetting, StringComparison.OrdinalIgnoreCase)
                ? HighlightBorderMode
                : configuredEntry.BorderMode;
        var previewOverlayOpacity = configuredEntry?.UseCustomOverlayOpacity == true
            ? configuredEntry.OverlayOpacity
            : HighlightOverlayOpacity;
        var previewBorderThickness = configuredEntry?.UseCustomBorderThickness == true
            ? configuredEntry.BorderThickness
            : HighlightBorderThickness;
        var previewAppName = !string.IsNullOrWhiteSpace(configuredEntry?.AppFilter)
            ? configuredEntry!.AppFilter.Trim()
            : "Filter Preview";
        var (title, body) = BuildHighlightPreviewMessage(keyword, previewScope);

        _queueManager.AddPreviewNotification(
            previewAppName,
            title,
            body,
            isHighlighted: true,
            highlightColor: previewColor,
            highlightAnimation: previewAnimation,
            highlightOverlayOpacity: previewOverlayOpacity,
            highlightBorderMode: previewBorderMode,
            highlightBorderThickness: previewBorderThickness);
    }

    private static (string Title, string Body) BuildHighlightPreviewMessage(string keyword, string scope)
    {
        return NotificationMatchScopeHelper.Normalize(scope) switch
        {
            NotificationMatchScopeHelper.TitleOnly => ($"Preview: {keyword}", "Filtering preview for a title-only highlight rule."),
            NotificationMatchScopeHelper.BodyOnly => ("Highlight preview", $"This body matches the current highlight preview using {keyword}."),
            _ => ($"Preview: {keyword}", $"This filtering preview repeats {keyword} in the body so title + body matches are obvious.")
        };
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

        // Calculate boundary edges safely using exact internal padding metrics.
        // OuterContentMargin=8px on all sides, Bottom Item Margin=16px.
        var targetWidth = Math.Clamp(updated.OverlayWidth, OverlayWidthMin, OverlayWidthMax);
        if (updated.SingleLineMode && updated.SingleLineAutoFullWidth)
            targetWidth = Math.Clamp(workArea.Width + 16, OverlayWidthMin, OverlayWidthMax);

        var overlayWindow = System.Windows.Application.Current.Windows.OfType<NotificationsPro.Views.OverlayWindow>().FirstOrDefault();
        var actualHeight = overlayWindow?.ActualHeight > 0 ? overlayWindow.ActualHeight : Math.Min(360, workArea.Height - 16);

        var targetTop = workArea.Top - 8;
        var targetLeft = workArea.Left - 8;

        switch (preset.Trim().ToLowerInvariant())
        {
            case "top-left":
                targetLeft = workArea.Left - 8;
                targetTop = workArea.Top - 8;
                break;
            case "top-center":
                targetLeft = workArea.Left + ((workArea.Width - targetWidth) / 2);
                targetTop = workArea.Top - 8;
                break;
            case "top-right":
                targetLeft = workArea.Right - targetWidth + 8;
                targetTop = workArea.Top - 8;
                break;
            case "middle-left":
                targetLeft = workArea.Left - 8;
                targetTop = workArea.Top + ((workArea.Height - actualHeight) / 2);
                break;
            case "middle-center":
                targetLeft = workArea.Left + ((workArea.Width - targetWidth) / 2);
                targetTop = workArea.Top + ((workArea.Height - actualHeight) / 2);
                break;
            case "middle-right":
                targetLeft = workArea.Right - targetWidth + 8;
                targetTop = workArea.Top + ((workArea.Height - actualHeight) / 2);
                break;
            case "bottom-left":
                targetLeft = workArea.Left - 8;
                targetTop = workArea.Bottom - actualHeight + 24;
                break;
            case "bottom-center":
                targetLeft = workArea.Left + ((workArea.Width - targetWidth) / 2);
                targetTop = workArea.Bottom - actualHeight + 24;
                break;
            case "bottom-right":
                targetLeft = workArea.Right - targetWidth + 8;
                targetTop = workArea.Bottom - actualHeight + 24;
                break;
            default:
                return;
        }

        var minLeft = workArea.Left - 8;
        var maxLeft = workArea.Right - targetWidth + 8;
        if (maxLeft < minLeft)
            maxLeft = minLeft;

        var minTop = workArea.Top - 8;
        var maxTop = workArea.Bottom - actualHeight + 24;
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
        var primaryScreen = WinForms.Screen.PrimaryScreen;
        if (primaryScreen != null)
        {
            _settingsManager.Settings.OverlayMaxHeight = Math.Clamp(
                primaryScreen.WorkingArea.Height,
                OverlayMaxHeightMin,
                OverlayMaxHeightMax);
            _settingsManager.Save();
        }
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
        if (!HighlightKeywordEntries.Any(e => string.Equals(e.Keyword, kw, StringComparison.OrdinalIgnoreCase)))
        {
            var entry = new KeywordHighlightEntry(kw, HighlightColor);
            entry.PropertyChanged += (_, _) => QueueSave();
            HighlightKeywordEntries.Add(entry);
            QueueSave();
        }
        NewHighlightKeyword = string.Empty;
    }

    private void RemoveHighlightKeyword(object? parameter)
    {
        KeywordHighlightEntry? entry = parameter switch
        {
            KeywordHighlightEntry e => e,
            string kw => HighlightKeywordEntries.FirstOrDefault(e => e.Keyword == kw),
            _ => null
        };
        if (entry != null)
        {
            HighlightKeywordEntries.Remove(entry);
            QueueSave();
        }
    }

    /// <summary>Called from code-behind after updating a KeywordHighlightEntry color.</summary>
    public void NotifyKeywordColorChanged() => QueueSave();

    private void AddMuteKeyword()
    {
        var kw = NewMuteKeyword?.Trim();
        if (string.IsNullOrWhiteSpace(kw)) return;
        if (!MuteKeywordEntries.Any(e => string.Equals(e.Keyword, kw, StringComparison.OrdinalIgnoreCase)))
        {
            var entry = new MuteKeywordEntry(kw);
            entry.PropertyChanged += (_, _) => QueueSave();
            MuteKeywordEntries.Add(entry);
            QueueSave();
        }
        NewMuteKeyword = string.Empty;
    }

    private void RemoveMuteKeyword(object? parameter)
    {
        MuteKeywordEntry? entry = parameter switch
        {
            MuteKeywordEntry e => e,
            string kw => MuteKeywordEntries.FirstOrDefault(e => e.Keyword == kw),
            _ => null
        };
        if (entry != null)
        {
            MuteKeywordEntries.Remove(entry);
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

        var appNames = _queueManager.SeenAppNames
            .Concat(_settingsManager.Settings.MutedApps)
            .Where(app => !string.IsNullOrWhiteSpace(app))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(app => app, StringComparer.OrdinalIgnoreCase);

        foreach (var app in appNames)
            MutedAppEntries.Add(new MutedAppEntry(app, _queueManager.IsAppMuted(app)));

        RefreshMutedAppFilters();
    }

    private void OnSpokenAppChanged(SpokenAppEntry entry)
    {
        var appName = entry.AppName;
        var updated = _settingsManager.Settings.Clone();
        if (entry.IsReadAloudEnabled)
        {
            updated.SpokenMutedApps.RemoveAll(existing => string.Equals(existing, appName, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            updated.SpokenMutedApps.Add(appName);
        }

        updated.SpokenMutedApps = updated.SpokenMutedApps
            .Where(app => !string.IsNullOrWhiteSpace(app))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(app => app, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _settingsManager.Apply(updated);
        RefreshSpokenAppEntries();
    }

    private bool IsSpokenAppMuted(string appName)
    {
        return _settingsManager.Settings.SpokenMutedApps.Contains(appName, StringComparer.OrdinalIgnoreCase);
    }

    public void RefreshSpokenAppEntries()
    {
        SpokenAppEntries.Clear();

        var appNames = _queueManager.SeenAppNames
            .Concat(_settingsManager.Settings.SpokenMutedApps)
            .Where(app => !string.IsNullOrWhiteSpace(app))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(app => app, StringComparer.OrdinalIgnoreCase);

        foreach (var app in appNames)
            SpokenAppEntries.Add(new SpokenAppEntry(app, !IsSpokenAppMuted(app), OnSpokenAppChanged));
    }

    public void RefreshPerAppConfig()
    {
        PerAppConfigEntries.Clear();
        var s = _settingsManager.Settings;
        var appNames = _queueManager.SeenAppNames
            .Concat(s.PerAppSounds.Keys)
            .Concat(s.PerAppIcons.Keys)
            .Concat(s.PerAppBackgroundImages.Keys)
            .Concat(s.SpokenMutedApps)
            .Where(app => !string.IsNullOrWhiteSpace(app))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(app => app, StringComparer.OrdinalIgnoreCase);

        foreach (var app in appNames)
        {
            s.PerAppSounds.TryGetValue(app, out var sound);
            s.PerAppIcons.TryGetValue(app, out var icon);
            s.PerAppBackgroundImages.TryGetValue(app, out var backgroundImagePath);
            // Migrate old system sound names (pre-registry) to "Default"
            if (sound is "Asterisk" or "Beep" or "Exclamation" or "Hand" or "Question")
                sound = null;
            PerAppConfigEntries.Add(new PerAppConfigEntry(
                app,
                sound ?? "Default",
                icon ?? "Default",
                backgroundImagePath ?? string.Empty,
                !IsSpokenAppMuted(app),
                OnPerAppConfigChanged));
        }

        RefreshPerAppConfigFilters();
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

        if (string.IsNullOrWhiteSpace(entry.BackgroundImagePath))
            updated.PerAppBackgroundImages.Remove(entry.AppName);
        else
            updated.PerAppBackgroundImages[entry.AppName] = entry.BackgroundImagePath;

        updated.SpokenMutedApps.RemoveAll(existing => string.Equals(existing, entry.AppName, StringComparison.OrdinalIgnoreCase));
        if (!entry.IsReadAloudEnabled)
            updated.SpokenMutedApps.Add(entry.AppName);

        updated.SpokenMutedApps = updated.SpokenMutedApps
            .Where(app => !string.IsNullOrWhiteSpace(app))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(app => app, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _settingsManager.Apply(updated);
        RefreshPerAppConfigFilters();
    }

    private void BrowsePerAppBackgroundImage(object? parameter)
    {
        if (parameter is not PerAppConfigEntry entry)
            return;

        var importedPath = ImportBackgroundImage($"Choose a background image for {entry.AppName}");
        if (!string.IsNullOrWhiteSpace(importedPath))
            entry.BackgroundImagePath = importedPath;
    }

    private void ClearPerAppBackgroundImage(object? parameter)
    {
        if (parameter is not PerAppConfigEntry entry)
            return;

        if (string.IsNullOrWhiteSpace(entry.BackgroundImagePath))
            return;

        entry.BackgroundImagePath = string.Empty;
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

    private void RefreshWindowsSounds()
    {
        var none = new Services.SoundService.WindowsSound("None", "None");
        AvailableWindowsSounds.Clear();
        AvailableWindowsSounds.Add(none);
        foreach (var s in Services.SoundService.GetWindowsSounds())
            AvailableWindowsSounds.Add(s);

        // Re-add any custom WAV entries that are in settings but not from registry
        var knownPaths = AvailableWindowsSounds.Select(s => s.WavPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(_defaultSound) && _defaultSound != "None" && !knownPaths.Contains(_defaultSound))
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(_defaultSound);
            AvailableWindowsSounds.Add(new Services.SoundService.WindowsSound(name, _defaultSound));
        }

        PerAppSoundOptions.Clear();
        PerAppSoundOptions.Add(new Services.SoundService.WindowsSound("Default", "Default"));
        foreach (var s in AvailableWindowsSounds)
            PerAppSoundOptions.Add(s);

        // Sync SelectedWindowsSound to current DefaultSound
        _selectedWindowsSound = AvailableWindowsSounds.FirstOrDefault(
            s => string.Equals(s.WavPath, _defaultSound, StringComparison.OrdinalIgnoreCase));
        OnPropertyChanged(nameof(SelectedWindowsSound));
    }

    private void RefreshNarrationVoices()
    {
        AvailableNarrationVoices.Clear();
        foreach (var voice in SpokenNotificationService.GetInstalledVoices())
            AvailableNarrationVoices.Add(voice);

        if (!string.IsNullOrWhiteSpace(_readNotificationsAloudVoiceId)
            && !AvailableNarrationVoices.Any(voice => string.Equals(voice.Id, _readNotificationsAloudVoiceId, StringComparison.Ordinal)))
        {
            _readNotificationsAloudVoiceId = string.Empty;
            OnPropertyChanged(nameof(ReadNotificationsAloudVoiceId));
        }
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

    private async void TestNarration()
    {
        if (IsNarrationPreviewInProgress)
            return;

        _saveDebounce.Stop();
        SaveSettings();
        IsNarrationPreviewInProgress = true;

        try
        {
            await SpokenNotificationService.PlayPreviewAsync(_settingsManager.Settings);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Could not play the narration preview.\n\n{ex.Message}",
                "Spoken Notifications",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsNarrationPreviewInProgress = false;
        }
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
        if (!AvailableWindowsSounds.Any(s => string.Equals(s.WavPath, destPath, StringComparison.OrdinalIgnoreCase)))
        {
            var ws = new Services.SoundService.WindowsSound(displayName, destPath);
            AvailableWindowsSounds.Add(ws);
            PerAppSoundOptions.Add(ws);
        }

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

    private void ApplyStreamingPreset()
    {
        ChromaKeyEnabled = true;
        ChromaKeyColor = "#00FF00";
        ObsFixedWindowMode = true;
        PerAppTintEnabled = true;
        QueueSave();
    }

    private void ViewSessionArchive()
    {
        var archive = _queueManager.SessionArchive;
        if (archive.Count == 0)
        {
            System.Windows.MessageBox.Show("No archived notifications yet.\n\nNotifications are archived while the app is running when Session Archive is enabled.",
                "Session Archive", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var lines = archive
            .AsEnumerable().Reverse()
            .Select(a =>
            {
                var elapsed = DateTime.Now - a.ReceivedAt;
                var timeStr = elapsed.TotalMinutes < 1 ? "just now"
                    : elapsed.TotalMinutes < 60 ? $"{(int)elapsed.TotalMinutes}m ago"
                    : $"{(int)elapsed.TotalHours}h ago";
                var parts = new[] { a.AppName, a.Title, a.Body }
                    .Where(s => !string.IsNullOrWhiteSpace(s));
                return $"[{timeStr}] {string.Join(" — ", parts)}";
            });
        var text = string.Join("\n", lines);
        System.Windows.Clipboard.SetText(text);
        System.Windows.MessageBox.Show($"{archive.Count} archived notification(s) copied to clipboard.\n\nThis data exists only in RAM and will be cleared when the app closes.",
            "Session Archive", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OpenNotificationAccessSettings()
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:privacy-notifications") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Could not open Windows notification access settings.\n\n{ex.Message}",
                "Notification Access",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void RetryNotificationAccess()
    {
        if (_retryNotificationAccessAsync == null)
            return;

        try
        {
            await _retryNotificationAccessAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Could not retry the notification access check.\n\n{ex.Message}",
                "Notification Access",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
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
    private const string DefaultOption = "Default";

    public string AppName { get; }
    private readonly Action<PerAppConfigEntry>? _onChanged;

    private string _sound;
    public string Sound
    {
        get => _sound;
        set
        {
            if (!SetProperty(ref _sound, value)) return;
            OnPropertyChanged(nameof(HasOverrides));
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
            OnPropertyChanged(nameof(HasOverrides));
            _onChanged?.Invoke(this);
        }
    }

    private string _backgroundImagePath;
    public string BackgroundImagePath
    {
        get => _backgroundImagePath;
        set
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (!SetProperty(ref _backgroundImagePath, normalized))
                return;

            OnPropertyChanged(nameof(BackgroundImageDisplay));
            OnPropertyChanged(nameof(BackgroundImageToolTip));
            OnPropertyChanged(nameof(HasOverrides));
            _onChanged?.Invoke(this);
        }
    }

    private bool _isReadAloudEnabled;
    public bool IsReadAloudEnabled
    {
        get => _isReadAloudEnabled;
        set
        {
            if (!SetProperty(ref _isReadAloudEnabled, value))
                return;

            OnPropertyChanged(nameof(HasOverrides));
            _onChanged?.Invoke(this);
        }
    }

    public string BackgroundImageDisplay => string.IsNullOrWhiteSpace(BackgroundImagePath)
        ? "Uses global card background"
        : Path.GetFileName(BackgroundImagePath);

    public string BackgroundImageToolTip => string.IsNullOrWhiteSpace(BackgroundImagePath)
        ? "This app uses the global card background from Appearance."
        : BackgroundImagePath;

    public bool HasOverrides =>
        !string.Equals(Sound, DefaultOption, StringComparison.OrdinalIgnoreCase)
        || !string.Equals(Icon, DefaultOption, StringComparison.OrdinalIgnoreCase)
        || !string.IsNullOrWhiteSpace(BackgroundImagePath)
        || !IsReadAloudEnabled;

    public void ApplyDefaults()
    {
        _sound = DefaultOption;
        _icon = DefaultOption;
        _backgroundImagePath = string.Empty;
        _isReadAloudEnabled = true;

        OnPropertyChanged(nameof(Sound));
        OnPropertyChanged(nameof(Icon));
        OnPropertyChanged(nameof(BackgroundImagePath));
        OnPropertyChanged(nameof(BackgroundImageDisplay));
        OnPropertyChanged(nameof(BackgroundImageToolTip));
        OnPropertyChanged(nameof(IsReadAloudEnabled));
        OnPropertyChanged(nameof(HasOverrides));
    }

    public PerAppConfigEntry(
        string appName,
        string sound,
        string icon,
        string backgroundImagePath,
        bool isReadAloudEnabled,
        Action<PerAppConfigEntry>? onChanged = null)
    {
        AppName = appName;
        _sound = sound;
        _icon = icon;
        _backgroundImagePath = backgroundImagePath;
        _isReadAloudEnabled = isReadAloudEnabled;
        _onChanged = onChanged;
    }
}

public class SpokenAppEntry : BaseViewModel
{
    public string AppName { get; }
    private readonly Action<SpokenAppEntry>? _onChanged;

    private bool _isReadAloudEnabled;
    public bool IsReadAloudEnabled
    {
        get => _isReadAloudEnabled;
        set
        {
            if (!SetProperty(ref _isReadAloudEnabled, value))
                return;

            _onChanged?.Invoke(this);
        }
    }

    public SpokenAppEntry(string appName, bool isReadAloudEnabled, Action<SpokenAppEntry>? onChanged = null)
    {
        AppName = appName;
        _isReadAloudEnabled = isReadAloudEnabled;
        _onChanged = onChanged;
    }
}
