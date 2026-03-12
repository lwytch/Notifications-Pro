using NotificationsPro.Helpers;

namespace NotificationsPro.ViewModels;

public class OverlayLaneEntry : BaseViewModel
{
    public string Id { get; }

    private string _name;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, string.IsNullOrWhiteSpace(value) ? "Overlay Lane" : value.Trim());
    }

    private bool _isEnabled = true;
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    private string _fontFamily = string.Empty;
    public string FontFamily
    {
        get => _fontFamily;
        set => SetProperty(ref _fontFamily, value?.Trim() ?? string.Empty);
    }

    private double _fontSize;
    public double FontSize
    {
        get => _fontSize;
        set => SetProperty(ref _fontSize, value);
    }

    private string _fontWeight = string.Empty;
    public string FontWeight
    {
        get => _fontWeight;
        set => SetProperty(ref _fontWeight, value?.Trim() ?? string.Empty);
    }

    private double _appNameFontSize;
    public double AppNameFontSize
    {
        get => _appNameFontSize;
        set => SetProperty(ref _appNameFontSize, value);
    }

    private string _appNameFontWeight = string.Empty;
    public string AppNameFontWeight
    {
        get => _appNameFontWeight;
        set => SetProperty(ref _appNameFontWeight, value?.Trim() ?? string.Empty);
    }

    private double _titleFontSize;
    public double TitleFontSize
    {
        get => _titleFontSize;
        set => SetProperty(ref _titleFontSize, value);
    }

    private string _titleFontWeight = string.Empty;
    public string TitleFontWeight
    {
        get => _titleFontWeight;
        set => SetProperty(ref _titleFontWeight, value?.Trim() ?? string.Empty);
    }

    private double _lineSpacing;
    public double LineSpacing
    {
        get => _lineSpacing;
        set => SetProperty(ref _lineSpacing, value);
    }

    private string _textAlignment = string.Empty;
    public string TextAlignment
    {
        get => _textAlignment;
        set => SetProperty(ref _textAlignment, value?.Trim() ?? string.Empty);
    }

    private int _monitorIndex;
    public int MonitorIndex
    {
        get => _monitorIndex;
        set => SetProperty(ref _monitorIndex, Math.Max(0, value));
    }

    private string _positionPreset = "Top Left";
    public string PositionPreset
    {
        get => _positionPreset;
        set => SetProperty(ref _positionPreset, SecondaryOverlayPositionHelper.Normalize(value));
    }

    private double _width = 340;
    public double Width
    {
        get => _width;
        set => SetProperty(ref _width, Math.Clamp(value, 220, 7680));
    }

    private double _maxHeight = 480;
    public double MaxHeight
    {
        get => _maxHeight;
        set => SetProperty(ref _maxHeight, Math.Clamp(value, 120, 4320));
    }

    private string _accentColor = string.Empty;
    public string AccentColor
    {
        get => _accentColor;
        set => SetProperty(ref _accentColor, value?.Trim() ?? string.Empty);
    }

    private string _backgroundColor = string.Empty;
    public string BackgroundColor
    {
        get => _backgroundColor;
        set => SetProperty(ref _backgroundColor, value?.Trim() ?? string.Empty);
    }

    private double _backgroundOpacity;
    public double BackgroundOpacity
    {
        get => _backgroundOpacity;
        set => SetProperty(ref _backgroundOpacity, Math.Clamp(value, 0.0, 1.0));
    }

    private string _titleColor = string.Empty;
    public string TitleColor
    {
        get => _titleColor;
        set => SetProperty(ref _titleColor, value?.Trim() ?? string.Empty);
    }

    private string _textColor = string.Empty;
    public string TextColor
    {
        get => _textColor;
        set => SetProperty(ref _textColor, value?.Trim() ?? string.Empty);
    }

    private string _appNameColor = string.Empty;
    public string AppNameColor
    {
        get => _appNameColor;
        set => SetProperty(ref _appNameColor, value?.Trim() ?? string.Empty);
    }

    private double _cornerRadius;
    public double CornerRadius
    {
        get => _cornerRadius;
        set => SetProperty(ref _cornerRadius, value);
    }

    private double _padding;
    public double Padding
    {
        get => _padding;
        set => SetProperty(ref _padding, value);
    }

    private double _cardGap;
    public double CardGap
    {
        get => _cardGap;
        set => SetProperty(ref _cardGap, value);
    }

    private double _outerMargin;
    public double OuterMargin
    {
        get => _outerMargin;
        set => SetProperty(ref _outerMargin, value);
    }

    private bool _showAccent;
    public bool ShowAccent
    {
        get => _showAccent;
        set => SetProperty(ref _showAccent, value);
    }

    private double _accentThickness = 3;
    public double AccentThickness
    {
        get => _accentThickness;
        set => SetProperty(ref _accentThickness, value);
    }

    private bool _showBorder;
    public bool ShowBorder
    {
        get => _showBorder;
        set => SetProperty(ref _showBorder, value);
    }

    private string _borderColor = string.Empty;
    public string BorderColor
    {
        get => _borderColor;
        set => SetProperty(ref _borderColor, value?.Trim() ?? string.Empty);
    }

    private double _borderThickness = 1;
    public double BorderThickness
    {
        get => _borderThickness;
        set => SetProperty(ref _borderThickness, value);
    }

    private bool _showAppName = true;
    public bool ShowAppName
    {
        get => _showAppName;
        set => SetProperty(ref _showAppName, value);
    }

    private bool _showNotificationTitle = true;
    public bool ShowNotificationTitle
    {
        get => _showNotificationTitle;
        set => SetProperty(ref _showNotificationTitle, value);
    }

    private bool _showNotificationBody = true;
    public bool ShowNotificationBody
    {
        get => _showNotificationBody;
        set => SetProperty(ref _showNotificationBody, value);
    }

    private bool _limitTextLines;
    public bool LimitTextLines
    {
        get => _limitTextLines;
        set => SetProperty(ref _limitTextLines, value);
    }

    private int _maxAppNameLines;
    public int MaxAppNameLines
    {
        get => _maxAppNameLines;
        set => SetProperty(ref _maxAppNameLines, Math.Max(1, value));
    }

    private int _maxTitleLines;
    public int MaxTitleLines
    {
        get => _maxTitleLines;
        set => SetProperty(ref _maxTitleLines, Math.Max(1, value));
    }

    private int _maxBodyLines;
    public int MaxBodyLines
    {
        get => _maxBodyLines;
        set => SetProperty(ref _maxBodyLines, Math.Max(1, value));
    }

    private bool _singleLineMode;
    public bool SingleLineMode
    {
        get => _singleLineMode;
        set => SetProperty(ref _singleLineMode, value);
    }

    private bool _singleLineWrapText;
    public bool SingleLineWrapText
    {
        get => _singleLineWrapText;
        set => SetProperty(ref _singleLineWrapText, value);
    }

    private int _singleLineMaxLines = 3;
    public int SingleLineMaxLines
    {
        get => _singleLineMaxLines;
        set => SetProperty(ref _singleLineMaxLines, Math.Max(1, value));
    }

    private bool _singleLineAutoFullWidth;
    public bool SingleLineAutoFullWidth
    {
        get => _singleLineAutoFullWidth;
        set => SetProperty(ref _singleLineAutoFullWidth, value);
    }

    private bool _showTimestamp = true;
    public bool ShowTimestamp
    {
        get => _showTimestamp;
        set => SetProperty(ref _showTimestamp, value);
    }

    private double _timestampFontSize;
    public double TimestampFontSize
    {
        get => _timestampFontSize;
        set => SetProperty(ref _timestampFontSize, Math.Clamp(value, 8, 32));
    }

    private string _timestampDisplayMode = string.Empty;
    public string TimestampDisplayMode
    {
        get => _timestampDisplayMode;
        set => SetProperty(ref _timestampDisplayMode, value?.Trim() ?? string.Empty);
    }

    private string _timestampFontWeight = string.Empty;
    public string TimestampFontWeight
    {
        get => _timestampFontWeight;
        set => SetProperty(ref _timestampFontWeight, value?.Trim() ?? string.Empty);
    }

    private string _timestampColor = string.Empty;
    public string TimestampColor
    {
        get => _timestampColor;
        set => SetProperty(ref _timestampColor, value?.Trim() ?? string.Empty);
    }

    private string _densityPreset = string.Empty;
    public string DensityPreset
    {
        get => _densityPreset;
        set => SetProperty(ref _densityPreset, value?.Trim() ?? string.Empty);
    }

    private double? _left;
    public double? Left
    {
        get => _left;
        set => SetProperty(ref _left, value);
    }

    private double? _top;
    public double? Top
    {
        get => _top;
        set => SetProperty(ref _top, value);
    }

    private string _backgroundImagePath = string.Empty;
    public string BackgroundImagePath
    {
        get => _backgroundImagePath;
        set => SetProperty(ref _backgroundImagePath, value?.Trim() ?? string.Empty);
    }

    private double _backgroundImageOpacity = 0.45;
    public double BackgroundImageOpacity
    {
        get => _backgroundImageOpacity;
        set => SetProperty(ref _backgroundImageOpacity, Math.Clamp(value, 0.0, 1.0));
    }

    private double _backgroundImageHueDegrees;
    public double BackgroundImageHueDegrees
    {
        get => _backgroundImageHueDegrees;
        set => SetProperty(ref _backgroundImageHueDegrees, Math.Clamp(value, -180.0, 180.0));
    }

    private double _backgroundImageBrightness = 1.0;
    public double BackgroundImageBrightness
    {
        get => _backgroundImageBrightness;
        set => SetProperty(ref _backgroundImageBrightness, Math.Clamp(value, 0.2, 2.0));
    }

    public OverlayLaneEntry(string id, string name)
    {
        Id = id;
        _name = string.IsNullOrWhiteSpace(name) ? "Overlay Lane" : name.Trim();
    }
}
