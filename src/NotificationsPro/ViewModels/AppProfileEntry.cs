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

    public AppProfileEntry(string appName)
    {
        AppName = appName;
    }
}
