using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using NotificationsPro.Models;
using NotificationsPro.Services;

namespace NotificationsPro.ViewModels;

public class OverlayViewModel : BaseViewModel
{
    private readonly QueueManager _queueManager;
    private readonly SettingsManager _settingsManager;

    public ReadOnlyObservableCollection<NotificationItem> Notifications => _queueManager.VisibleNotifications;
    public QueueManager Queue => _queueManager;

    // Typography — shared
    private string _fontFamily = "Segoe UI";
    public string FontFamily { get => _fontFamily; set => SetProperty(ref _fontFamily, value); }

    private double _lineSpacing = 1.5;
    public double LineSpacing { get => _lineSpacing; set => SetProperty(ref _lineSpacing, value); }

    // Typography — body
    private double _fontSize = 14;
    public double FontSize { get => _fontSize; set => SetProperty(ref _fontSize, value); }

    private string _fontWeight = "Normal";
    public string FontWeight { get => _fontWeight; set => SetProperty(ref _fontWeight, value); }

    // Typography — app name
    private double _appNameFontSize = 14;
    public double AppNameFontSize { get => _appNameFontSize; set => SetProperty(ref _appNameFontSize, value); }

    private string _appNameFontWeight = "SemiBold";
    public string AppNameFontWeight { get => _appNameFontWeight; set => SetProperty(ref _appNameFontWeight, value); }

    // Typography — title
    private double _titleFontSize = 16;
    public double TitleFontSize { get => _titleFontSize; set => SetProperty(ref _titleFontSize, value); }

    private string _titleFontWeight = "SemiBold";
    public string TitleFontWeight { get => _titleFontWeight; set => SetProperty(ref _titleFontWeight, value); }

    // Colors
    private string _textColor = "#E4E4EF";
    public string TextColor { get => _textColor; set => SetProperty(ref _textColor, value); }

    private string _titleColor = "#FFFFFF";
    public string TitleColor { get => _titleColor; set => SetProperty(ref _titleColor, value); }

    private string _appNameColor = "#B8B8CC";
    public string AppNameColor { get => _appNameColor; set => SetProperty(ref _appNameColor, value); }

    private string _backgroundColor = "#1E1E2E";
    public string BackgroundColor { get => _backgroundColor; set => SetProperty(ref _backgroundColor, value); }

    private double _backgroundOpacity = 0.92;
    public double BackgroundOpacity { get => _backgroundOpacity; set => SetProperty(ref _backgroundOpacity, value); }

    private string _accentColor = "#7C5CFC";
    public string AccentColor { get => _accentColor; set => SetProperty(ref _accentColor, value); }

    private string _highlightColor = "#FFD700";
    public string HighlightColor { get => _highlightColor; set => SetProperty(ref _highlightColor, value); }

    // Card shape
    private double _cornerRadius = 12;
    public double CornerRadius { get => _cornerRadius; set => SetProperty(ref _cornerRadius, value); }

    private double _padding = 16;
    public double Padding { get => _padding; set => SetProperty(ref _padding, value); }

    private double _cardGap = 8;
    public double CardGap
    {
        get => _cardGap;
        set
        {
            if (!SetProperty(ref _cardGap, value)) return;
            OnPropertyChanged(nameof(CardMargin));
        }
    }

    private double _outerMargin = 4;
    public double OuterMargin
    {
        get => _outerMargin;
        set
        {
            if (!SetProperty(ref _outerMargin, value)) return;
            OnPropertyChanged(nameof(OuterContentMargin));
        }
    }

    private bool _showAccent = true;
    public bool ShowAccent
    {
        get => _showAccent;
        set
        {
            if (!SetProperty(ref _showAccent, value)) return;
            OnPropertyChanged(nameof(AccentBorderThickness));
        }
    }

    private double _accentThickness = 3;
    public double AccentThickness
    {
        get => _accentThickness;
        set
        {
            if (!SetProperty(ref _accentThickness, value)) return;
            OnPropertyChanged(nameof(AccentBorderThickness));
        }
    }

    private bool _showBorder;
    public bool ShowBorder
    {
        get => _showBorder;
        set
        {
            if (!SetProperty(ref _showBorder, value)) return;
            OnPropertyChanged(nameof(CardBorderThickness));
        }
    }

    private string _borderColor = "#363650";
    public string BorderColor { get => _borderColor; set => SetProperty(ref _borderColor, value); }

    private double _borderThickness = 1;
    public double BorderThickness
    {
        get => _borderThickness;
        set
        {
            if (!SetProperty(ref _borderThickness, value)) return;
            OnPropertyChanged(nameof(CardBorderThickness));
        }
    }

    // Window
    private bool _alwaysOnTop = true;
    public bool AlwaysOnTop { get => _alwaysOnTop; set => SetProperty(ref _alwaysOnTop, value); }

    private bool _animationsEnabled = true;
    public bool AnimationsEnabled
    {
        get => _animationsEnabled;
        set
        {
            if (!SetProperty(ref _animationsEnabled, value)) return;
            OnPropertyChanged(nameof(EnterOffset));
            OnPropertyChanged(nameof(ExitOffset));
            OnPropertyChanged(nameof(EntryMotionDuration));
            OnPropertyChanged(nameof(EntryFadeDuration));
            OnPropertyChanged(nameof(ExitMotionDuration));
            OnPropertyChanged(nameof(ExitFadeDuration));
        }
    }

    private string _slideInDirection = "Left";
    public string SlideInDirection { get => _slideInDirection; set => SetProperty(ref _slideInDirection, value); }

    private bool _fadeOnlyAnimation;
    public bool FadeOnlyAnimation
    {
        get => _fadeOnlyAnimation;
        set
        {
            if (!SetProperty(ref _fadeOnlyAnimation, value)) return;
            OnPropertyChanged(nameof(EnterOffset));
            OnPropertyChanged(nameof(ExitOffset));
        }
    }

    private double _animationDurationMs = 300;
    public double AnimationDurationMs
    {
        get => _animationDurationMs;
        set
        {
            if (!SetProperty(ref _animationDurationMs, value)) return;
            OnPropertyChanged(nameof(EntryMotionDuration));
            OnPropertyChanged(nameof(EntryFadeDuration));
            OnPropertyChanged(nameof(ExitMotionDuration));
            OnPropertyChanged(nameof(ExitFadeDuration));
        }
    }

    private double _overlayWidth = 380;
    public double OverlayWidth
    {
        get => _overlayWidth;
        set
        {
            if (!SetProperty(ref _overlayWidth, value)) return;
            OnPropertyChanged(nameof(EnterOffset));
        }
    }

    private double _overlayMaxHeight = 600;
    public double OverlayMaxHeight { get => _overlayMaxHeight; set => SetProperty(ref _overlayMaxHeight, value); }

    // Content
    private bool _showAppName = true;
    public bool ShowAppName { get => _showAppName; set => SetProperty(ref _showAppName, value); }

    private bool _showNotificationTitle = true;
    public bool ShowNotificationTitle { get => _showNotificationTitle; set => SetProperty(ref _showNotificationTitle, value); }

    private bool _showNotificationBody = true;
    public bool ShowNotificationBody { get => _showNotificationBody; set => SetProperty(ref _showNotificationBody, value); }

    private bool _limitTextLines;
    public bool LimitTextLines
    {
        get => _limitTextLines;
        set
        {
            if (!SetProperty(ref _limitTextLines, value)) return;
            OnPropertyChanged(nameof(AppNameEffectiveMaxHeight));
            OnPropertyChanged(nameof(TitleEffectiveMaxHeight));
            OnPropertyChanged(nameof(BodyEffectiveMaxHeight));
        }
    }

    private int _maxAppNameLines = 2;
    public int MaxAppNameLines
    {
        get => _maxAppNameLines;
        set
        {
            if (!SetProperty(ref _maxAppNameLines, Math.Max(1, value))) return;
            OnPropertyChanged(nameof(AppNameMaxHeight));
            OnPropertyChanged(nameof(AppNameEffectiveMaxHeight));
        }
    }

    private int _maxTitleLines = 2;
    public int MaxTitleLines
    {
        get => _maxTitleLines;
        set
        {
            if (!SetProperty(ref _maxTitleLines, Math.Max(1, value))) return;
            OnPropertyChanged(nameof(TitleMaxHeight));
            OnPropertyChanged(nameof(TitleEffectiveMaxHeight));
        }
    }

    private int _maxBodyLines = 4;
    public int MaxBodyLines
    {
        get => _maxBodyLines;
        set
        {
            if (!SetProperty(ref _maxBodyLines, Math.Max(1, value))) return;
            OnPropertyChanged(nameof(BodyMaxHeight));
            OnPropertyChanged(nameof(BodyEffectiveMaxHeight));
        }
    }

    // Display mode
    private bool _singleLineMode;
    public bool SingleLineMode
    {
        get => _singleLineMode;
        set
        {
            if (!SetProperty(ref _singleLineMode, value)) return;
            OnPropertyChanged(nameof(StackedContentVisibility));
            OnPropertyChanged(nameof(SingleLineContentVisibility));
        }
    }

    private bool _singleLineWrapText;
    public bool SingleLineWrapText
    {
        get => _singleLineWrapText;
        set
        {
            if (!SetProperty(ref _singleLineWrapText, value)) return;
            OnPropertyChanged(nameof(SingleLineCompactVisibility));
            OnPropertyChanged(nameof(SingleLineWrappedVisibility));
        }
    }

    private int _singleLineMaxLines = 3;
    public int SingleLineMaxLines
    {
        get => _singleLineMaxLines;
        set
        {
            if (!SetProperty(ref _singleLineMaxLines, Math.Max(1, value))) return;
            OnPropertyChanged(nameof(SingleLineWrappedMaxHeight));
        }
    }

    private bool _showTimestamp;
    public bool ShowTimestamp
    {
        get => _showTimestamp;
        set
        {
            if (!SetProperty(ref _showTimestamp, value)) return;
            OnPropertyChanged(nameof(TimestampVisibility));
        }
    }
    public Visibility TimestampVisibility => ShowTimestamp ? Visibility.Visible : Visibility.Collapsed;

    private bool _singleLineAutoFullWidth;
    public bool SingleLineAutoFullWidth { get => _singleLineAutoFullWidth; set => SetProperty(ref _singleLineAutoFullWidth, value); }

    // Computed — visibility
    public Visibility StackedContentVisibility => SingleLineMode ? Visibility.Collapsed : Visibility.Visible;
    public Visibility SingleLineContentVisibility => SingleLineMode ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SingleLineCompactVisibility => SingleLineWrapText ? Visibility.Collapsed : Visibility.Visible;
    public Visibility SingleLineWrappedVisibility => SingleLineWrapText ? Visibility.Visible : Visibility.Collapsed;

    // Computed — line heights (per-field)
    public double AppNameLineHeight => AppNameFontSize * LineSpacing;
    public double TitleLineHeight => TitleFontSize * LineSpacing;
    public double BodyLineHeight => FontSize * LineSpacing;

    // Computed — max heights for line clamping
    public double AppNameMaxHeight => Math.Max(4, MaxAppNameLines * AppNameLineHeight);
    public double TitleMaxHeight => Math.Max(4, MaxTitleLines * TitleLineHeight);
    public double BodyMaxHeight => Math.Max(4, MaxBodyLines * BodyLineHeight);
    public double AppNameEffectiveMaxHeight => LimitTextLines ? AppNameMaxHeight : double.PositiveInfinity;
    public double TitleEffectiveMaxHeight => LimitTextLines ? TitleMaxHeight : double.PositiveInfinity;
    public double BodyEffectiveMaxHeight => LimitTextLines ? BodyMaxHeight : double.PositiveInfinity;
    public double SingleLineWrappedMaxHeight => Math.Max(4, SingleLineMaxLines * BodyLineHeight);

    // Computed — layout
    public Thickness CardMargin => new Thickness(0, 0, 0, CardGap);
    public Thickness OuterContentMargin => new Thickness(OuterMargin);
    public Thickness AccentBorderThickness => ShowAccent
        ? new Thickness(AccentThickness, 0, 0, 0)
        : new Thickness(0);
    public Thickness CardBorderThickness => ShowBorder
        ? new Thickness(BorderThickness)
        : new Thickness(0);

    // Computed — animations
    public double EnterOffset => AnimationsEnabled && !FadeOnlyAnimation ? -(OverlayWidth + 40) : 0;
    public double ExitOffset => AnimationsEnabled && !FadeOnlyAnimation ? 50 : 0;
    public Duration EntryMotionDuration => DurationFor(AnimationDurationMs);
    public Duration EntryFadeDuration => DurationFor(AnimationDurationMs * 0.75);
    public Duration ExitMotionDuration => DurationFor(AnimationDurationMs);
    public Duration ExitFadeDuration => DurationFor(AnimationDurationMs);

    // Streaming (M10)
    private bool _chromaKeyEnabled;
    public bool ChromaKeyEnabled { get => _chromaKeyEnabled; set => SetProperty(ref _chromaKeyEnabled, value); }

    private string _chromaKeyColor = "#00FF00";
    public string ChromaKeyColor { get => _chromaKeyColor; set => SetProperty(ref _chromaKeyColor, value); }

    private bool _perAppTintEnabled;
    public bool PerAppTintEnabled { get => _perAppTintEnabled; set => SetProperty(ref _perAppTintEnabled, value); }

    private double _perAppTintOpacity = 0.15;
    public double PerAppTintOpacity { get => _perAppTintOpacity; set => SetProperty(ref _perAppTintOpacity, value); }

    // Empty state ghost card visibility
    private Visibility _emptyStateVisibility = Visibility.Visible;
    public Visibility EmptyStateVisibility { get => _emptyStateVisibility; set => SetProperty(ref _emptyStateVisibility, value); }

    public OverlayViewModel(QueueManager queueManager, SettingsManager settingsManager)
    {
        _queueManager = queueManager;
        _settingsManager = settingsManager;

        ApplySettings(_settingsManager.Settings);
        _settingsManager.SettingsChanged += () => ApplySettings(_settingsManager.Settings);

        // Track empty state
        ((System.Collections.Specialized.INotifyCollectionChanged)_queueManager.VisibleNotifications)
            .CollectionChanged += (_, _) => UpdateEmptyState();
        UpdateEmptyState();

        // Refresh relative timestamps every 15 seconds
        var timestampTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        timestampTimer.Tick += (_, _) =>
        {
            if (!ShowTimestamp) return;
            foreach (var n in _queueManager.VisibleNotifications)
                n.NotifyTimestampChanged();
        };
        timestampTimer.Start();
    }

    public void ApplySettings(AppSettings s)
    {
        FontFamily = s.FontFamily;
        FontSize = s.FontSize;
        FontWeight = s.FontWeight;
        AppNameFontSize = s.AppNameFontSize;
        AppNameFontWeight = s.AppNameFontWeight;
        TitleFontSize = s.TitleFontSize;
        TitleFontWeight = s.TitleFontWeight;
        LineSpacing = s.LineSpacing;

        // Apply text scaling from system if enabled
        if (s.RespectTextScaling)
        {
            var scale = SystemParameters.CaretWidth; // Trigger property access
            try
            {
                // Use the DPI-based text scale factor from the registry
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Accessibility");
                if (key?.GetValue("TextScaleFactor") is int scaleFactor && scaleFactor > 100)
                {
                    var factor = scaleFactor / 100.0;
                    FontSize = s.FontSize * factor;
                    AppNameFontSize = s.AppNameFontSize * factor;
                    TitleFontSize = s.TitleFontSize * factor;
                }
            }
            catch { /* Registry unavailable — use base sizes */ }
        }

        TextColor = s.TextColor;
        TitleColor = s.TitleColor;
        AppNameColor = s.AppNameColor;
        BackgroundColor = s.BackgroundColor;
        BackgroundOpacity = s.BackgroundOpacity;
        AccentColor = s.AccentColor;
        HighlightColor = s.HighlightColor;
        CornerRadius = s.CornerRadius;
        Padding = s.Padding;
        CardGap = s.CardGap;
        OuterMargin = s.OuterMargin;
        ShowAccent = s.ShowAccent;
        AccentThickness = s.AccentThickness;
        ShowBorder = s.ShowBorder;
        BorderColor = s.BorderColor;
        BorderThickness = s.BorderThickness;
        AlwaysOnTop = s.AlwaysOnTop;

        // Override animations when system "Reduce Motion" is active
        var reduceMotion = s.RespectReduceMotion && !SystemParameters.ClientAreaAnimation;
        AnimationsEnabled = reduceMotion ? false : s.AnimationsEnabled;
        FadeOnlyAnimation = reduceMotion ? true : s.FadeOnlyAnimation;

        SlideInDirection = s.SlideInDirection;
        AnimationDurationMs = s.AnimationDurationMs;
        OverlayWidth = s.OverlayWidth;
        OverlayMaxHeight = s.OverlayMaxHeight;
        ShowAppName = s.ShowAppName;
        ShowNotificationTitle = s.ShowNotificationTitle;
        ShowNotificationBody = s.ShowNotificationBody;
        LimitTextLines = s.LimitTextLines;
        MaxAppNameLines = s.MaxAppNameLines;
        MaxTitleLines = s.MaxTitleLines;
        MaxBodyLines = s.MaxBodyLines;
        SingleLineMode = s.SingleLineMode;
        SingleLineWrapText = s.SingleLineWrapText;
        SingleLineMaxLines = Math.Max(1, s.SingleLineMaxLines);
        SingleLineAutoFullWidth = s.SingleLineAutoFullWidth;
        ShowTimestamp = s.ShowTimestamp;
        ChromaKeyEnabled = s.ChromaKeyEnabled;
        ChromaKeyColor = s.ChromaKeyColor;
        PerAppTintEnabled = s.PerAppTintEnabled;
        PerAppTintOpacity = s.PerAppTintOpacity;

        // Notify all computed properties
        OnPropertyChanged(nameof(AppNameLineHeight));
        OnPropertyChanged(nameof(TitleLineHeight));
        OnPropertyChanged(nameof(BodyLineHeight));
        OnPropertyChanged(nameof(AppNameMaxHeight));
        OnPropertyChanged(nameof(TitleMaxHeight));
        OnPropertyChanged(nameof(BodyMaxHeight));
        OnPropertyChanged(nameof(AppNameEffectiveMaxHeight));
        OnPropertyChanged(nameof(TitleEffectiveMaxHeight));
        OnPropertyChanged(nameof(BodyEffectiveMaxHeight));
        OnPropertyChanged(nameof(SingleLineWrappedMaxHeight));
        OnPropertyChanged(nameof(CardMargin));
        OnPropertyChanged(nameof(OuterContentMargin));
        OnPropertyChanged(nameof(AccentBorderThickness));
        OnPropertyChanged(nameof(CardBorderThickness));
        OnPropertyChanged(nameof(StackedContentVisibility));
        OnPropertyChanged(nameof(SingleLineContentVisibility));
        OnPropertyChanged(nameof(SingleLineCompactVisibility));
        OnPropertyChanged(nameof(SingleLineWrappedVisibility));
        OnPropertyChanged(nameof(TimestampVisibility));
        OnPropertyChanged(nameof(EnterOffset));
        OnPropertyChanged(nameof(ExitOffset));
        OnPropertyChanged(nameof(EntryMotionDuration));
        OnPropertyChanged(nameof(EntryFadeDuration));
        OnPropertyChanged(nameof(ExitMotionDuration));
        OnPropertyChanged(nameof(ExitFadeDuration));
    }

    private void UpdateEmptyState()
    {
        EmptyStateVisibility = _queueManager.VisibleNotifications.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private static Duration DurationFor(double ms)
    {
        if (ms <= 0)
            return new Duration(TimeSpan.Zero);
        return new Duration(TimeSpan.FromMilliseconds(ms));
    }
}
