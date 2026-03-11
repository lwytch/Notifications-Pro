using NotificationsPro.Models;

namespace NotificationsPro.Helpers;

/// <summary>
/// Tracks which visible notification instances have already been spoken so
/// narration can resume unfinished items without replaying completed ones.
/// </summary>
internal sealed class SpokenNotificationPlaybackTracker
{
    private readonly HashSet<NotificationItem> _spokenItems = new();
    private readonly HashSet<NotificationItem> _queuedItems = new();

    public bool TryQueue(NotificationItem item, NotificationItem? currentItem)
    {
        if (_spokenItems.Contains(item) || _queuedItems.Contains(item) || ReferenceEquals(currentItem, item))
            return false;

        _queuedItems.Add(item);
        return true;
    }

    public void MarkDequeued(NotificationItem item)
    {
        _queuedItems.Remove(item);
    }

    public void MarkSpoken(NotificationItem item)
    {
        _queuedItems.Remove(item);
        _spokenItems.Add(item);
    }

    public void Prune(IEnumerable<NotificationItem> visibleItems)
    {
        var visible = visibleItems.ToHashSet();
        _queuedItems.RemoveWhere(item => !visible.Contains(item));
        _spokenItems.RemoveWhere(item => !visible.Contains(item));
    }
}
