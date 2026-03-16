using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using NotificationsPro.Helpers;
using NotificationsPro.Models;
using NotificationsPro.Services;

namespace NotificationsPro.ViewModels;

public class OverlayViewModel : BaseViewModel
{
    private readonly QueueManager _queueManager;
    private readonly SettingsManager _settingsManager;
    private bool _hasScrollableOverflow;

    public ReadOnlyObservableCollection<NotificationItem> Notifications => _queueManager.VisibleNotifications;
    public QueueManager Queue => _queueManager;

    // Icon service for per-app icon resolution
    public IconService IconService { get; } = new();

    // Current settings snapshot for icon resolution in converters
    private AppSettings? _currentSettings;
    public AppSettings? CurrentSettings { get => _currentSettings; set => SetProperty(ref _currentSettings, value); }

    // Icon display
    private bool _showNotificationIcons;
    public bool ShowNotificationIcons { get => _showNotificationIcons; set => SetProperty(ref _showNotificationIcons, value); }

    private double _iconSize = 24;
    public double IconSize { get => _iconSize; set => SetProperty(ref _iconSize, value); }

    // Typography — shared
    private string _fontFamily = "Segoe UI";
    public string FontFamily { get => _fontFamily; set => SetProperty(ref _fontFamily, value); }

    private double _lineSpacing = 1.5;
    public double LineSpacing { get => _lineSpacing; set => SetProperty(ref _lineSpacing, value); }

    private string _textAlignment = "Left";
    public string TextAlignment { get => _textAlignment; set => SetProperty(ref _textAlignment, value); }

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
    private string _textColor = "#E6E6E6";
    public string TextColor { get => _textColor; set => SetProperty(ref _textColor, value); }

    private string _titleColor = "#FFFFFF";
    public string TitleColor { get => _titleColor; set => SetProperty(ref _titleColor, value); }

    private string _appNameColor = "#C8C8C8";
    public string AppNameColor { get => _appNameColor; set => SetProperty(ref _appNameColor, value); }

    private string _backgroundColor = "#202020";
    public string BackgroundColor { get => _backgroundColor; set => SetProperty(ref _backgroundColor, value); }

    private double _backgroundOpacity = 0.94;
    public double BackgroundOpacity { get => _backgroundOpacity; set => SetProperty(ref _backgroundOpacity, value); }

    private string _cardBackgroundMode = CardBackgroundModeHelper.Solid;
    public string CardBackgroundMode { get => _cardBackgroundMode; set => SetProperty(ref _cardBackgroundMode, value); }

    private string _cardBackgroundImagePath = string.Empty;
    public string CardBackgroundImagePath { get => _cardBackgroundImagePath; set => SetProperty(ref _cardBackgroundImagePath, value); }

    private double _cardBackgroundImageOpacity = 0.45;
    public double CardBackgroundImageOpacity { get => _cardBackgroundImageOpacity; set => SetProperty(ref _cardBackgroundImageOpacity, value); }

    private double _cardBackgroundImageHueDegrees;
    public double CardBackgroundImageHueDegrees { get => _cardBackgroundImageHueDegrees; set => SetProperty(ref _cardBackgroundImageHueDegrees, value); }

    private double _cardBackgroundImageBrightness = 1.0;
    public double CardBackgroundImageBrightness { get => _cardBackgroundImageBrightness; set => SetProperty(ref _cardBackgroundImageBrightness, value); }

    private double _cardBackgroundImageSaturation = 1.0;
    public double CardBackgroundImageSaturation { get => _cardBackgroundImageSaturation; set => SetProperty(ref _cardBackgroundImageSaturation, value); }

    private double _cardBackgroundImageContrast = 1.0;
    public double CardBackgroundImageContrast { get => _cardBackgroundImageContrast; set => SetProperty(ref _cardBackgroundImageContrast, value); }

    private bool _cardBackgroundImageBlackAndWhite;
    public bool CardBackgroundImageBlackAndWhite { get => _cardBackgroundImageBlackAndWhite; set => SetProperty(ref _cardBackgroundImageBlackAndWhite, value); }

    private string _cardBackgroundImageFitMode = "Fill Card";
    public string CardBackgroundImageFitMode { get => _cardBackgroundImageFitMode; set => SetProperty(ref _cardBackgroundImageFitMode, value); }

    private string _cardBackgroundImagePlacement = "Inside Padding";
    public string CardBackgroundImagePlacement { get => _cardBackgroundImagePlacement; set => SetProperty(ref _cardBackgroundImagePlacement, value); }

    private string _cardBackgroundImageVerticalFocus = ImageVerticalFocusHelper.Center;
    public string CardBackgroundImageVerticalFocus { get => _cardBackgroundImageVerticalFocus; set => SetProperty(ref _cardBackgroundImageVerticalFocus, value); }

    private string _accentColor = "#0078D4";
    public string AccentColor { get => _accentColor; set => SetProperty(ref _accentColor, value); }

    private string _highlightColor = "#FFD700";
    public string HighlightColor { get => _highlightColor; set => SetProperty(ref _highlightColor, value); }

    private double _highlightOverlayOpacity = 0.25;
    public double HighlightOverlayOpacity { get => _highlightOverlayOpacity; set => SetProperty(ref _highlightOverlayOpacity, Math.Clamp(value, 0.05, 0.80)); }

    private string _highlightAnimation = HighlightAnimationHelper.None;
    public string HighlightAnimation { get => _highlightAnimation; set => SetProperty(ref _highlightAnimation, HighlightAnimationHelper.Normalize(value)); }

    private string _highlightBorderMode = HighlightBorderModeHelper.FullBorder;
    public string HighlightBorderMode
    {
        get => _highlightBorderMode;
        set
        {
            if (!SetProperty(ref _highlightBorderMode, HighlightBorderModeHelper.Normalize(value))) return;
            OnPropertyChanged(nameof(HighlightCardBorderThickness));
        }
    }

    private double _highlightBorderThickness = 1;
    public double HighlightBorderThickness
    {
        get => _highlightBorderThickness;
        set
        {
            if (!SetProperty(ref _highlightBorderThickness, Math.Clamp(value, 0.5, 8.0))) return;
            OnPropertyChanged(nameof(HighlightCardBorderThickness));
        }
    }

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
            OnPropertyChanged(nameof(HighlightCardBorderThickness));
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

    private string _borderColor = "#3A3A3A";
    public string BorderColor { get => _borderColor; set => SetProperty(ref _borderColor, value); }

    private double _borderThickness = 1;
    public double BorderThickness
    {
        get => _borderThickness;
        set
        {
            if (!SetProperty(ref _borderThickness, value)) return;
            OnPropertyChanged(nameof(CardBorderThickness));
            OnPropertyChanged(nameof(HighlightCardBorderThickness));
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

            OnPropertyChanged(nameof(EnterOffset));
            OnPropertyChanged(nameof(ExitOffset));
        }
    }

    private double _animationDurationMs = 1200;
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

    private string _animationEasing = AnimationEasingHelper.EaseOut;
    public string AnimationEasing { get => _animationEasing; set => SetProperty(ref _animationEasing, AnimationEasingHelper.Normalize(value)); }

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

    // Scrollbar
    private bool _overlayScrollbarVisible;
    public bool OverlayScrollbarVisible
    {
        get => _overlayScrollbarVisible;
        set
        {
            if (!SetProperty(ref _overlayScrollbarVisible, value)) return;
            OnPropertyChanged(nameof(ScrollbarVisibility));
            OnPropertyChanged(nameof(OverlayContentMargin));
        }
    }

    private double _overlayScrollbarWidth = 8;
    public double OverlayScrollbarWidth { get => _overlayScrollbarWidth; set => SetProperty(ref _overlayScrollbarWidth, value); }

    private double _overlayScrollbarOpacity = 1.0;
    public double OverlayScrollbarOpacity { get => _overlayScrollbarOpacity; set => SetProperty(ref _overlayScrollbarOpacity, value); }

    private string _overlayScrollbarTrackColor = "#141414";
    public string OverlayScrollbarTrackColor { get => _overlayScrollbarTrackColor; set => SetProperty(ref _overlayScrollbarTrackColor, value); }

    private string _overlayScrollbarThumbColor = "#4F4F4F";
    public string OverlayScrollbarThumbColor { get => _overlayScrollbarThumbColor; set => SetProperty(ref _overlayScrollbarThumbColor, value); }

    private string _overlayScrollbarThumbHoverColor = "#0078D4";
    public string OverlayScrollbarThumbHoverColor { get => _overlayScrollbarThumbHoverColor; set => SetProperty(ref _overlayScrollbarThumbHoverColor, value); }

    private double _overlayScrollbarPadding = 1.5;
    public double OverlayScrollbarPadding
    {
        get => _overlayScrollbarPadding;
        set
        {
            if (!SetProperty(ref _overlayScrollbarPadding, value)) return;
            OnPropertyChanged(nameof(OverlayScrollbarPaddingThickness));
        }
    }

    private double _overlayScrollbarContentGap = 10;
    public double OverlayScrollbarContentGap
    {
        get => _overlayScrollbarContentGap;
        set
        {
            if (!SetProperty(ref _overlayScrollbarContentGap, value)) return;
            OnPropertyChanged(nameof(OverlayContentMargin));
        }
    }

    private double _overlayScrollbarCornerRadius = 6;
    public double OverlayScrollbarCornerRadius
    {
        get => _overlayScrollbarCornerRadius;
        set
        {
            if (!SetProperty(ref _overlayScrollbarCornerRadius, value)) return;
            OnPropertyChanged(nameof(OverlayScrollbarCornerRadiusValue));
        }
    }

    public System.Windows.Controls.ScrollBarVisibility ScrollbarVisibility =>
        OverlayScrollbarVisible && _hasScrollableOverflow
            ? System.Windows.Controls.ScrollBarVisibility.Visible
            : System.Windows.Controls.ScrollBarVisibility.Hidden;
    public Thickness OverlayScrollbarPaddingThickness => new(OverlayScrollbarPadding);
    public Thickness OverlayContentMargin => ScrollbarVisibility == System.Windows.Controls.ScrollBarVisibility.Visible
        ? new Thickness(0, 0, OverlayScrollbarContentGap, 0)
        : new Thickness(0);
    public CornerRadius OverlayScrollbarCornerRadiusValue => new(OverlayScrollbarCornerRadius);

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

    private double _timestampFontSize = 11;
    public double TimestampFontSize { get => _timestampFontSize; set => SetProperty(ref _timestampFontSize, Math.Clamp(value, 8, 32)); }

    private string _timestampDisplayMode = "Relative";
    public string TimestampDisplayMode { get => _timestampDisplayMode; set => SetProperty(ref _timestampDisplayMode, value); }

    private string _timestampFontWeight = "Normal";
    public string TimestampFontWeight { get => _timestampFontWeight; set => SetProperty(ref _timestampFontWeight, value); }

    private string _timestampColor = "#C8C8C8";
    public string TimestampColor { get => _timestampColor; set => SetProperty(ref _timestampColor, value); }

    private string _voiceAccessReadMode = VoiceAccessTextFormatter.ModeOff;
    public string VoiceAccessReadMode { get => _voiceAccessReadMode; set => SetProperty(ref _voiceAccessReadMode, value); }

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
    public Thickness HighlightCardBorderThickness => HighlightBorderMode switch
    {
        HighlightBorderModeHelper.NoBorder => new Thickness(0),
        HighlightBorderModeHelper.AccentSideOnly => new Thickness(0, HighlightBorderThickness, HighlightBorderThickness, HighlightBorderThickness),
        _ => new Thickness(HighlightBorderThickness)
    };

    // Computed — animations
    public double EnterOffset => AnimationsEnabled && NotificationAnimationStyleHelper.UsesDirection(NotificationAnimationStyle) ? -(OverlayWidth + 40) : 0;
    public double ExitOffset => AnimationsEnabled && NotificationAnimationStyleHelper.UsesDirection(NotificationAnimationStyle) ? 50 : 0;
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

    // Fullscreen overlay mode (M9.5)
    private bool _fullscreenOverlayMode;
    public bool FullscreenOverlayMode { get => _fullscreenOverlayMode; set => SetProperty(ref _fullscreenOverlayMode, value); }

    private double _fullscreenOverlayOpacity = 0.5;
    public double FullscreenOverlayOpacity { get => _fullscreenOverlayOpacity; set => SetProperty(ref _fullscreenOverlayOpacity, value); }

    private string _fullscreenOverlayColor = "#000000";
    public string FullscreenOverlayColor { get => _fullscreenOverlayColor; set => SetProperty(ref _fullscreenOverlayColor, value); }

    // Search/filter (M13)
    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value)) return;
            _notificationsView?.Refresh();
        }
    }

    private bool _isSearchVisible;
    public bool IsSearchVisible
    {
        get => _isSearchVisible;
        set
        {
            if (!SetProperty(ref _isSearchVisible, value)) return;
            OnPropertyChanged(nameof(SearchBarVisibility));
            if (!value)
            {
                SearchText = string.Empty;
            }
        }
    }

    public Visibility SearchBarVisibility => IsSearchVisible ? Visibility.Visible : Visibility.Collapsed;

    // Notification grouping
    private bool _groupByApp;
    public bool GroupByApp
    {
        get => _groupByApp;
        set
        {
            if (SetProperty(ref _groupByApp, value))
                UpdateGrouping();
        }
    }

    private string _appGroupingStyle = "Framed Group";
    public string AppGroupingStyle { get => _appGroupingStyle; set => SetProperty(ref _appGroupingStyle, NormalizeAppGroupingStyle(value)); }

    private bool _showAppGroupCounts = true;
    public bool ShowAppGroupCounts { get => _showAppGroupCounts; set => SetProperty(ref _showAppGroupCounts, value); }

    private ICollectionView? _notificationsView;
    public ICollectionView NotificationsView
    {
        get
        {
            if (_notificationsView == null)
            {
                _notificationsView = CollectionViewSource.GetDefaultView(Notifications);
                _notificationsView.Filter = FilterNotification;
            }
            return _notificationsView;
        }
    }

    private bool FilterNotification(object obj)
    {
        if (string.IsNullOrWhiteSpace(_searchText)) return true;
        if (obj is not NotificationItem item) return false;
        return item.AppName.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || item.Title.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || item.Body.Contains(_searchText, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateGrouping()
    {
        if (_notificationsView == null) return;
        _notificationsView.GroupDescriptions.Clear();
        if (_groupByApp)
            _notificationsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(NotificationItem.AppName)));
    }

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
        _timestampTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        _timestampTimer.Tick += (_, _) =>
        {
            if (!ShowTimestamp && !VoiceAccessTextFormatter.IncludesTimestamp(VoiceAccessReadMode)) return;
            foreach (var n in _queueManager.VisibleNotifications)
                n.NotifyTimestampChanged();
        };
        _timestampTimer.Start();
    }

    private readonly DispatcherTimer _timestampTimer;

    /// <summary>Stop background timers when the overlay is no longer needed.</summary>
    public void Cleanup()
    {
        _timestampTimer.Stop();
    }

    public void SetScrollableOverflow(bool hasScrollableOverflow)
    {
        if (_hasScrollableOverflow == hasScrollableOverflow)
            return;

        _hasScrollableOverflow = hasScrollableOverflow;
        OnPropertyChanged(nameof(ScrollbarVisibility));
        OnPropertyChanged(nameof(OverlayContentMargin));
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
        TextAlignment = string.IsNullOrWhiteSpace(s.TextAlignment) ? "Left" : s.TextAlignment;

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
        CardBackgroundMode = CardBackgroundModeHelper.Normalize(
            string.IsNullOrWhiteSpace(s.CardBackgroundMode) && !string.IsNullOrWhiteSpace(s.CardBackgroundImagePath)
                ? CardBackgroundModeHelper.Image
                : s.CardBackgroundMode);
        CardBackgroundImagePath = s.CardBackgroundImagePath;
        CardBackgroundImageOpacity = s.CardBackgroundImageOpacity;
        CardBackgroundImageHueDegrees = s.CardBackgroundImageHueDegrees;
        CardBackgroundImageBrightness = s.CardBackgroundImageBrightness;
        CardBackgroundImageSaturation = s.CardBackgroundImageSaturation;
        CardBackgroundImageContrast = s.CardBackgroundImageContrast;
        CardBackgroundImageBlackAndWhite = s.CardBackgroundImageBlackAndWhite;
        CardBackgroundImageFitMode = s.CardBackgroundImageFitMode;
        CardBackgroundImagePlacement = s.CardBackgroundImagePlacement;
        CardBackgroundImageVerticalFocus = s.CardBackgroundImageVerticalFocus;
        AccentColor = s.AccentColor;
        HighlightColor = s.HighlightColor;
        HighlightOverlayOpacity = s.HighlightOverlayOpacity;
        HighlightAnimation = s.HighlightAnimation;
        HighlightBorderMode = s.HighlightBorderMode;
        HighlightBorderThickness = s.HighlightBorderThickness;
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

        // Respect Reduce Motion by removing directional movement, but keep fades
        // if the user has animations enabled to avoid "randomly no animation" behavior.
        var reduceMotion = s.RespectReduceMotion && !SystemParameters.ClientAreaAnimation;
        AnimationsEnabled = s.AnimationsEnabled;
        NotificationAnimationStyle = reduceMotion
            ? NotificationAnimationStyleHelper.Fade
            : NotificationAnimationStyleHelper.Normalize(s.NotificationAnimationStyle);
        SlideInDirection = s.SlideInDirection;
        AnimationDurationMs = s.AnimationDurationMs;
        AnimationEasing = s.AnimationEasing;
        OverlayWidth = s.OverlayWidth;
        OverlayMaxHeight = s.OverlayMaxHeight;
        OverlayScrollbarVisible = s.OverlayScrollbarVisible;
        OverlayScrollbarWidth = s.OverlayScrollbarWidth;
        OverlayScrollbarOpacity = s.OverlayScrollbarOpacity;
        OverlayScrollbarTrackColor = s.OverlayScrollbarTrackColor;
        OverlayScrollbarThumbColor = s.OverlayScrollbarThumbColor;
        OverlayScrollbarThumbHoverColor = s.OverlayScrollbarThumbHoverColor;
        OverlayScrollbarPadding = s.OverlayScrollbarPadding;
        OverlayScrollbarContentGap = s.OverlayScrollbarContentGap;
        OverlayScrollbarCornerRadius = s.OverlayScrollbarCornerRadius;
        OnPropertyChanged(nameof(ScrollbarVisibility));
        OnPropertyChanged(nameof(OverlayContentMargin));
        ShowNotificationIcons = s.ShowNotificationIcons;
        IconSize = s.IconSize;
        CurrentSettings = s;
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
        TimestampFontSize = s.TimestampFontSize;
        TimestampDisplayMode = string.IsNullOrWhiteSpace(s.TimestampDisplayMode) ? "Relative" : s.TimestampDisplayMode;
        TimestampFontWeight = string.IsNullOrWhiteSpace(s.TimestampFontWeight) ? "Normal" : s.TimestampFontWeight;
        TimestampColor = string.IsNullOrWhiteSpace(s.TimestampColor) ? "#C8C8C8" : s.TimestampColor;
        VoiceAccessReadMode = VoiceAccessTextFormatter.NormalizeMode(s.VoiceAccessReadMode);
        ChromaKeyEnabled = s.ChromaKeyEnabled;
        ChromaKeyColor = s.ChromaKeyColor;
        PerAppTintEnabled = s.PerAppTintEnabled;
        PerAppTintOpacity = s.PerAppTintOpacity;
        FullscreenOverlayMode = s.FullscreenOverlayMode;
        FullscreenOverlayOpacity = s.FullscreenOverlayOpacity;
        FullscreenOverlayColor = s.FullscreenOverlayColor;
        GroupByApp = s.GroupByApp;
        AppGroupingStyle = s.AppGroupingStyle;
        ShowAppGroupCounts = s.ShowAppGroupCounts;

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
        OnPropertyChanged(nameof(HighlightCardBorderThickness));
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
        var isEmpty = _queueManager.VisibleNotifications.Count == 0;
        EmptyStateVisibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
        if (isEmpty)
            SetScrollableOverflow(false);
    }

    private static Duration DurationFor(double ms)
    {
        if (ms <= 0)
            return new Duration(TimeSpan.Zero);
        return new Duration(TimeSpan.FromMilliseconds(ms));
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

}
