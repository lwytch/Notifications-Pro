using System.Collections.ObjectModel;
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
    public bool AnimationsEnabled { get => _animationsEnabled; set => SetProperty(ref _animationsEnabled, value); }

    private double _overlayWidth = 380;
    public double OverlayWidth { get => _overlayWidth; set => SetProperty(ref _overlayWidth, value); }

    private double _overlayMaxHeight = 600;
    public double OverlayMaxHeight { get => _overlayMaxHeight; set => SetProperty(ref _overlayMaxHeight, value); }

    public double TitleFontSize => FontSize + 2;

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
        OverlayWidth = s.OverlayWidth;
        OverlayMaxHeight = s.OverlayMaxHeight;
        OnPropertyChanged(nameof(TitleFontSize));
    }
}
