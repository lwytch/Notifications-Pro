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

    // Appearance
    private string _fontFamily = "Segoe UI";
    public string FontFamily { get => _fontFamily; set { if (SetProperty(ref _fontFamily, value)) QueueSave(); } }

    private double _fontSize = 14;
    public double FontSize { get => _fontSize; set { if (SetProperty(ref _fontSize, value)) QueueSave(); } }

    private string _fontWeight = "Normal";
    public string FontWeight { get => _fontWeight; set { if (SetProperty(ref _fontWeight, value)) QueueSave(); } }

    private double _lineSpacing = 1.5;
    public double LineSpacing { get => _lineSpacing; set { if (SetProperty(ref _lineSpacing, value)) QueueSave(); } }

    private string _textColor = "#E4E4EF";
    public string TextColor { get => _textColor; set { if (SetProperty(ref _textColor, value)) QueueSave(); } }

    private string _titleColor = "#FFFFFF";
    public string TitleColor { get => _titleColor; set { if (SetProperty(ref _titleColor, value)) QueueSave(); } }

    private string _appNameColor = "#B8B8CC";
    public string AppNameColor { get => _appNameColor; set { if (SetProperty(ref _appNameColor, value)) QueueSave(); } }

    private string _backgroundColor = "#1E1E2E";
    public string BackgroundColor { get => _backgroundColor; set { if (SetProperty(ref _backgroundColor, value)) QueueSave(); } }

    private double _backgroundOpacity = 0.92;
    public double BackgroundOpacity { get => _backgroundOpacity; set { if (SetProperty(ref _backgroundOpacity, value)) QueueSave(); } }

    private double _cornerRadius = 12;
    public double CornerRadius { get => _cornerRadius; set { if (SetProperty(ref _cornerRadius, value)) QueueSave(); } }

    private double _padding = 16;
    public double Padding { get => _padding; set { if (SetProperty(ref _padding, value)) QueueSave(); } }

    private bool _showBorder = true;
    public bool ShowBorder { get => _showBorder; set { if (SetProperty(ref _showBorder, value)) QueueSave(); } }

    private string _borderColor = "#7C5CFC";
    public string BorderColor { get => _borderColor; set { if (SetProperty(ref _borderColor, value)) QueueSave(); } }

    private double _borderThickness = 1;
    public double BorderThickness { get => _borderThickness; set { if (SetProperty(ref _borderThickness, value)) QueueSave(); } }

    private string _accentColor = "#7C5CFC";
    public string AccentColor { get => _accentColor; set { if (SetProperty(ref _accentColor, value)) QueueSave(); } }

    // Behavior
    private double _notificationDuration = 5;
    public double NotificationDuration { get => _notificationDuration; set { if (SetProperty(ref _notificationDuration, value)) QueueSave(); } }

    private int _maxVisibleNotifications = 3;
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

    private double _animationDurationMs = 300;
    public double AnimationDurationMs { get => _animationDurationMs; set { if (SetProperty(ref _animationDurationMs, value)) QueueSave(); } }

    // Position
    private double _overlayWidth = 380;
    public double OverlayWidth
    {
        get => _overlayWidth;
        set
        {
            if (!SetProperty(ref _overlayWidth, value)) return;
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

    // Collections
    public List<string> AvailableFonts { get; }
    public List<string> AvailableFontWeights { get; } = new()
    {
        "Thin", "Light", "Normal", "Medium", "SemiBold", "Bold"
    };

    // Commands
    public ICommand PreviewNotificationCommand { get; }
    public ICommand ResetToDefaultsCommand { get; }
    public ICommand MoveOverlayPresetCommand { get; }
    public ICommand SetOverlayHeightPresetCommand { get; }
    public ImageSource TrayIconImage { get; }

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
        SetOverlayHeightPresetCommand = new RelayCommand(SetOverlayHeightPreset);
        TrayIconImage = IconHelper.CreateTrayIconImageSource(32);

        LoadFromSettings();
    }

    private void LoadFromSettings()
    {
        var s = _settingsManager.Settings;
        _fontFamily = s.FontFamily;
        _fontSize = s.FontSize;
        _fontWeight = s.FontWeight;
        _lineSpacing = s.LineSpacing;
        _textColor = s.TextColor;
        _titleColor = s.TitleColor;
        _appNameColor = s.AppNameColor;
        _backgroundColor = s.BackgroundColor;
        _backgroundOpacity = s.BackgroundOpacity;
        _cornerRadius = s.CornerRadius;
        _padding = s.Padding;
        _showBorder = s.ShowBorder;
        _borderColor = s.BorderColor;
        _borderThickness = s.BorderThickness;
        _accentColor = s.AccentColor;
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
        _newestOnTop = s.NewestOnTop;
        _alwaysOnTop = s.AlwaysOnTop;
        _clickThrough = s.ClickThrough;
        _animationsEnabled = s.AnimationsEnabled;
        _fadeOnlyAnimation = s.FadeOnlyAnimation;
        _animationDurationMs = s.AnimationDurationMs;
        _overlayWidth = s.OverlayWidth;
        _overlayMaxHeight = Math.Clamp(s.OverlayMaxHeight, OverlayMaxHeightMin, OverlayMaxHeightMax);
        _allowManualResize = s.AllowManualResize;
        _snapToEdges = s.SnapToEdges;
        _snapDistance = s.SnapDistance;
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
        var savedManualWidth = Math.Max(220,
            previousSettings.LastManualOverlayWidth > 0
                ? previousSettings.LastManualOverlayWidth
                : previousSettings.OverlayWidth);

        var resolvedOverlayWidth = _overlayWidthDirty
            ? OverlayWidth
            : previousSettings.OverlayWidth;

        if (previousAutoFullWidth && !nextAutoFullWidth)
            resolvedOverlayWidth = savedManualWidth;

        resolvedOverlayWidth = Math.Max(220, resolvedOverlayWidth);
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
            LineSpacing = LineSpacing,
            TextColor = TextColor,
            TitleColor = TitleColor,
            AppNameColor = AppNameColor,
            BackgroundColor = BackgroundColor,
            BackgroundOpacity = BackgroundOpacity,
            CornerRadius = CornerRadius,
            Padding = Padding,
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
            NewestOnTop = NewestOnTop,
            AlwaysOnTop = AlwaysOnTop,
            ClickThrough = ClickThrough,
            AnimationsEnabled = AnimationsEnabled,
            FadeOnlyAnimation = FadeOnlyAnimation,
            AnimationDurationMs = Math.Max(0, AnimationDurationMs),
            OverlayWidth = resolvedOverlayWidth,
            LastManualOverlayWidth = Math.Max(220, nextLastManualWidth),
            OverlayMaxHeight = Math.Clamp(OverlayMaxHeight, OverlayMaxHeightMin, OverlayMaxHeightMax),
            AllowManualResize = AllowManualResize,
            SnapToEdges = SnapToEdges,
            SnapDistance = SnapDistance,
            // Preserve position from current settings
            OverlayLeft = previousSettings.OverlayLeft,
            OverlayTop = previousSettings.OverlayTop,
            MonitorIndex = previousSettings.MonitorIndex,
            OverlayVisible = previousSettings.OverlayVisible,
            NotificationsPaused = previousSettings.NotificationsPaused,
        };

        _settingsManager.Apply(s);
        _overlayWidthDirty = false;
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
        var workArea = GetWorkAreaForMonitor(updated.MonitorIndex);
        const double margin = 16;

        var targetWidth = Math.Max(220, updated.OverlayWidth);
        if (updated.SingleLineMode && updated.SingleLineAutoFullWidth)
            targetWidth = Math.Max(220, workArea.Width - (margin * 2));

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

    private void ResetToDefaults()
    {
        _settingsManager.ResetToDefaults();
        LoadFromSettings();

        // Notify all properties changed
        var props = GetType().GetProperties();
        foreach (var prop in props)
            OnPropertyChanged(prop.Name);
    }
}
