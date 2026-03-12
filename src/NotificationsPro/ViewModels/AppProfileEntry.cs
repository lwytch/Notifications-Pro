using NotificationsPro.Helpers;

namespace NotificationsPro.ViewModels;

public class AppProfileEntry : BaseViewModel
{
    public string AppName { get; }

    private string _sound = "Default";
    public string Sound
    {
        get => _sound;
        set => SetProperty(ref _sound, string.IsNullOrWhiteSpace(value) ? "Default" : value);
    }

    private string _icon = "Default";
    public string Icon
    {
        get => _icon;
        set => SetProperty(ref _icon, string.IsNullOrWhiteSpace(value) ? "Default" : value);
    }

    private bool _isReadAloudEnabled = true;
    public bool IsReadAloudEnabled
    {
        get => _isReadAloudEnabled;
        set => SetProperty(ref _isReadAloudEnabled, value);
    }

    private string _overlayLane = OverlayLaneHelper.Main;
    public string OverlayLane
    {
        get => _overlayLane;
        set => SetProperty(ref _overlayLane, OverlayLaneHelper.Normalize(value));
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

    public AppProfileEntry(string appName)
    {
        AppName = appName;
    }
}
