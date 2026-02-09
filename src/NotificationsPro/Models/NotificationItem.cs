using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NotificationsPro.Models;

/// <summary>
/// In-memory-only notification data. Never serialized or persisted.
/// Discarded from memory after display expires.
/// </summary>
public class NotificationItem : INotifyPropertyChanged
{
    public string Title { get; }
    public string Body { get; }
    public DateTime ReceivedAt { get; }

    private bool _isExpiring;
    public bool IsExpiring
    {
        get => _isExpiring;
        set { _isExpiring = value; OnPropertyChanged(); }
    }

    public NotificationItem(string title, string body)
    {
        Title = title ?? string.Empty;
        Body = body ?? string.Empty;
        ReceivedAt = DateTime.Now;
    }

    public bool IsDuplicateOf(string title, string body, TimeSpan window)
    {
        return Title == title && Body == body
            && (DateTime.Now - ReceivedAt) < window;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
