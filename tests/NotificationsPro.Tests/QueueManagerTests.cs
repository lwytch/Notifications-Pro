using NotificationsPro.Models;
using NotificationsPro.Services;
using System.IO;

namespace NotificationsPro.Tests;

public class QueueManagerTests
{
    private static SettingsManager CreateSettings(double duration = 60, int maxVisible = 3)
    {
        var sm = new SettingsManager();
        sm.Settings.NotificationDuration = duration;
        sm.Settings.MaxVisibleNotifications = maxVisible;
        sm.Settings.AnimationDurationMs = 0;
        return sm;
    }

    [Fact]
    public void AddNotification_AddsToVisibleList()
    {
        var queue = new QueueManager(CreateSettings());
        queue.AddNotification("Title", "Body");

        Assert.Single(queue.VisibleNotifications);
        Assert.Equal("Title", queue.VisibleNotifications[0].Title);
        Assert.Equal("Body", queue.VisibleNotifications[0].Body);
    }

    [Fact]
    public void AddNotification_WithAppName_StoresSourceApp()
    {
        var queue = new QueueManager(CreateSettings());
        queue.AddNotification("Slack", "Mention", "Alex mentioned you in #design");

        Assert.Single(queue.VisibleNotifications);
        Assert.Equal("Slack", queue.VisibleNotifications[0].AppName);
        Assert.Equal("Mention", queue.VisibleNotifications[0].Title);
    }

    [Fact]
    public void AddNotification_MaxThreeVisible()
    {
        var queue = new QueueManager(CreateSettings());
        queue.AddNotification("A", "1");
        queue.AddNotification("B", "2");
        queue.AddNotification("C", "3");
        queue.AddNotification("D", "4");

        Assert.Equal(3, queue.VisibleNotifications.Count);
        Assert.Equal(1, queue.OverflowCount);
    }

    [Fact]
    public void AddNotification_OverflowDoesNotStoreContent()
    {
        var queue = new QueueManager(CreateSettings());
        queue.AddNotification("A", "1");
        queue.AddNotification("B", "2");
        queue.AddNotification("C", "3");
        queue.AddNotification("Overflow1", "Should not be stored");
        queue.AddNotification("Overflow2", "Also not stored");

        // Only the first 3 titles are in the visible list
        var titles = queue.VisibleNotifications.Select(n => n.Title).ToList();
        Assert.DoesNotContain("Overflow1", titles);
        Assert.DoesNotContain("Overflow2", titles);
        Assert.Equal(2, queue.OverflowCount);
    }

    [Fact]
    public void AddNotification_SkipsEmptyTitleAndBody()
    {
        var queue = new QueueManager(CreateSettings());
        queue.AddNotification("", "");
        queue.AddNotification("  ", "  ");
        queue.AddNotification(null!, null!);

        Assert.Empty(queue.VisibleNotifications);
    }

    [Fact]
    public void AddNotification_AllowsTitleOnlyOrBodyOnly()
    {
        var queue = new QueueManager(CreateSettings());
        queue.AddNotification("Title only", "");
        queue.AddNotification("", "Body only");

        Assert.Equal(2, queue.VisibleNotifications.Count);
    }

    [Fact]
    public void AddNotification_DeduplicatesWithinWindow()
    {
        var queue = new QueueManager(CreateSettings());
        queue.AddNotification("Same", "Content");
        queue.AddNotification("Same", "Content");

        Assert.Single(queue.VisibleNotifications);
    }

    [Fact]
    public void AddNotification_AllowsDifferentContent()
    {
        var queue = new QueueManager(CreateSettings());
        queue.AddNotification("A", "Content1");
        queue.AddNotification("B", "Content2");

        Assert.Equal(2, queue.VisibleNotifications.Count);
    }

    [Fact]
    public void AddNotification_NewestFirst()
    {
        var queue = new QueueManager(CreateSettings());
        queue.AddNotification("First", "1");
        queue.AddNotification("Second", "2");

        Assert.Equal("Second", queue.VisibleNotifications[0].Title);
        Assert.Equal("First", queue.VisibleNotifications[1].Title);
    }

    [Fact]
    public void AddNotification_NewestOnBottom_WhenConfigured()
    {
        var settings = CreateSettings();
        settings.Settings.NewestOnTop = false;
        var queue = new QueueManager(settings);

        queue.AddNotification("First", "1");
        queue.AddNotification("Second", "2");

        Assert.Equal("First", queue.VisibleNotifications[0].Title);
        Assert.Equal("Second", queue.VisibleNotifications[1].Title);
    }

    [Fact]
    public void AddNotification_SkipsWhenPaused()
    {
        var queue = new QueueManager(CreateSettings());
        queue.Pause();
        queue.AddNotification("Title", "Body");

        Assert.Empty(queue.VisibleNotifications);
    }

    [Fact]
    public void Resume_AcceptsNotificationsAgain()
    {
        var queue = new QueueManager(CreateSettings());
        queue.Pause();
        queue.AddNotification("Ignored", "While paused");
        queue.Resume();
        queue.AddNotification("Accepted", "After resume");

        Assert.Single(queue.VisibleNotifications);
        Assert.Equal("Accepted", queue.VisibleNotifications[0].Title);
    }

    [Fact]
    public void ClearAll_RemovesEverything()
    {
        var queue = new QueueManager(CreateSettings());
        queue.AddNotification("A", "1");
        queue.AddNotification("B", "2");
        queue.AddNotification("C", "3");
        queue.AddNotification("D", "4");

        queue.ClearAll();

        Assert.Empty(queue.VisibleNotifications);
        Assert.Equal(0, queue.OverflowCount);
    }

    [Fact]
    public void HasOverflow_ReflectsOverflowState()
    {
        var queue = new QueueManager(CreateSettings());
        Assert.False(queue.HasOverflow);

        queue.AddNotification("A", "1");
        queue.AddNotification("B", "2");
        queue.AddNotification("C", "3");
        Assert.False(queue.HasOverflow);

        queue.AddNotification("D", "4");
        Assert.True(queue.HasOverflow);
    }

    [Fact]
    public void MaxVisible_RespectsSettingsValue()
    {
        var queue = new QueueManager(CreateSettings(maxVisible: 1));
        queue.AddNotification("A", "1");
        queue.AddNotification("B", "2");

        Assert.Single(queue.VisibleNotifications);
        Assert.Equal(1, queue.OverflowCount);
    }

    [Fact]
    public void SettingsChange_TrimsVisibleNotificationsToNewLimit()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "NotificationsProQueue_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var settings = new SettingsManager(tempDir);
            settings.Settings.MaxVisibleNotifications = 3;
            settings.Settings.AnimationDurationMs = 0;
            var queue = new QueueManager(settings);

            queue.AddNotification("A", "1");
            queue.AddNotification("B", "2");
            queue.AddNotification("C", "3");

            var updated = settings.Settings.Clone();
            updated.MaxVisibleNotifications = 1;
            settings.Apply(updated);

            Assert.Single(queue.VisibleNotifications);
            Assert.Equal("C", queue.VisibleNotifications[0].Title);
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    [Fact]
    public void SettingsChange_ReordersVisibleNotificationsForBottomMode()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "NotificationsProQueue_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var settings = new SettingsManager(tempDir);
            settings.Settings.MaxVisibleNotifications = 3;
            settings.Settings.AnimationDurationMs = 0;
            var queue = new QueueManager(settings);

            queue.AddNotification("First", "1");
            queue.AddNotification("Second", "2");
            queue.AddNotification("Third", "3");

            var updated = settings.Settings.Clone();
            updated.NewestOnTop = false;
            settings.Apply(updated);

            Assert.Equal("First", queue.VisibleNotifications[0].Title);
            Assert.Equal("Second", queue.VisibleNotifications[1].Title);
            Assert.Equal("Third", queue.VisibleNotifications[2].Title);
        }
        finally
        {
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }
}
