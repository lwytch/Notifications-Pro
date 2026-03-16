using System.ComponentModel;
using System.Runtime.CompilerServices;
using NotificationsPro.Helpers;

namespace NotificationsPro.ViewModels;

public class KeywordHighlightEntry : INotifyPropertyChanged
{
    public const string UseGlobalSetting = "Use global";

    private string _keyword;
    public string Keyword
    {
        get => _keyword;
        set
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (string.Equals(_keyword, normalized, StringComparison.Ordinal))
                return;

            _keyword = normalized;
            OnPropertyChanged();
        }
    }

    private string _color;
    public string Color
    {
        get => _color;
        set { _color = value; OnPropertyChanged(); }
    }

    private bool _isRegex;
    public bool IsRegex
    {
        get => _isRegex;
        set { _isRegex = value; OnPropertyChanged(); }
    }

    private string _scope = NotificationMatchScopeHelper.TitleAndBody;
    public string Scope
    {
        get => _scope;
        set
        {
            _scope = NotificationMatchScopeHelper.Normalize(value);
            OnPropertyChanged();
        }
    }

    private string _appFilter = string.Empty;
    public string AppFilter
    {
        get => _appFilter;
        set
        {
            _appFilter = value?.Trim() ?? string.Empty;
            OnPropertyChanged();
        }
    }

    private string _animation = UseGlobalSetting;
    public string Animation
    {
        get => _animation;
        set
        {
            var normalized = NormalizeAnimationOverride(value);
            if (string.Equals(_animation, normalized, StringComparison.Ordinal))
                return;

            _animation = normalized;
            OnPropertyChanged();
        }
    }

    private string _borderMode = UseGlobalSetting;
    public string BorderMode
    {
        get => _borderMode;
        set
        {
            var normalized = NormalizeBorderModeOverride(value);
            if (string.Equals(_borderMode, normalized, StringComparison.Ordinal))
                return;

            _borderMode = normalized;
            OnPropertyChanged();
        }
    }

    private bool _useCustomOverlayOpacity;
    public bool UseCustomOverlayOpacity
    {
        get => _useCustomOverlayOpacity;
        set
        {
            if (_useCustomOverlayOpacity == value)
                return;

            _useCustomOverlayOpacity = value;
            OnPropertyChanged();
        }
    }

    private double _overlayOpacity = 0.25;
    public double OverlayOpacity
    {
        get => _overlayOpacity;
        set
        {
            var normalized = double.IsNaN(value) ? 0.25 : Math.Clamp(value, 0.05, 0.80);
            if (Math.Abs(_overlayOpacity - normalized) < 0.0001)
                return;

            _overlayOpacity = normalized;
            OnPropertyChanged();
        }
    }

    private bool _useCustomBorderThickness;
    public bool UseCustomBorderThickness
    {
        get => _useCustomBorderThickness;
        set
        {
            if (_useCustomBorderThickness == value)
                return;

            _useCustomBorderThickness = value;
            OnPropertyChanged();
        }
    }

    private double _borderThickness = 1;
    public double BorderThickness
    {
        get => _borderThickness;
        set
        {
            var normalized = double.IsNaN(value) ? 1 : Math.Clamp(value, 0.5, 8.0);
            if (Math.Abs(_borderThickness - normalized) < 0.0001)
                return;

            _borderThickness = normalized;
            OnPropertyChanged();
        }
    }

    public KeywordHighlightEntry(
        string keyword,
        string color,
        bool isRegex = false,
        string? scope = null,
        string? appFilter = null,
        string? animation = null,
        string? borderMode = null,
        double? overlayOpacity = null,
        double? borderThickness = null)
    {
        _keyword = keyword?.Trim() ?? string.Empty;
        _color = color;
        _isRegex = isRegex;
        _scope = NotificationMatchScopeHelper.Normalize(scope);
        _appFilter = appFilter?.Trim() ?? string.Empty;
        _animation = NormalizeAnimationOverride(animation);
        _borderMode = NormalizeBorderModeOverride(borderMode);
        _useCustomOverlayOpacity = overlayOpacity.HasValue;
        _overlayOpacity = overlayOpacity.HasValue ? Math.Clamp(overlayOpacity.Value, 0.05, 0.80) : 0.25;
        _useCustomBorderThickness = borderThickness.HasValue;
        _borderThickness = borderThickness.HasValue ? Math.Clamp(borderThickness.Value, 0.5, 8.0) : 1;
    }

    private static string NormalizeAnimationOverride(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || string.Equals(value.Trim(), UseGlobalSetting, StringComparison.OrdinalIgnoreCase))
        {
            return UseGlobalSetting;
        }

        return HighlightAnimationHelper.Normalize(value);
    }

    private static string NormalizeBorderModeOverride(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || string.Equals(value.Trim(), UseGlobalSetting, StringComparison.OrdinalIgnoreCase))
        {
            return UseGlobalSetting;
        }

        return HighlightBorderModeHelper.Normalize(value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
