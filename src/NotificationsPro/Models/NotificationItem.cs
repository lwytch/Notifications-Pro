using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using NotificationsPro.Helpers;

namespace NotificationsPro.Models;

/// <summary>
/// In-memory-only notification data. Never serialized or persisted.
/// Discarded from memory after display expires.
/// </summary>
public class NotificationItem : INotifyPropertyChanged
{
    public string AppName { get; }
    public string Title { get; }
    public string Body { get; }
    public DateTime ReceivedAt { get; }

    private bool _isExpiring;
    public bool IsExpiring
    {
        get => _isExpiring;
        set { _isExpiring = value; OnPropertyChanged(); }
    }

    public string RelativeTimeText
    {
        get
        {
            var elapsed = DateTime.Now - ReceivedAt;
            if (elapsed.TotalSeconds < 10) return "just now";
            if (elapsed.TotalSeconds < 60) return $"{(int)elapsed.TotalSeconds}s ago";
            if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes}m ago";
            return $"{(int)elapsed.TotalHours}h ago";
        }
    }

    private bool _isHighlighted;
    public bool IsHighlighted
    {
        get => _isHighlighted;
        set
        {
            if (_isHighlighted == value)
                return;

            _isHighlighted = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Per-notification highlight color (hex). Empty means fall back to the global HighlightColor.</summary>
    private string _highlightColor = string.Empty;
    public string HighlightColor
    {
        get => _highlightColor;
        set
        {
            var normalized = value ?? string.Empty;
            if (string.Equals(_highlightColor, normalized, StringComparison.Ordinal))
                return;

            _highlightColor = normalized;
            OnPropertyChanged();
        }
    }

    private string _highlightAnimation = HighlightAnimationHelper.None;
    public string HighlightAnimation
    {
        get => _highlightAnimation;
        set
        {
            var normalized = HighlightAnimationHelper.Normalize(value);
            if (string.Equals(_highlightAnimation, normalized, StringComparison.Ordinal))
                return;

            _highlightAnimation = normalized;
            OnPropertyChanged();
        }
    }

    private double _highlightOverlayOpacity = 0.25;
    public double HighlightOverlayOpacity
    {
        get => _highlightOverlayOpacity;
        set
        {
            var normalized = double.IsNaN(value) ? 0.25 : Math.Clamp(value, 0.05, 0.80);
            if (Math.Abs(_highlightOverlayOpacity - normalized) < 0.0001)
                return;

            _highlightOverlayOpacity = normalized;
            OnPropertyChanged();
        }
    }

    private Thickness _highlightCardBorderThickness = new(1);
    public Thickness HighlightCardBorderThickness
    {
        get => _highlightCardBorderThickness;
        set
        {
            if (_highlightCardBorderThickness.Equals(value))
                return;

            _highlightCardBorderThickness = value;
            OnPropertyChanged();
        }
    }

    public string BackgroundImageMode { get; set; } = Helpers.CardBackgroundModeHelper.Solid;
    public string BackgroundImagePath { get; set; } = string.Empty;
    public double BackgroundImageOpacity { get; set; } = 0.45;
    public double BackgroundImageHueDegrees { get; set; }
    public double BackgroundImageBrightness { get; set; } = 1.0;
    public double BackgroundImageSaturation { get; set; } = 1.0;
    public double BackgroundImageContrast { get; set; } = 1.0;
    public bool BackgroundImageBlackAndWhite { get; set; }
    public string BackgroundImageFitMode { get; set; } = "Fill Card";
    public string BackgroundImagePlacement { get; set; } = "Inside Padding";
    public string BackgroundImageVerticalFocus { get; set; } = Helpers.ImageVerticalFocusHelper.Center;
    public bool? ReadAloudEnabledOverride { get; set; }
    public string ReadAloudModeOverride { get; set; } = string.Empty;
    public bool IsLocalPreview { get; set; }

    public void NotifyTimestampChanged() => OnPropertyChanged(nameof(RelativeTimeText));

    public NotificationItem(string appName, string title, string body)
    {
        AppName = appName ?? string.Empty;
        Title = title ?? string.Empty;
        Body = body ?? string.Empty;
        ReceivedAt = DateTime.Now;
    }

    public NotificationItem(string title, string body)
        : this(string.Empty, title, body)
    {
    }

    public bool IsDuplicateOf(string appName, string title, string body, TimeSpan window)
    {
        return AppName == appName
            && Title == title
            && Body == body
            && (DateTime.Now - ReceivedAt) < window;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
