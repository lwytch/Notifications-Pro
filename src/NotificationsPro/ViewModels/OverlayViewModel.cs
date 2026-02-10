using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using NotificationsPro.Models;
using NotificationsPro.Services;

namespace NotificationsPro.ViewModels;

public class OverlayViewModel : BaseViewModel
{
    private readonly QueueManager _queueManager;
    private readonly SettingsManager _settingsManager;

    public ReadOnlyObservableCollection<NotificationItem> Notifications => _queueManager.VisibleNotifications;
    public QueueManager Queue => _queueManager;

    // Appearance bindings
    private string _fontFamily = "Segoe UI";
    public string FontFamily { get => _fontFamily; set => SetProperty(ref _fontFamily, value); }

    private double _fontSize = 14;
    public double FontSize { get => _fontSize; set => SetProperty(ref _fontSize, value); }

    private string _fontWeight = "Normal";
    public string FontWeight { get => _fontWeight; set => SetProperty(ref _fontWeight, value); }

    private double _lineSpacing = 1.5;
    public double LineSpacing { get => _lineSpacing; set => SetProperty(ref _lineSpacing, value); }

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

    private double _cornerRadius = 12;
    public double CornerRadius { get => _cornerRadius; set => SetProperty(ref _cornerRadius, value); }

    private double _padding = 16;
    public double Padding { get => _padding; set => SetProperty(ref _padding, value); }

    private bool _showBorder = true;
    public bool ShowBorder { get => _showBorder; set => SetProperty(ref _showBorder, value); }

    private string _borderColor = "#7C5CFC";
    public string BorderColor { get => _borderColor; set => SetProperty(ref _borderColor, value); }

    private double _borderThickness = 1;
    public double BorderThickness { get => _borderThickness; set => SetProperty(ref _borderThickness, value); }

    private string _accentColor = "#7C5CFC";
    public string AccentColor { get => _accentColor; set => SetProperty(ref _accentColor, value); }

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

    private bool _showAppName = true;
    public bool ShowAppName { get => _showAppName; set => SetProperty(ref _showAppName, value); }

    private bool _showNotificationTitle = true;
    public bool ShowNotificationTitle { get => _showNotificationTitle; set => SetProperty(ref _showNotificationTitle, value); }

    private bool _showNotificationBody = true;
    public bool ShowNotificationBody { get => _showNotificationBody; set => SetProperty(ref _showNotificationBody, value); }

    private int _maxAppNameLines = 2;
    public int MaxAppNameLines
    {
        get => _maxAppNameLines;
        set
        {
            if (!SetProperty(ref _maxAppNameLines, Math.Max(1, value))) return;
            OnPropertyChanged(nameof(AppNameMaxHeight));
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
        }
    }

    private bool _singleLineMode;
    public bool SingleLineMode { get => _singleLineMode; set => SetProperty(ref _singleLineMode, value); }

    public double TitleFontSize => FontSize + 2;
    public double TitleLineHeight => TitleFontSize * LineSpacing;
    public double BodyLineHeight => FontSize * LineSpacing;
    public double AppNameMaxHeight => Math.Max(4, MaxAppNameLines * BodyLineHeight);
    public double TitleMaxHeight => Math.Max(4, MaxTitleLines * TitleLineHeight);
    public double BodyMaxHeight => Math.Max(4, MaxBodyLines * BodyLineHeight);
    public double EnterOffset => AnimationsEnabled && !FadeOnlyAnimation ? -(OverlayWidth + 40) : 0;
    public double ExitOffset => AnimationsEnabled && !FadeOnlyAnimation ? 50 : 0;
    public Duration EntryMotionDuration => DurationFor(AnimationDurationMs);
    public Duration EntryFadeDuration => DurationFor(AnimationDurationMs * 0.75);
    public Duration ExitMotionDuration => DurationFor(AnimationDurationMs);
    public Duration ExitFadeDuration => DurationFor(AnimationDurationMs);

    public OverlayViewModel(QueueManager queueManager, SettingsManager settingsManager)
    {
        _queueManager = queueManager;
        _settingsManager = settingsManager;

        ApplySettings(_settingsManager.Settings);
        _settingsManager.SettingsChanged += () => ApplySettings(_settingsManager.Settings);
    }

    public void ApplySettings(AppSettings s)
    {
        FontFamily = s.FontFamily;
        FontSize = s.FontSize;
        FontWeight = s.FontWeight;
        LineSpacing = s.LineSpacing;
        TextColor = s.TextColor;
        TitleColor = s.TitleColor;
        AppNameColor = s.AppNameColor;
        BackgroundColor = s.BackgroundColor;
        BackgroundOpacity = s.BackgroundOpacity;
        CornerRadius = s.CornerRadius;
        Padding = s.Padding;
        ShowBorder = s.ShowBorder;
        BorderColor = s.BorderColor;
        BorderThickness = s.BorderThickness;
        AccentColor = s.AccentColor;
        AlwaysOnTop = s.AlwaysOnTop;
        AnimationsEnabled = s.AnimationsEnabled;
        FadeOnlyAnimation = s.FadeOnlyAnimation;
        AnimationDurationMs = s.AnimationDurationMs;
        OverlayWidth = s.OverlayWidth;
        OverlayMaxHeight = s.OverlayMaxHeight;
        ShowAppName = s.ShowAppName;
        ShowNotificationTitle = s.ShowNotificationTitle;
        ShowNotificationBody = s.ShowNotificationBody;
        MaxAppNameLines = s.MaxAppNameLines;
        MaxTitleLines = s.MaxTitleLines;
        MaxBodyLines = s.MaxBodyLines;
        SingleLineMode = s.SingleLineMode;
        OnPropertyChanged(nameof(TitleFontSize));
        OnPropertyChanged(nameof(TitleLineHeight));
        OnPropertyChanged(nameof(BodyLineHeight));
        OnPropertyChanged(nameof(AppNameMaxHeight));
        OnPropertyChanged(nameof(TitleMaxHeight));
        OnPropertyChanged(nameof(BodyMaxHeight));
        OnPropertyChanged(nameof(EnterOffset));
        OnPropertyChanged(nameof(ExitOffset));
        OnPropertyChanged(nameof(EntryMotionDuration));
        OnPropertyChanged(nameof(EntryFadeDuration));
        OnPropertyChanged(nameof(ExitMotionDuration));
        OnPropertyChanged(nameof(ExitFadeDuration));
    }

    private static Duration DurationFor(double ms)
    {
        if (ms <= 0)
            return new Duration(TimeSpan.Zero);
        return new Duration(TimeSpan.FromMilliseconds(ms));
    }
}
