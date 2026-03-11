using NotificationsPro.Helpers;
using NotificationsPro.Models;

namespace NotificationsPro.Tests;

public class SpokenNotificationPlaybackTrackerTests
{
    [Fact]
    public void TryQueue_AllowsNewItem_Once()
    {
        var tracker = new SpokenNotificationPlaybackTracker();
        var item = new NotificationItem("Teams", "Title", "Body");

        Assert.True(tracker.TryQueue(item, currentItem: null));
        Assert.False(tracker.TryQueue(item, currentItem: null));
    }

    [Fact]
    public void MarkSpoken_PreventsReplay_ForVisibleItem()
    {
        var tracker = new SpokenNotificationPlaybackTracker();
        var item = new NotificationItem("Slack", "Build complete", "All tests passed");

        Assert.True(tracker.TryQueue(item, currentItem: null));
        tracker.MarkDequeued(item);
        tracker.MarkSpoken(item);

        Assert.False(tracker.TryQueue(item, currentItem: null));
    }

    [Fact]
    public void Prune_RemovesDismissedItems_FromReplayTracking()
    {
        var tracker = new SpokenNotificationPlaybackTracker();
        var oldItem = new NotificationItem("Outlook", "Mail", "Body");
        var newItem = new NotificationItem("Discord", "Ping", "Body");

        Assert.True(tracker.TryQueue(oldItem, currentItem: null));
        tracker.MarkDequeued(oldItem);
        tracker.MarkSpoken(oldItem);
        tracker.Prune(new[] { newItem });

        Assert.True(tracker.TryQueue(oldItem, currentItem: null));
    }
}
