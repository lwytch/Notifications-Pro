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
