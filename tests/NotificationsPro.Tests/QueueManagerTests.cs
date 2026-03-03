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

    [Fact]
    public void AddNotification_MutedAppIsSuppressed()
    {
        var settings = CreateSettings();
        settings.Settings.MutedApps.Add("Teams");
        var queue = new QueueManager(settings);

        queue.AddNotification("Teams", "Meeting", "Join now");

        Assert.Empty(queue.VisibleNotifications);
    }

    [Fact]
    public void AddNotification_MuteKeywordSuppresses()
    {
        var settings = CreateSettings();
        settings.Settings.MuteKeywords.Add("spam");
        var queue = new QueueManager(settings);

        queue.AddNotification("App", "This is spam", "body");

        Assert.Empty(queue.VisibleNotifications);
    }

    [Fact]
    public void AddNotification_HighlightKeywordSetsFlag()
    {
        var settings = CreateSettings();
        settings.Settings.HighlightKeywords.Add("urgent");
        var queue = new QueueManager(settings);

        queue.AddNotification("App", "Urgent issue", "Fix now");

        Assert.Single(queue.VisibleNotifications);
        Assert.True(queue.VisibleNotifications[0].IsHighlighted);
    }

    [Fact]
    public void AddNotification_NoHighlightWithoutKeyword()
    {
        var settings = CreateSettings();
        settings.Settings.HighlightKeywords.Add("urgent");
        var queue = new QueueManager(settings);

        queue.AddNotification("App", "Normal message", "Nothing special");

        Assert.Single(queue.VisibleNotifications);
        Assert.False(queue.VisibleNotifications[0].IsHighlighted);
    }

    [Fact]
    public void MuteApp_PreventsNotifications()
    {
        var settings = CreateSettings();
        var queue = new QueueManager(settings);

        queue.AddNotification("Slack", "Hey", "World");
        Assert.Single(queue.VisibleNotifications);

        queue.MuteApp("Slack");
        queue.AddNotification("Slack", "New", "Message");
        Assert.Single(queue.VisibleNotifications);
    }

    [Fact]
    public void UnmuteApp_AllowsNotificationsAgain()
    {
        var settings = CreateSettings();
        var queue = new QueueManager(settings);

        queue.MuteApp("Slack");
        queue.AddNotification("Slack", "Suppressed", "Gone");
        Assert.Empty(queue.VisibleNotifications);

        queue.UnmuteApp("Slack");
        queue.AddNotification("Slack", "Allowed", "Back");
        Assert.Single(queue.VisibleNotifications);
    }

    [Fact]
    public void AddNotification_TracksSeenAppNames()
    {
        var queue = new QueueManager(CreateSettings());

        queue.AddNotification("Teams", "Hello", "World");
        queue.AddNotification("Slack", "Hey", "There");

        Assert.Contains("Teams", queue.SeenAppNames);
        Assert.Contains("Slack", queue.SeenAppNames);
    }

    [Fact]
    public void AddNotification_QuietHoursSuppresses()
    {
        var settings = CreateSettings();
        settings.Settings.QuietHoursEnabled = true;
        // Set quiet hours to cover all day so the test always triggers
        settings.Settings.QuietHoursStart = "00:00";
        settings.Settings.QuietHoursEnd = "23:59";
        var queue = new QueueManager(settings);

        queue.AddNotification("App", "Title", "Body");

        Assert.Empty(queue.VisibleNotifications);
    }

    [Fact]
    public void AddNotification_BurstLimitSuppresses()
    {
        var settings = CreateSettings();
        settings.Settings.BurstLimitEnabled = true;
        settings.Settings.BurstLimitCount = 2;
        settings.Settings.BurstLimitWindowSeconds = 60;
        settings.Settings.DeduplicationEnabled = false;
        var queue = new QueueManager(settings);

        queue.AddNotification("App", "One", "1");
        queue.AddNotification("App", "Two", "2");
        queue.AddNotification("App", "Three", "3"); // should be suppressed by burst

        Assert.Equal(2, queue.VisibleNotifications.Count);
    }

    [Fact]
    public void AddNotification_RegexMuteKeywordSuppresses()
    {
        var settings = CreateSettings();
        settings.Settings.MuteKeywords.Add(@"bug\d+");
        settings.Settings.MuteKeywordRegexFlags["bug\\d+"] = true;
        var queue = new QueueManager(settings);

        queue.AddNotification("App", "Fixed bug123 today", "details");

        Assert.Empty(queue.VisibleNotifications);
    }

    [Fact]
    public void AddNotification_RegexMuteKeywordDoesNotSuppressNonMatch()
    {
        var settings = CreateSettings();
        settings.Settings.MuteKeywords.Add(@"bug\d+");
        settings.Settings.MuteKeywordRegexFlags["bug\\d+"] = true;
        var queue = new QueueManager(settings);

        queue.AddNotification("App", "Fixed bugs today", "details");

        Assert.Single(queue.VisibleNotifications);
    }

    [Fact]
    public void AddNotification_RegexHighlightKeywordSetsFlag()
    {
        var settings = CreateSettings();
        settings.Settings.HighlightKeywords.Add(@"error|warning");
        settings.Settings.HighlightKeywordRegexFlags["error|warning"] = true;
        var queue = new QueueManager(settings);

        queue.AddNotification("App", "Build warning found", "details");

        Assert.Single(queue.VisibleNotifications);
        Assert.True(queue.VisibleNotifications[0].IsHighlighted);
    }

    [Fact]
    public void AddNotification_InvalidRegexDoesNotCrash()
    {
        var settings = CreateSettings();
        settings.Settings.MuteKeywords.Add("[invalid");
        settings.Settings.MuteKeywordRegexFlags["[invalid"] = true;
        var queue = new QueueManager(settings);

        queue.AddNotification("App", "[invalid regex", "body");

        // Invalid regex is silently skipped, notification passes through
        Assert.Single(queue.VisibleNotifications);
    }

    [Fact]
    public void SessionArchive_RecordsNotificationsWhenEnabled()
    {
        var settings = CreateSettings();
        settings.Settings.SessionArchiveEnabled = true;
        settings.Settings.SessionArchiveMaxItems = 100;
        var queue = new QueueManager(settings);

        queue.AddNotification("Teams", "Meeting", "Join now");
        queue.AddNotification("Slack", "Message", "Hello");

        Assert.Equal(2, queue.SessionArchive.Count);
        Assert.Equal("Teams", queue.SessionArchive[0].AppName);
        Assert.Equal("Slack", queue.SessionArchive[1].AppName);
    }

    [Fact]
    public void SessionArchive_DoesNotRecordWhenDisabled()
    {
        var settings = CreateSettings();
        settings.Settings.SessionArchiveEnabled = false;
        var queue = new QueueManager(settings);

        queue.AddNotification("Teams", "Meeting", "Join now");

        Assert.Empty(queue.SessionArchive);
    }

    [Fact]
    public void SessionArchive_RespectsMaxItems()
    {
        var settings = CreateSettings(maxVisible: 20);
        settings.Settings.SessionArchiveEnabled = true;
        settings.Settings.SessionArchiveMaxItems = 10;
        var queue = new QueueManager(settings);

        for (var i = 0; i < 15; i++)
            queue.AddNotification("App", $"Title {i}", $"Body {i}");

        Assert.Equal(10, queue.SessionArchive.Count);
        // Oldest items removed — first entry should be item #5
        Assert.Equal("Title 5", queue.SessionArchive[0].Title);
    }

    [Fact]
    public void SessionArchive_IsRamOnly_ClearedWithNewInstance()
    {
        var settings = CreateSettings();
        settings.Settings.SessionArchiveEnabled = true;
        var queue1 = new QueueManager(settings);
        queue1.AddNotification("App", "Test", "Body");
        Assert.Single(queue1.SessionArchive);

        // New instance has empty archive (simulates app restart)
        var queue2 = new QueueManager(settings);
        Assert.Empty(queue2.SessionArchive);
    }
}
