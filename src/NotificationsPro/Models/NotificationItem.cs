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
