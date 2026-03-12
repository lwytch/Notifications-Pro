using System.ComponentModel;
using System.Runtime.CompilerServices;

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

    public bool IsHighlighted { get; set; }

    /// <summary>Per-notification highlight color (hex). Empty means fall back to the global HighlightColor.</summary>
    public string HighlightColor { get; set; } = string.Empty;
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
